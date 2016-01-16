using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnimeAssessor
{
    /// <summary>
    /// Interaction logic for ScanResults.xaml
    /// </summary>
    public partial class ScanResults : Window
    {
        List<Category> _SortedList;
        List<Children> _UnsortedList;
        List<string> _Categories;
        public ScanResults(List<Category> sortedList, List<Children> unsortedList, List<string> categories)
        {
            InitializeComponent();

            _SortedList = sortedList;
            _UnsortedList = unsortedList;
            _Categories = categories;
            
            tree_sortedFiles.ItemsSource = _SortedList;
            list_unsortedList.ItemsSource = _UnsortedList;
            list_categoriesList.ItemsSource = _Categories;
        }
    }
}
