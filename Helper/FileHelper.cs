using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFileReader.Helper
{
    public static class FileHelper
    {
        public static List<string> GetFileList(string directory)
        {
            List<string> fileList = new List<string>();

            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
            {
                fileList.Add(file);
            }

            string[] subDirectories = Directory.GetDirectories(directory);

            foreach (string subDirectory in subDirectories)
            {
                GetFileList(subDirectory);
            }

            return fileList;
        }
    }
}
