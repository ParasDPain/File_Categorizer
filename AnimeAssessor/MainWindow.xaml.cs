using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Gat.Controls;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
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
        // TODO Algorithm isn't functioning properly, test Manyuu Hiken-chou
        // Produce an output file or email it?
        // Add matrix for Quality/Size ratio

        OpenDialogView openDialog;
        List<DirectoryInfo> dirList;
        BackgroundWorker AnalyzerThread;

        // Analyzer Components
        public static char[] removablesNum = new char[] { '.', '_', '-', ' ', '^', '!', '@', '#', '$', '%', '&', '*', '~', '`', '?', '(', ')', '[', ']', '{', '}', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static char[] removables = new char[] { '.', '_', '-', ' ', '^', '!', '@', '#', '$', '%', '&', '*', '~', '`', '?', '(', ')', '[', ']', '{', '}', '+' };
        public static string animeDBPath = "ANN_AnimeDB_20-12-2015.xml";
        public string parentPath, outputPath;
        public List<string> titles;
        public List<Children> notSortedFiles;
        public List<Category> sortedFiles;

        public MainWindow()
        {
            InitializeComponent();

            InitObjects();
        }

        private void btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            InitBackgroundWorker();

            // Assigns handler methods
            AnalyzerThread.DoWork += AnalyzerThread_DoWork;
            AnalyzerThread.ProgressChanged += AnalyzerThread_ProgressChanged;
            AnalyzerThread.RunWorkerCompleted += AnalyzerThread_RunWorkerCompleted;
            
            // Starts the scan
            AnalyzerThread.RunWorkerAsync();
        }
        
        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            txt_AddDir.Text = string.Empty;
            dirList.Clear();
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
                    if (!dirList.Contains(dir))
                    {
                        dirList.Add(dir);
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
                if (!dirList.Contains(dir))
                {
                    dirList.Add(dir);
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
                    if (!dirList.Contains(dir))
                    {
                        dirList.Add(dir);
                        // TextBox.Text was never changed so no need to reset it

                        btn_Submit.IsEnabled = true;
                    }
                }
            }
            list_Folders.Items.Refresh();
        }

        private void InitObjects()
        {
            dirList = new List<DirectoryInfo>();
            list_Folders.ItemsSource = dirList;
            notSortedFiles = new List<Children>();
            sortedFiles = new List<Category>();
        }

        #region BackgroundWorker Thread
        private void InitBackgroundWorker()
        {
            AnalyzerThread = new BackgroundWorker();
            AnalyzerThread.WorkerReportsProgress = true;
        }

        // Main worker thread
        private void AnalyzerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            RunAnalysis(sender);
        }

        // This event will be called after each file's analysis
        private void AnalyzerThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb_main.Value = e.ProgressPercentage;
            // Update label with current values
            if(e.ProgressPercentage >= 99)
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
            ScanResults resultWindow = new ScanResults(sortedFiles, notSortedFiles, titles);
            resultWindow.ShowDialog();
            lbl_scanProgress.Content = string.Empty;
        }
        #endregion
        private string OpenDialog()
        {
            // Don't reinitialize if the object already exists
            // Won't be able to detect new devices
            if (openDialog == null)
            {
                openDialog = new OpenDialogView();
            }
            OpenDialogViewModel browseWindow = (OpenDialogViewModel)openDialog.DataContext;

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
        private void RunAnalysis(object backgroundWorker)
        {
            titles = LoadXML(animeDBPath, "item", "name");

            List<DirectoryInfo> dirs = new List<DirectoryInfo>();
            List<FileInfo> allFiles = new List<FileInfo>();

            // Find all directories
            foreach (DirectoryInfo d in dirList)
            {
                dirs.AddRange(d.GetDirectories("*", SearchOption.AllDirectories));
            }
            // Add the parent directory as well
            dirs.AddRange(dirList);

            // Find all the files
            foreach (DirectoryInfo dir in dirs)
            {
                allFiles.AddRange(dir.EnumerateFiles());
            }

            sortedFiles = SortFiles(allFiles, backgroundWorker);
        }

        #endregion

        private List<Category> SortFiles(List<FileInfo> allFiles, object backgroundWorker)
        {
            List<Category> categories = new List<Category>();
            int fileCount = 0;

            foreach (FileInfo file in allFiles)
            {
                fileCount++;
                string[] subStrings = file.Name.Split(removables, StringSplitOptions.RemoveEmptyEntries);
                // score holds a value for each title, highest score indicates closer match
                int[] score = new int[titles.Count];
                bool hasAScore = false;

                // list's length - 1 to avoid extensions from being checked
                for (int i = 0; i < titles.Count; i++)
                {
                    for (int j = 0; j < subStrings.Length - 1; j++)
                    {
                        // @\b defines the match to be specific to whole words
                        if (Regex.IsMatch(titles[i], @"\b" + subStrings[j] + @"\b", RegexOptions.IgnoreCase))
                        {
                            foreach (string s in file.Directory.Name.Split(removables, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (Regex.IsMatch(titles[i], @"\b" + s + @"\b", RegexOptions.IgnoreCase))
                                {
                                    score[i]++;
                                }
                            }

                            score[i]++;
                            hasAScore = true;
                            // Console.WriteLine("Found match with title '{0}' with string '{1}' from file '{2}'", titles[j], subStrings[i], file.Name);
                        }
                    }
                    // if the percentage of word matches and total words in the title is > 80% 
                    // boost the score
                    int titleWordCount = titles[i].Split(removables, StringSplitOptions.RemoveEmptyEntries).Length;
                    if ((100 * (score[i]) / (2 * titleWordCount)) > 80)
                    {
                        score[i] += 2;
                    }
                }
                if (hasAScore)
                {
                    // Find the highest score in the list and use it's title value as the title of the Category
                    string titleName = titles[Array.IndexOf(score, score.Max())];
                    bool exists = false;
                    // Check through all the categories if it already exists, otherwise add a new one
                    // TODO perhaps check this in the class's constructor
                    foreach (Category c in categories)
                    {
                        if (c.Name == titleName)
                        {
                            c.AddChildren(new Children(file), titleName);
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        categories.Add(new Category(new Children(file), titleName));
                    }
                }
                else
                {
                    // Files without a score were not matched with any existing category
                    notSortedFiles.Add(new Children(file));
                }
                // Console.WriteLine("File: '{0}' has a max score of {1}", file.Name, score.Max());

                // Update Progress
                // Send percentComplete to the backgroundWorker and the current file number
                int progressPercentage = 100 * fileCount / allFiles.Count;
                // Only the ReportProgress method can update the UI
                (backgroundWorker as BackgroundWorker).ReportProgress(progressPercentage, fileCount);
            }
            return categories;
        }
        
        private List<string> LoadXML(string filePath, string descendant, string element)
        {
            // Load the db
            XDocument db = XDocument.Load(@filePath);
            List<string> titles = new List<string>();

            var query = from c in db.Root.Descendants(descendant)
                            // Only do anime
                        select c.Element("type").Value == "TV" ? c.Element(element).Value : "null";

            foreach (string animeName in query)
            {
                titles.Add(animeName);
            }
            // Sanitize "null" additions
            titles.RemoveAll(value => value == "null");
            titles.Sort();

            titles = DeAccentTitles(titles);

            return titles;
        }

        private List<string> DeAccentTitles(List<string> titlesToClean)
        {
            List<string> sanitizedTitles = new List<string>();
            foreach (string s in titlesToClean)
            {
                string nfdNormalizedString = s.Normalize(NormalizationForm.FormD);
                StringBuilder builder = new StringBuilder();

                foreach (char c in nfdNormalizedString)
                {
                    if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    {
                        builder.Append(c);
                    }
                }
                sanitizedTitles.Add(builder.ToString().Normalize(NormalizationForm.FormC));
            }
            return sanitizedTitles;
        }
    }
}
