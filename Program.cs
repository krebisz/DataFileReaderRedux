namespace DataFileReader
{
    using System;
    using System.Collections;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using DataFileReader.Class;
    using DataFileReader.Helper;
    using System.Text.Json;
    using Newtonsoft.Json.Linq;

    class Program
    {
        //public static string topDirectory = @"C:\Documents\Personal\Health Data\"; 
        public static string topDirectory = @"C:\Documents\Temp\csv\";

        public static List<string> FileList = new List<string>();
        public static List<MetaData> MetaDataList = new List<MetaData>();       

        static void Main(string[] args)
        {
            FileList = FileHelper.GetFileList(topDirectory);

            try
            {
                foreach (string fileName in FileList)
                {
                    ProcessFileData(fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.ReadKey();
        }

        public static void ProcessFileData(string file)
        {
            string fileName = DataHelper.GetFileName(file);
            string fileExtension = DataHelper.GetFileExtension(file);
            string fileContent = File.ReadAllText(file);

            if (fileExtension == "csv")
            {
                ProcessFile_CSV(fileName, fileContent);
            }
            if (fileExtension == "json")
            {
                ProceessFile_JSON(fileName, fileContent);
            }
            if (fileExtension == "tcx")
            {
                ProceessFile_TCX(fileName, fileContent);
            }
            else
            {
                Console.WriteLine($"Unknown File Extension. NO SUPPORT.");
            }


        }

        public static void ProcessFile_CSV(string fileName, string fileData)
        {
            string headerLine = DataHelper.GetHeaderLine(fileData);
            string dataSetName = DataHelper.GetDataSetName(fileName);

            string fileContent = fileData.Replace(headerLine, string.Empty);
            //fileContent = fileContent.Replace("\r", string.Empty);
            //fileContent = fileContent.Replace("\n", string.Empty);

            MetaData metaData = new MetaData(dataSetName, headerLine);
            MetaData existingMetaData = MetaDataList.FirstOrDefault(x => x.ID == metaData.ID);

            if (existingMetaData is null)
            {
                MetaDataList.Add(metaData);
                SQLHelper.CreateSQLTable(metaData);
            }

            SQLHelper.UpdateSQLTable(metaData, fileContent);
        }

        public static void ProceessFile_JSON(string fileName, string fileData)
        {
            object dynamicObject = new object();

            fileData = fileData.Trim().Replace(" ", "");
            fileData = fileData.Trim().Replace("\r", "");
            fileData = fileData.Trim().Replace("\n", "");
            fileData = fileData.Trim().Replace("\t", "");

            dynamicObject = JsonSerializer.Deserialize<dynamic>(fileData);

            JArray objectArray = JArray.Parse(dynamicObject.ToString());
            //FormatJSON(dynamicObject.ToString(), 0);
            //var objectDictionary = ConvertToDictionary(dynamicObject);

            List<string> list = new List<string>();

            list = DataHelper.GetFieldList(objectArray);

        }

        public static void ProceessFile_TCX(string fileName, string fileData)
        {

        }




        public static void PrintUniqueFileExtensions()
        {
            List<string> fileExtensions = DataHelper.GetDistinctFileExtensions(FileList);

            Console.WriteLine($"Unique File Extensions:");

            foreach (string fileExtension in fileExtensions)
            {
                Console.WriteLine($"{fileExtension}");
            }
        }

        public static void PrintDataSetInformation()
        {
            List<MetaData> distinctMetaDataList = MetaDataList.Distinct(new MetaDataComparer()).ToList();

            Console.WriteLine($"Data Sets: " + MetaDataList.Count());
            Console.WriteLine($"Distinct Data Sets: " + distinctMetaDataList.Distinct().Count());
        }

        public static void PrintFields(MetaData metaData)
        {
            Console.WriteLine($"Data Fields:");

            foreach (var field in metaData.Fields)
            {
                Console.WriteLine($"Field: {field.Key}, Type: {field.Value.Name}");
            }
        }






        static Dictionary<string, object> ConvertToDictionary(dynamic dynamicObject)
        {
            var dictionary = new Dictionary<string, object>();

            System.Text.Json.JsonDocument jsonDocument;
            jsonDocument = (JsonDocument)dynamicObject;

            //Convert dynamic to JsonElement (if using System.Text.Json)
            JsonElement jsonElement = (JsonElement)dynamicObject;

            foreach (var property in jsonElement.EnumerateObject())
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                dictionary[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDecimal(), //Use GetInt32, GetDouble, etc., if specific type expected
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Object => property.Value.ToString(), //For nested objects
                    JsonValueKind.Array => property.Value.ToString(), //For arrays
                    _ => null //Handle nulls and undefined types
                };
#pragma warning restore CS8601 // Possible null reference assignment.
            }

            return dictionary;
        }


    }
}