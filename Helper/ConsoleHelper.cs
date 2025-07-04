using DataFileReader.Class;
using System.Data;

namespace DataFileReader.Helper;

public static class ConsoleHelper
{
    public static ConsoleColor ConsoleOutputColour(string variableType)
    {
        var consoleColor = new ConsoleColor();

        switch (variableType)
        {
            case "Container":
                {
                    consoleColor = ConsoleColor.Blue;
                    break;
                }
            case "Element":
                {
                    consoleColor = ConsoleColor.Green;
                    break;
                }
            default:
                {
                    consoleColor = ConsoleColor.Red;
                    break;
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
        var padLevel = int.Parse(level) * 2;
        var paddedPrefix = string.Empty.PadLeft(padLevel);
        var paddedKey = (paddedPrefix + "|" + key).PadRight(30);

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

        Console.WriteLine("Unique File Extensions:");

        foreach (var fileExtension in fileExtensions) Console.WriteLine($"{fileExtension}");
    }

    public static void PrintDataSetInformation(List<MetaData> MetaDataList)
    {
        List<MetaData> distinctMetaDataList = MetaDataList.Distinct(new MetaDataComparer()).ToList();

        Console.WriteLine("Data Sets: " + MetaDataList.Count);
        Console.WriteLine("Distinct Data Sets: " + distinctMetaDataList.Distinct().Count());
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
        var variableColour = ConsoleOutputColour(metaData.Type);

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Type: ");

        Console.ForegroundColor = variableColour;
        Console.Write($" {metaData.Type.PadRight(12)}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("ID: ");

        Console.ForegroundColor = variableColour;
        Console.Write($" {metaData.ID.ToString().PadRight(16)}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Data Fields: ");

        Console.ForegroundColor = variableColour;
        Console.Write($" {metaData.Fields.First().Key.PadRight(50)}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("REFERENCE: ");

        Console.ForegroundColor = variableColour;
        Console.WriteLine($" {metaData.RefVal.PadRight(20)}");
    }

    public static void PrintMetaDataElements(List<string> elements)
    {
        var variableColour = ConsoleOutputColour("Element");
        Console.ForegroundColor = variableColour;

        foreach (string element in elements)
        {
            Console.Write(element + ",");
        }

        Console.WriteLine();
    }

    public static void PrintFlattenedData(DataTable flattenedData)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine("FLATTENED DATA:");

        for (int i = 0; i < flattenedData.Columns.Count; i++)
        {
            Console.Write(flattenedData.Columns[i].ColumnName + ", ");
        }

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        foreach (DataRow row in flattenedData.Rows)
        {
            Console.WriteLine(string.Join(", ", row.ItemArray));
        }

        //for (int i = 0; i < flattenedData.Rows.Count; i++)
        //{
        //    for (int j = 0; j < flattenedData.Columns.Count; j++)
        //    {
        //        Console.Write(flattenedData.Rows[i][j].ToString() + ", ");
        //    }

        //    Console.WriteLine();
        //}
    }
}