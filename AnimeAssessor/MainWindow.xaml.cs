using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Gat.Controls;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Text;
using System.Globalization;

namespace AnimeAssessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO Produce and suggest the top 5 matches?
        // TODO Folder selection is not user-friendly
        // TODO Pre-compile all patterns beforehand
        // TODO Match directory names for all cases and avoid repetitions for files from the same directory
        // TODO Filter out common encoder keywords
        // TODO Filter by file-format
        // Produce an output file or email it?
        // Add matrix for Quality/Size ratio

        OpenDialogView _openDialog;
        List<DirectoryInfo> _dirList;
        BackgroundWorker _AnalyzerThread;

        // Analyzer Components
        public static char[] _removablesNum = new char[] { '.', '_', '-', ' ', '^', '!', '@', '#', '$', '%', '&', '*', '~', '`', '?', '(', ')', '[', ']', '{', '}', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static char[] _removables = new char[] { '.', '_', '-', ' ', '^', '!', '@', '#', '$', '%', '&', '*', '~', '`', '?', '(', ')', '[', ']', '{', '}', '+' };
        public static string _animeDBPath = "ANN_AnimeDB_20-12-2015.xml";
        public string _parentPath, _outputPath;
        public List<string> _titles;
        public List<Children> _notSortedFiles;
        public List<Category> _sortedFiles;

        public MainWindow()
        {
            InitializeComponent();

            InitObjects();
        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            InitBackgroundWorker();

            // Assigns handler methods
            _AnalyzerThread.DoWork += AnalyzerThread_DoWork;
            _AnalyzerThread.ProgressChanged += AnalyzerThread_ProgressChanged;
            _AnalyzerThread.RunWorkerCompleted += AnalyzerThread_RunWorkerCompleted;

            // Starts the scan
            _AnalyzerThread.RunWorkerAsync();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_AddDir.Text = string.Empty;
            _dirList.Clear();
            list_Folders.Items.Refresh();

            btn_Submit.IsEnabled = false;
        }

        private void btn_AddDir_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (Directory.Exists(txt_AddDir.Text.Trim()))
                {
                    DirectoryInfo dir = new DirectoryInfo(txt_AddDir.Text.Trim());
                    if (!_dirList.Contains(dir))
                    {
                        _dirList.Add(dir);
                        txt_AddDir.Text = string.Empty;
                        list_Folders.Items.Refresh();

                        btn_Submit.IsEnabled = true;
                    }
                }
                // TODO Display notification
                e.Handled = true;
            }
        }

        private void btn_AddDir_Click(object sender, RoutedEventArgs e)
        {
            string folderPath;

            // If TextBox is not null or doesn't contain a valid path
            if (Directory.Exists(txt_AddDir.Text.Trim()))
            {
                DirectoryInfo dir = new DirectoryInfo(txt_AddDir.Text.Trim());
                if (!_dirList.Contains(dir))
                {
                    _dirList.Add(dir);
                    txt_AddDir.Text = string.Empty;

                    btn_Submit.IsEnabled = true;
                }
            }
            // TODO Display notification
            else
            {
                folderPath = OpenDialog();

                // If no path was selected, don't change the textBox value
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    if (!_dirList.Contains(dir))
                    {
                        _dirList.Add(dir);
                        // TextBox.Text was never changed so no need to reset it

                        btn_Submit.IsEnabled = true;
                    }
                }
            }
            list_Folders.Items.Refresh();
        }

        private void InitObjects()
        {
            _dirList = new List<DirectoryInfo>();
            list_Folders.ItemsSource = _dirList;
            _notSortedFiles = new List<Children>();
            _sortedFiles = new List<Category>();
        }

        #region BackgroundWorker Thread
        private void InitBackgroundWorker()
        {
            _AnalyzerThread = new BackgroundWorker();
            _AnalyzerThread.WorkerReportsProgress = true;
        }

        // Main worker thread
        private void AnalyzerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            RunAnalysis((BackgroundWorker)sender);
        }

        // This event will be called after each file's analysis
        private void AnalyzerThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb_main.Value = e.ProgressPercentage;
            // Update label with current values
            if (e.ProgressPercentage >= 99)
            {
                lbl_scanProgress.Content = "Producing Results...";
            }
            else
            {
                lbl_scanProgress.Content = e.UserState + " files scanned";
            }
        }

        // When thread finishes
        private void AnalyzerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Open the Result Window once the scan is complete
            ScanResults resultWindow = new ScanResults(_sortedFiles, _notSortedFiles, _titles);
            resultWindow.ShowDialog();
            lbl_scanProgress.Content = string.Empty;
        }
        #endregion
        private string OpenDialog()
        {
            // Don't reinitialize if the object already exists
            // Won't be able to detect newly added drives/devices
            if (_openDialog == null)
            {
                _openDialog = new OpenDialogView();
            }
            OpenDialogViewModel browseWindow = (OpenDialogViewModel)_openDialog.DataContext;

            // Just disables file view, file view is limited though
            browseWindow.IsDirectoryChooser = true;
            browseWindow.Owner = this;
            browseWindow.NameText = "Choose thy folder from the left pane";


            // Add the selected directory to the list
            if (browseWindow.Show() == true)
            {
                return browseWindow.SelectedFolder.Path;
            }
            else
            {
                return string.Empty;
            }
        }

        #region Analyzer 
        private void RunAnalysis(BackgroundWorker backgroundWorker)
        {
            var repository = new AnimeTvRepository();
            _titles = repository.GetTvTitles(_animeDBPath);
            List<FileInfo> allFiles = new List<FileInfo>();

            try
            {
                // Find all directories
                allFiles = _dirList.SelectMany(d => d.GetDirectories("*", SearchOption.AllDirectories))
                          .SelectMany(d => d.EnumerateFiles())
                          .ToList();
                // Add the files in the parent directory as well
                allFiles.AddRange(_dirList.SelectMany(d => d.EnumerateFiles()).ToList());
            }
            catch (UnauthorizedAccessException)
            {
                // Do nothing for now
            }

            _sortedFiles = SortFiles(allFiles, backgroundWorker);
        }


        private List<Category> SortFiles(List<FileInfo> allFiles, object backgroundWorker)
        {
            List<Category> categories = new List<Category>();
            // Anonymous type creates a list of all titles split into substrings
            var splitTitles = _titles.Select(t => new { titleName = t, Parts = SplitByRemovables(t) }).ToList();
            int fileCount = 0;

            foreach (FileInfo file in allFiles)
            {
                fileCount++;
                string[] fileParts = SplitByRemovables(Path.GetFileNameWithoutExtension(file.Name));
                string[] directoryParts = SplitByRemovables(file.Directory.Name);

                // Anonymous type finds the best match for each file
                // Finds the score for each title, orders it by descending based on the score
                // and gets assigned the first title in the ordered list from splitTitles
                var topTitle = splitTitles.Select(t => new { titleObj = t, Score = ScoreTitles(t.Parts, fileParts, directoryParts) })
                    .OrderByDescending(x => x.Score)
                    .First();

                Children child = new Children(file);

                // A score of 0 would indicate no matches
                if(topTitle.Score > 0)
                {
                    // Searches for a Category with the same name as the topTitle
                    // returns null if none is found
                    Category category = categories.FirstOrDefault(c => c.Name == topTitle.titleObj.titleName);

                    // Child assignment as per the returned value from above
                    if (category == null)
                    {
                        // Create a new category and add to the list
                        category = new Category(child, topTitle.titleObj.titleName);
                        categories.Add(category);
                    }
                    else
                    {
                        // Add to the existing category
                        category.AddChildren(child, topTitle.titleObj.titleName);
                    }
                }
                else
                {
                    // Files without a score were not matched with any existing category
                    _notSortedFiles.Add(child);
                }

                // Update Progress
                // Send percentComplete to the backgroundWorker and the current file number
                int progressPercentage = 100 * fileCount / allFiles.Count;
                // Only the ReportProgress method can update the UI
                ((BackgroundWorker)backgroundWorker).ReportProgress(progressPercentage, fileCount);
            }
            return categories;
        }

        private string[] SplitByRemovables(string value)
        {
            return value.Split(_removables, StringSplitOptions.RemoveEmptyEntries);
        }

        private int ScoreTitles(string[] titleParts, string[] fileParts, string[] directoryParts)
        {
            int score = 0;
            score = fileParts.Intersect(titleParts).Count();
            score += directoryParts.Intersect(titleParts).Count();
            return score;
        }

        #endregion
    }
}
