using DataFileReader.Class;
using DataFileReader.Helper;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;

namespace DataFileReader;

internal class Program
{
    public static Dictionary<string, string> FlatMap = new();


    public static string TopDirectory; // = @"C:\Documents\Temp\csv\";

    public static List<string> FileList = new();
    public static MetaDataList MetaDataList = new();


    public static HierarchyObjectList _hierarchyObjectList;

    private static void Main(string[] args)
    {
        TopDirectory = ConfigurationManager.AppSettings["RootDirectory"];

        FileList = FileHelper.GetFileList(TopDirectory);

        try
        {
            foreach (var fileName in FileList) ProcessFileData(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.ReadKey();
    }

    public static void ProcessFileData(string file)
    {
        var fileName = FileHelper.GetFileName(file);
        var fileExtension = FileHelper.GetFileExtension(file);
        var fileContent = File.ReadAllText(file);

        if (fileExtension == "csv") ProcessFile_CSV(fileName, fileContent);
        if (fileExtension == "json") ProceessFile_JSON(fileName, fileContent);
        if (fileExtension == "tcx") ProceessFile_TCX(fileName, fileContent);
        //Console.WriteLine($"Unknown File Extension. NO SUPPORT.");
    }

    public static void ProcessFile_CSV(string fileName, string fileData)
    {
        var headerLine = DataHelper.GetHeaderLine(fileData);
        var dataSetName = DataHelper.GetDataSetName(fileName);

        var fileContent = fileData.Replace(headerLine, string.Empty);
        //fileContent = fileContent.Replace("\r", string.Empty);
        //fileContent = fileContent.Replace("\n", string.Empty);

        var metaData = new MetaData(dataSetName, headerLine);
        var existingMetaData = MetaDataList.MetaDataObjects.FirstOrDefault(x => x.ID == metaData.ID);

        if (existingMetaData is null)
        {
            MetaDataList.MetaDataObjects.Add(metaData);
            SQLHelper.CreateSQLTable(metaData);
        }

        SQLHelper.UpdateSQLTable(metaData, fileContent);
    }

    public static void ProceessFile_JSON(string fileName, string fileData)
    {
        fileData = DataHelper.RemoveEscapeCharacters(fileData);
        fileData = DataHelper.RemoveFaultyCharacterSequences(fileData);




        //HierarchyObjectList hierarchyObjectList = new HierarchyObjectList();
        JToken jsonData = JToken.Parse(fileData);

        _hierarchyObjectList = new HierarchyObjectList();

        //HierarchyObject hierarchyObject = new HierarchyObject(0, "Root", "Root", 0, null, "Container");
        //_hierarchyObjectList.Add(hierarchyObject);

        TraverseJson(jsonData);
        GenerateMetaDataList(_hierarchyObjectList);

        //HierarchyObjectList HierarchyObjectList = DataHelper.GetObjectHierarchy(0, "Root", jsonData.ToString(), 0, null);


        try
        {
            //string formattedData = DataHelper.RemoveEscapeCharacters(fileData);
            //formattedData = DataHelper.RemoveFaultyCharacterSequences(formattedData);
            //HierarchyObjectList HierarchyObjectList = DataHelper.GetObjectHierarchy(0, "Root", formattedData, 0, null);

            //GenerateMetaDataList(HierarchyObjectList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static void ProceessFile_TCX(string fileName, string fileData)
    {
    }

    public static void GenerateMetaDataList(HierarchyObjectList HierarchyObjectList)
    {
        CreateMetaData(HierarchyObjectList);

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine("METADATA:");

        foreach (var metaData in MetaDataList.MetaDataObjects)
        {
            ConsoleHelper.PrintMetaData(metaData);
        }

        DataTable flattenedData = MetaDataList.FlattenData(HierarchyObjectList);
        ConsoleHelper.PrintFlattenedData(flattenedData);
    }

    public static void CreateMetaData(HierarchyObjectList HierarchyObjectList)
    {
        HierarchyObjectList.GenerateMetaIDs();


        DataTable hierarchyDataTable = DataHelper.HierarchyObjectList_To_DataTable(HierarchyObjectList);



        foreach (var hierarchyObject in HierarchyObjectList.HierarchyObjects) //MAKE SURE HIERARCHY IS SORTED, OR, GENERATE PARENT ID's RETROACTIVELY
        {
            int? referenceValue = null;
            hierarchyObject.RefVal = GetMetaDataObjectReferenceValue(HierarchyObjectList.HierarchyObjects, hierarchyObject.ID, ref referenceValue).ToString();


            //ConsoleHelper.PrintFields(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleHelper.ConsoleOutputColour(hierarchyObject.ClassID));
            ConsoleHelper.PrintHierarchyObject(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), hierarchyObject.RefVal, ConsoleHelper.ConsoleOutputColour(hierarchyObject.ClassID));

            var metaData = new MetaData();

            var type = hierarchyObject.Value.GetType();

            if (string.IsNullOrEmpty(hierarchyObject.Name)) hierarchyObject.Name = Guid.NewGuid().ToString();

            metaData.Fields.Add(hierarchyObject.Value, type);

            //THIS CAN EITHER BE GENERATED AS BELOW, OR ASSIGNED FROM: hierarchyObject.MetaDataID;
            metaData.GenerateID();

            metaData.Name = hierarchyObject.Name;
            metaData.Type = hierarchyObject.ClassID;
            //metaData.RefVal = hierarchyObject.ParentID.ToString() + ":" + metaData.ID.ToString();

            //int? referenceValue = null;
            metaData.RefVal = GetMetaDataObjectReferenceValue(HierarchyObjectList.HierarchyObjects, hierarchyObject.ID, ref referenceValue).ToString();

            //metaData.RefVal = hierarchyObject.RefVal;


            if (metaData.Type != "Element")
            {
                var existingMetaData = MetaDataList.MetaDataObjects.FirstOrDefault(x => x.RefVal == metaData.RefVal);

                //if (existingMetaData is null)
                //{
                    MetaDataList.MetaDataObjects.Add(metaData);
                //}
            }
            else
            {
                var existingElement = MetaDataList.ElementsList.FirstOrDefault(x => x == metaData.Name);

                if (existingElement is null)
                {
                    MetaDataList.ElementsList.Add(metaData.Name);
                }
            }

        }
    }

    public static int? GetMetaDataObjectReferenceValue(List<HierarchyObject> HierarchyObjectList, int hierarchyObjectID, ref int? referenceValue)
    {
        if (referenceValue == null) referenceValue = 0;

        var hierarchyObject = HierarchyObjectList.FirstOrDefault(x => x.ID == hierarchyObjectID);

        if (hierarchyObject != null)
        {
            referenceValue = referenceValue + hierarchyObject.MetaDataID;

            if (hierarchyObject.ParentID != null && hierarchyObject.ParentID > -1) GetMetaDataObjectReferenceValue(HierarchyObjectList, (int)hierarchyObject.ParentID, ref referenceValue);
        }

        return referenceValue;
    }








    // Change TraverseJson to static so it can be called from a static context
    public static void TraverseJson(JToken token, string path = "Root")
    {
        // Use a static local variable for flatMap since static methods can't access instance fields
        // If you want to keep flatMap as a class-level field, make it static as well
        // For now, let's use a static local variable for demonstration
        // If you want to accumulate results across calls, consider making flatMap static at the class level
        // Dictionary<string, string> flatMap = new(); // Remove this line if you want to use the class-level static field

        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                string currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                _hierarchyObjectList.Add(path, token, "Container");
                TraverseJson(prop.Value, currentPath);
            }
        }
        else if (token is JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                string currentPath = $"{path}[{i}]";
                _hierarchyObjectList.Add(path, token, "Array");
                TraverseJson(array[i], currentPath);
            }
        }
        else
        {
            FlatMap[path] = token.ToString();
            _hierarchyObjectList.Add(path, token, "Element");
            // Primitive value (string, number, bool, etc.)
            Console.WriteLine($"Path: {path}, Value: {token}");





            //HierarchyObject hierarchyObject = new HierarchyObject
            //{
            //    Name = path,
            //    Value = token.ToString(),
            //    ClassID = "Element",
            //    Level = 0, // Adjust level as needed
            //    ParentID = null, // Adjust parent ID as needed
            //    ID = 0 // Assign a unique ID as needed
            //};
        }
    }
}