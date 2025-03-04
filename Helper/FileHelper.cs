namespace DataFileReader.Helper;

public static class FileHelper
{
    public static List<string> GetFileList(string directory)
    {
        List<string> fileList = new List<string>();

        string[] files = Directory.GetFiles(directory);

        foreach (var file in files) fileList.Add(file);

        string[] subDirectories = Directory.GetDirectories(directory);

        foreach (var subDirectory in subDirectories) GetFileList(subDirectory);

        return fileList;
    }

    public static string GetFileName(string file)
    {
        var fileName = string.Empty;

        char[] separator = { '/', '\\' };
        string[] fileParts = file.Split(separator);

        var filePartsLength = fileParts.Length;

        if (filePartsLength > 0) fileName = fileParts[filePartsLength - 1].Trim().ToLower();

        fileName = DataHelper.RemoveSpecialCharacters(fileName);

        return fileName;
    }

    public static string GetFileExtension(string file)
    {
        var fileExtension = string.Empty;

        string[] fileParts = file.Split('.');

        var filePartsLength = fileParts.Length;

        if (filePartsLength > 0) fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();

        return fileExtension;
    }

    public static List<string> GetDistinctFileExtensions(List<string> fileList)
    {
        List<string> fileExtensions = new List<string>();

        foreach (var file in fileList)
        {
            var fileExtension = string.Empty;

            string[] fileParts = file.Split('.');

            var filePartsLength = fileParts.Length;

            if (filePartsLength > 0) fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();

            if (!fileExtensions.Contains(fileExtension)) fileExtensions.Add(fileExtension);
        }

        return fileExtensions;
    }
}