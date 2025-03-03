using DataFileReader.Class;

namespace DataFileReader.Helper
{
    public static class ConsoleHelper
    {
        public static ConsoleColor ConsoleOutputColour(string variableType)
        {
            ConsoleColor consoleColor = new ConsoleColor();

            switch (variableType)
            {
                case "Container":
                    {
                        consoleColor = ConsoleColor.Blue; break;
                    }
                case "Element":
                    {
                        consoleColor = ConsoleColor.Green; break;
                    }
                default:
                    {
                        consoleColor = ConsoleColor.Red; break;
                    }
            }

            return consoleColor;
        }

        public static void PrintFields(string key, string Id, string level, string value, string parent, string metaId, ConsoleColor colour)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   ID: ");

            Console.ForegroundColor = colour;
            Console.Write(Id.PadRight(2));

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   PARENT: ");

            Console.ForegroundColor = colour;
            Console.Write(parent.PadRight(2));

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   LEVEL: ");

            Console.ForegroundColor = colour;
            Console.Write(level.PadRight(2));

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" OBJECT: ");

            Console.ForegroundColor = colour;
            int padLevel = (Int32.Parse(level) * 2);
            string paddedPrefix = string.Empty.PadLeft(padLevel);
            string paddedKey = (paddedPrefix + "|" + key).PadRight(30);

            Console.Write(paddedKey);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   VALUE: ");

            Console.ForegroundColor = colour;
            Console.Write(value.PadRight(60));

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   META-ID: ");

            Console.ForegroundColor = colour;
            Console.Write(metaId.PadRight(30));

            Console.WriteLine();
        }

        public static void PrintUniqueFileExtensions(List<string> FileList)
        {
            List<string> fileExtensions = FileHelper.GetDistinctFileExtensions(FileList);

            Console.WriteLine($"Unique File Extensions:");

            foreach (string fileExtension in fileExtensions)
            {
                Console.WriteLine($"{fileExtension}");
            }
        }

        public static void PrintDataSetInformation(List<MetaData> MetaDataList)
        {
            List<MetaData> distinctMetaDataList = MetaDataList.Distinct(new MetaDataComparer()).ToList();

            Console.WriteLine($"Data Sets: " + MetaDataList.Count);
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

        public static void PrintMetaData(MetaData metaData)
        {
            ConsoleColor variableColour = ConsoleOutputColour(metaData.Type);

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
