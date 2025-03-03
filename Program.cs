namespace DataFileReader
{
    using DataFileReader.Class;
    using DataFileReader.Helper;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    internal class Program
    {
        //public static string topDirectory = @"C:\Documents\Personal\Health Data\";
        public static string TopDirectory; // = @"C:\Documents\Temp\csv\";

        public static List<string> FileList = new List<string>();
        public static List<MetaData> MetaDataList = new List<MetaData>();

        private static void Main(string[] args)
        {
            TopDirectory = ConfigurationManager.AppSettings["RootDirectory"];

            FileList = FileHelper.GetFileList(TopDirectory);

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
            string fileName = FileHelper.GetFileName(file);
            string fileExtension = FileHelper.GetFileExtension(file);
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





        public static void CreateMetaData(List<HierarchyObject> HierarchyObjectList)
        {
            DataHelper.GenerateObjectHierarchyMetaID(ref HierarchyObjectList);

            foreach (HierarchyObject hierarchyObject in HierarchyObjectList) //MAKE SURE HIERARCHY IS SORTED, OR, GENERATE PARENT ID's RETROACTIVELY
            {
                ConsoleHelper.PrintFields(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleHelper.ConsoleOutputColour(hierarchyObject.ClassID));

                MetaData metaData = new MetaData();

                System.Type type = hierarchyObject.Value.GetType();

                if (String.IsNullOrEmpty(hierarchyObject.Name))
                {
                    hierarchyObject.Name = Guid.NewGuid().ToString();
                }

                metaData.Fields.Add(hierarchyObject.Value, type);

                //THIS CAN EITHER BE GENERATED AS BELOW, OR ASSIGNED FROM: hierarchyObject.MetaDataID;
                metaData.GenerateID();

                metaData.Name = hierarchyObject.Name;
                metaData.Type = hierarchyObject.ClassID;
                //metaData.RefVal = hierarchyObject.ParentID.ToString() + ":" + metaData.ID.ToString();
                int? referenceValue = null;
                metaData.RefVal = GetMetaDataObjectReferenceValue(HierarchyObjectList, hierarchyObject.ID, ref referenceValue).ToString();

                MetaData existingMetaData = MetaDataList.FirstOrDefault(x => x.RefVal == metaData.RefVal);

                if (existingMetaData is null && metaData.Type != "Element")
                {
                    MetaDataList.Add(metaData);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("METADATA:");

            foreach (MetaData metaData in MetaDataList)
            {
                ConsoleHelper.PrintMetaData(metaData);
            }
        }

        public static int? GetMetaDataObjectReferenceValue(List<HierarchyObject> HierarchyObjectList, int hierarchyObjectID, ref int? referenceValue)
        {
            if (referenceValue == null)
            {
                referenceValue = 0;
            }

            HierarchyObject hierarchyObject = HierarchyObjectList.FirstOrDefault(x => x.ID == hierarchyObjectID);

            if (hierarchyObject != null)
            {
                referenceValue = referenceValue + hierarchyObject.MetaDataID;

                if (hierarchyObject.ParentID != null && hierarchyObject.ParentID > -1)
                {
                    GetMetaDataObjectReferenceValue(HierarchyObjectList, (int)hierarchyObject.ParentID, ref referenceValue);
                }
            }

            return referenceValue;
        }
    }
}