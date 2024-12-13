using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataFileReader.Helper
{
    public static class DataHelper
    {
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

            fileName = RemoveSpecialCharacters(fileName);

            return fileName;
        }

        public static string GetHeaderLine(string fileData)
        {
            string headerLine = string.Empty;

            if (!String.IsNullOrEmpty(fileData))
            {
                headerLine = fileData.Split('\n').FirstOrDefault();
            }

            headerLine = headerLine.Replace("\r", string.Empty);
            headerLine = headerLine.Replace("\n", string.Empty);

            return headerLine;
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

        public static string GetDataSetName(string name)
        {
            string dataSetName = string.Empty;

            if (!string.IsNullOrEmpty(name))
            {
                char[] separator = { ',', '.' };
                string[] fileParts = name.Split(separator);
                dataSetName = fileParts.FirstOrDefault();

                dataSetName = dataSetName.Replace("–", string.Empty);
                dataSetName = dataSetName.Replace(" ", string.Empty);
                dataSetName = dataSetName.Replace("_", string.Empty);

                Regex matchingPattern = new Regex("[0-9].*");
                dataSetName = matchingPattern.Replace(dataSetName, string.Empty);

            }

            dataSetName = RemoveSpecialCharacters(dataSetName);

            return dataSetName;
        }

        public static bool HasSpecialChars(string yourString)
        {
            return yourString.Any(character => !char.IsLetterOrDigit(character));
        }

        public static bool IsSpecialCharacter(char character)
        {
            return char.IsLetterOrDigit(character);
        }

        public static string RemoveSpecialCharacters(string name)
        {
            char[] separator = { ',', '.' };
            string[] fileParts = name.Split(separator);
            name = fileParts.FirstOrDefault();

            string normalizedString = string.Empty;

            foreach (char character in name)
            {
                if (!char.IsLetterOrDigit(character)) 
                {
                    
                }
                else
                {
                    normalizedString += character.ToString();
                }
            }

            return normalizedString;
        }
    }
}
