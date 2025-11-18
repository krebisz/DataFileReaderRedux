using DataFileReader.Class;
using DataFileReader.Helper;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;

namespace DataFileReader;

internal class Program
{
    public static List<string> fileList = new();
    public static MetaDataList metaDataList = new();
    public static HierarchyObjectList hierarchyObjectList = new();
    public static DataTable flattenedData = new();

    private static void Main(string[] args)
    {
        Console.WriteLine("Health Data Reader - Starting...");
        Console.WriteLine("================================\n");

        // Ensure database table exists
        try
        {
            SQLHelper.EnsureHealthMetricsTableExists();
            Console.WriteLine("✓ HealthMetrics table verified/created\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error setting up database: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        var rootDirectory = ConfigurationManager.AppSettings["RootDirectory"];
        Console.WriteLine($"Scanning directory: {rootDirectory}\n");

        fileList = FileHelper.GetFileList(rootDirectory);
        Console.WriteLine($"Found {fileList.Count} files\n");

        // Get list of already processed files from database
        Console.WriteLine("Checking for already processed files...");
        var processedFiles = SQLHelper.GetProcessedFiles();
        Console.WriteLine($"Found {processedFiles.Count} files already processed in database\n");

        // Filter out already processed files
        var originalCount = fileList.Count;
        fileList = fileList.Where(file => !processedFiles.Contains(file)).ToList();
        var filteredCount = fileList.Count;
        var skippedCount = originalCount - filteredCount;

        if (skippedCount > 0)
        {
            Console.WriteLine($"Filtered out {skippedCount} already processed file(s)");
        }
        Console.WriteLine($"{filteredCount} new file(s) to process\n");

        // Check for empty files and mark them as processed
        Console.WriteLine("Checking for empty files...");

        var emptyFiles = new List<string>();

        foreach (var file in fileList.ToList())
        {
            try
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length == 0)
                {
                    emptyFiles.Add(file);
                    SQLHelper.MarkFileAsProcessed(file);
                    processedFiles.Add(file); // Add to processed set so it's filtered out
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Error checking file {FileHelper.GetFileName(file)}: {ex.Message}");
            }
        }

        // Filter out empty files from the list
        if (emptyFiles.Count > 0)
        {
            fileList = fileList.Where(file => !emptyFiles.Contains(file)).ToList();
            Console.WriteLine($"Skipped {emptyFiles.Count} empty file(s)");
        }
        else
        {
            Console.WriteLine("No empty files found");
        }


            Console.WriteLine($"{fileList.Count} file(s) remaining to process\n");

        var maxFiles = int.TryParse(ConfigurationManager.AppSettings["MaxFilesToProcess"], out int max) && max > 0 ? max : fileList.Count;
        var processedCount = 0;
        var totalMetricsInserted = 0;

        try
        {
            foreach (var file in fileList.Take(maxFiles))
            {
                string fileName = FileHelper.GetFileName(file);
                string fileExtension = FileHelper.GetFileExtension(file);

                // Skip unsupported file types
                if (fileExtension != "json" && fileExtension != "csv") continue;

                try
                {
                    string fileContent = File.ReadAllText(file);

                    // Check if this is a Samsung Health file
                    if (SamsungHealthParser.IsSamsungHealthFile(file))
                    {
                        if (fileExtension == "json")
                        {
                            Console.WriteLine($"Processing Samsung Health JSON file: {fileName}");
                            var metrics = SamsungHealthParser.Parse(file, fileContent);

                            if (metrics.Count > 0)
                            {
                                SQLHelper.InsertHealthMetrics(metrics);
                                totalMetricsInserted += metrics.Count;
                                Console.WriteLine($"  ✓ Inserted {metrics.Count} metrics");
                            }
                            else
                            {
                                Console.WriteLine($"  ⚠ No metrics extracted");
                            }
                        }
                        else if (fileExtension == "csv")
                        {
                            //Console.WriteLine($"Processing Samsung Health CSV file: {fileName}");
                            //var metrics = SamsungHealthCsvParser.Parse(file, fileContent);

                            //if (metrics.Count > 0)
                            //{
                            //    SQLHelper.InsertHealthMetrics(metrics);
                            //    totalMetricsInserted += metrics.Count;
                            //    Console.WriteLine($"  ✓ Inserted {metrics.Count} metrics");
                            //}
                            //else
                            //{
                            //    Console.WriteLine($"  ⚠ No metrics extracted");
                            //}
                        }
                    }
                    else
                    {
                        // Use original processing for non-Samsung files
                        if (fileExtension == "json")
                        {
                            Console.WriteLine($"Processing JSON file: {fileName}");
                            Process_JSON_Legacy(fileName, fileContent);
                        }
                        else
                        {
                            Console.WriteLine($"  ⚠ Unsupported file type for non-Samsung files: {fileExtension}");
                        }
                    }

                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        Console.WriteLine($"\nProgress: {processedCount}/{Math.Min(maxFiles, fileList.Count)} files processed, {totalMetricsInserted} metrics inserted\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error processing {fileName}: {ex.Message}");
                }
            }

            Console.WriteLine($"\n================================\n");
            Console.WriteLine($"Processing complete!");
            Console.WriteLine($"Files processed: {processedCount}");
            Console.WriteLine($"Total metrics inserted: {totalMetricsInserted}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Fatal Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Legacy JSON processing (original implementation)
    /// </summary>
    public static void Process_JSON_Legacy(string fileName, string fileData)
    {
        try
        {
            JToken jsonData = JToken.Parse(fileData);

            hierarchyObjectList = new HierarchyObjectList();
            JsoonHelper.CreateHierarchyObjectList(ref hierarchyObjectList, jsonData);

            metaDataList = new MetaDataList(hierarchyObjectList);

            flattenedData = metaDataList.FlattenData(hierarchyObjectList);

            var verboseLogging = ConfigurationManager.AppSettings["VerboseLogging"] == "true";
            if (verboseLogging)
            {
                PrintPathMapList(hierarchyObjectList);
                PrintHierarchyObjectList(hierarchyObjectList);
                PrintMetaDataList(metaDataList);
                PrintFlattenedDataList(flattenedData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error in legacy processing: {ex.Message}");
        }
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