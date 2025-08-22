using DataFileReader.Class;
using DataFileReader.Helper;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;

namespace DataFileReader;

internal class Program
{
    public static List<string> fileList = new();
    public static MetaDataList metaDataList = new();
    public static HierarchyObjectList hierarchyObjectList = new();
    public static DataTable flattenedData = new();

    private static void Main(string[] args)
    {
        fileList = FileHelper.GetFileList(ConfigurationManager.AppSettings["RootDirectory"]);

        try
        {
            foreach (var file in fileList)
            {
                string fileName = FileHelper.GetFileName(file);
                string fileExtension = FileHelper.GetFileExtension(file);
                string fileContent = File.ReadAllText(file);

                fileContent = DataHelper.RemoveEscapeCharacters(fileContent);
                fileContent = DataHelper.RemoveFaultyCharacterSequences(fileContent);

                if (fileExtension == "json") Proceess_JSON(fileName, fileContent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.ReadKey();
    }

    public static void Proceess_JSON(string fileName, string fileData)
    {
        JToken jsonData = JToken.Parse(fileData);

        JsoonHelper.CreateHierarchyObjectList(ref hierarchyObjectList, jsonData);
        PrintPathMapList(hierarchyObjectList);

        metaDataList = new MetaDataList(hierarchyObjectList);

        PrintHierarchyObjectList(hierarchyObjectList);
        PrintMetaDataList(metaDataList);

        flattenedData = metaDataList.FlattenData(hierarchyObjectList);
        PrintFlattenedDataList(flattenedData);
    }   

    public static void PrintPathMapList(HierarchyObjectList hierarchyObjectList)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("PATH MAP:");

        foreach (var hierarchyObject in hierarchyObjectList.HierarchyObjects)
        {
            if (!string.IsNullOrEmpty(hierarchyObject.Path))
            {
                ConsoleHelper.PrintPathMap(hierarchyObject);
            }
        }
    }

    private static void PrintHierarchyObjectList(HierarchyObjectList HierarchyObjectList)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("HIERARCHY:");

        //HierarchyObjectList.HierarchyObjects = HierarchyObjectList.HierarchyObjects.OrderBy(h => h.Level).OrderBy(h => h.ParentID).OrderBy(h => h.MetaDataID).ToList();
        //HierarchyObjectList.HierarchyObjects = HierarchyObjectList.HierarchyObjects.OrderBy(h => (Convert.ToDecimal(h.ReferenceValue))).OrderBy(h => h.MetaDataID).ToList();

        foreach (var hierarchyObject in HierarchyObjectList.HierarchyObjects)
        {
            ConsoleHelper.PrintHierarchyObject(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), hierarchyObject.ReferenceValue, ConsoleHelper.ConsoleOutputColour(hierarchyObject.ClassID));
        }
    }

    public static void PrintMetaDataList(MetaDataList metaDataList)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("METADATA:");

        foreach (var metaData in metaDataList.MetaDataObjects)
        {
            ConsoleHelper.PrintMetaData(metaData);
        }
    }

    public static void PrintFlattenedDataList(DataTable flattenedData)
    {
        ConsoleHelper.PrintFlattenedData(flattenedData);
    }
}