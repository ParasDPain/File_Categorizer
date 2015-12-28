using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimeAssessor
{
    public class Category
    {
        public string Name { get; private set; }
        public long TotalSize { get; private set; }
        public long AverageSize { get { return NumberOfFiles > 0 ? (TotalSize / NumberOfFiles) : 0; } }
        public int NumberOfFiles { get { return ListOfFiles.Count > 0 ? ListOfFiles.Count : 0; } }
        public ObservableCollection<Children> ListOfFiles { get; private set; }

        public Category(Children file, string name)
        {
            ListOfFiles = new ObservableCollection<Children>();
            ListOfFiles.Add(file);
            // Assign totalSize
            TotalSize += file.Size;
            Name = name;
        }

        public void AddChildren(Children f, string name)
        {
            ListOfFiles.Add(f);
            TotalSize += f.Size;
            Name = name;
        }

        private string FindName()
        {
            return "name";
        }
    }
}
