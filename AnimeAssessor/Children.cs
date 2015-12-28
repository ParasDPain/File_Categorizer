using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimeAssessor
{
    public class Children
    {
        public char[] REMOVABLES = new char[] { '.', '_', '-', ' ', '^', '!', '@', '#', '$', '%', '&', '*', '~', '`', '?', '(', ')', '[', ']', '{', '}', '+' };

        public FileInfo File { get; set; }
        public string FileName { get; private set; }
        public string FullPath { get; private set; }
        public int WidthX { get; private set; }
        public int HeightY { get; private set; }
        public string[] Keywords { get; set; }
        public string Format { get; private set; }
        public long Size { get { return File.Length / 1000000; } }

        public Children(FileInfo file)
        {
            File = file;
            FileName = file.Name;
            FullPath = file.FullName;
            Keywords = file.Name.Split(REMOVABLES, StringSplitOptions.RemoveEmptyEntries);
            Format = Keywords[Keywords.Length - 1];
            
        }
    }
}
