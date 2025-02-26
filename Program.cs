namespace DataFileReader
{
    using DataFileReader.Class;
    using DataFileReader.Helper;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    internal class Program
    {
        //public static string topDirectory = @"C:\Documents\Personal\Health Data\";
        public static string topDirectory = @"C:\Documents\Temp\csv\";

        public static List<string> FileList = new List<string>();
        public static List<MetaData> MetaDataList = new List<MetaData>();

        private static void Main(string[] args)
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
                //Console.WriteLine($"Unknown File Extension. NO SUPPORT.");
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

        //public static void ProceessFile_JSON(string fileName, string fileData)
        //{
        //    object dynamicObject = new object();

        //    fileData = fileData.Trim().Replace(" ", "");
        //    fileData = fileData.Trim().Replace("\r", "");
        //    fileData = fileData.Trim().Replace("\n", "");
        //    fileData = fileData.Trim().Replace("\t", "");

        //    dynamicObject = JsonSerializer.Deserialize<dynamic>(fileData);

        //    JArray objectArray = JArray.Parse(dynamicObject.ToString());
        //    //FormatJSON(dynamicObject.ToString(), 0);
        //    //var objectDictionary = ConvertToDictionary(dynamicObject);

        //    List<string> list = new List<string>();
        //    //list = DataHelper.GetFieldList(objectArray);

        //    List<HierarchyObject> HierarchyObject = new List<HierarchyObject>();
        //    HierarchyObject = DataHelper.GetObjectHierarchy(objectArray, null);

        //    MetaData metaData = new MetaData();

        //    //metaData.Fields = ConvertToDictionary(HierarchyObject);
        //    foreach (HierarchyObject hierarchyObject in HierarchyObject)
        //    {
        //        System.Type type = hierarchyObject.Value.GetType();

        //        if (String.IsNullOrEmpty(hierarchyObject.Name))
        //        {
        //            hierarchyObject.Name = Guid.NewGuid().ToString();
        //        }

        //        if (!metaData.Fields.ContainsKey(hierarchyObject.Name))
        //        {
        //            metaData.Fields.Add(hierarchyObject.Name, type);
        //        }
        //        else
        //        {
        //            Console.WriteLine($"ERROR: {hierarchyObject.Name}, of Type: {type.ToString()}, and Value: {hierarchyObject.Value.ToString()} already Adeded.");
        //        }

        //    }

        //    metaData.GenerateID();

        //    MetaData existingMetaData = MetaDataList.FirstOrDefault(x => x.ID == metaData.ID);

        //    if (existingMetaData is null)
        //    {
        //        MetaDataList.Add(metaData);
        //        SQLHelper.CreateSQLTable(metaData);
        //    }
        //}

        public static void ProceessFile_JSON(string fileName, string fileData)
        {
            fileData = DataHelper.RemoveEscapeCharacters(fileData);
            fileData = DataHelper.RemoveFaultyCharacterSequences(fileData);


            object dynamicObject = new object();

            try
            {
                //dynamicObject = JsonSerializer.Deserialize<dynamic>(fileData);

                List<HierarchyObject> HierarchyObjectList = DataHelper.GetObjectHierarchy(0, "Root", fileData, 0, null);

                CreateMetaData(HierarchyObjectList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void ProceessFile_TCX(string fileName, string fileData)
        {
        }



        public static void CreateMetaDataOld(List<HierarchyObject> HierarchyObjectList)
        {
            MetaData metaData = new MetaData();

            foreach (HierarchyObject hierarchyObject in HierarchyObjectList)
            {
                System.Type type = hierarchyObject.Value.GetType();

                if (String.IsNullOrEmpty(hierarchyObject.Name))
                {
                    hierarchyObject.Name = Guid.NewGuid().ToString();
                }

                if (!metaData.Fields.ContainsKey(hierarchyObject.Name))
                {
                    metaData.Fields.Add(hierarchyObject.Name, type);
                }
                else
                {
                    //Console.WriteLine($"ERROR: {hierarchyObject.Name}, of Type: {type.ToString()}, and Value: {hierarchyObject.Value.ToString()} already Adeded.");
                }
            }

            metaData.GenerateID();

            MetaData existingMetaData = MetaDataList.FirstOrDefault(x => x.ID == metaData.ID);

            if (existingMetaData is null)
            {
                MetaDataList.Add(metaData);
            }
        }





        public static void CreateMetaData(List<HierarchyObject> HierarchyObjectList)
        {
            foreach (HierarchyObject hierarchyObject in HierarchyObjectList) //MAKE SURE HIERARCHY IS SORTED, OR, GENERATE PARENT ID's RETROACTIVELY
            {
                MetaData metaData = new MetaData();

                System.Type type = hierarchyObject.Value.GetType();

                if (String.IsNullOrEmpty(hierarchyObject.Name))
                {
                    hierarchyObject.Name = Guid.NewGuid().ToString();
                }

                metaData.Fields.Add(hierarchyObject.Value, type);
                metaData.GenerateID();

                metaData.Name = hierarchyObject.Name;
                metaData.RefVal = hierarchyObject.ParentID.ToString() + ":" + metaData.ID.ToString();
                metaData.Type = hierarchyObject.ClassID;

                hierarchyObject.MetaDataID = metaData.RefVal;

                MetaDataList.Add(metaData);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("METADATA:");

            foreach (MetaData metaData in MetaDataList)
            {
                PrintFields(metaData);
            }
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

        //public static void PrintFields(MetaData metaData)
        //{
        //    Console.WriteLine($"Data Fields:");

        //    foreach (var field in metaData.Fields)
        //    {
        //        Console.WriteLine($"Field: {field.Key}, Type: {field.Value.Name}");
        //    }
        //}

        public static void PrintFields(MetaData metaData)
        {
            ConsoleColor variableColour = new ConsoleColor();

            if (metaData.Type == "Container")
            {
                variableColour = ConsoleColor.Blue;
            }
            else
            {
                variableColour = ConsoleColor.Green;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Type: ");

            Console.ForegroundColor = variableColour;
            Console.Write($" {metaData.Type.PadRight(12)}");



            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"ID: ");

            Console.ForegroundColor = variableColour;
            Console.Write($" {metaData.ID.ToString().PadRight(16)}");



            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Data Fields: ");

            Console.ForegroundColor = variableColour;
            Console.Write($" {metaData.Fields.First().Key.PadRight(50)}");




            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"REFERENCE: ");

            Console.ForegroundColor = variableColour;
            Console.WriteLine($" {metaData.RefVal.ToString().PadRight(20)}");


        }

    }
}