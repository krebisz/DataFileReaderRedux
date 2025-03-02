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

        public static string GetFileName(string file)
        {
            string fileName = string.Empty;

            char[] separator = { '/', '\\' };
            string[] fileParts = file.Split(separator);

            int filePartsLength = fileParts.Length;

            if (filePartsLength > 0)
            {
                fileName = fileParts[filePartsLength - 1].Trim().ToLower();
            }

            fileName = DataHelper.RemoveSpecialCharacters(fileName);

            return fileName;
        }


        public static string GetFileExtension(string file)
        {
            string fileExtension = string.Empty;

            string[] fileParts = file.Split('.');

            int filePartsLength = fileParts.Length;

            if (filePartsLength > 0)
            {
                fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();
            }

            return fileExtension;
        }


        public static List<string> GetDistinctFileExtensions(List<string> fileList)
        {
            List<string> fileExtensions = new List<string>();

            foreach (string file in fileList)
            {
                string fileExtension = string.Empty;

                string[] fileParts = file.Split('.');

                int filePartsLength = fileParts.Length;

                if (filePartsLength > 0)
                {
                    fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();
                }

                if (!fileExtensions.Contains(fileExtension))
                {
                    fileExtensions.Add(fileExtension);
                }
            }

            return fileExtensions;
        }


    }
}