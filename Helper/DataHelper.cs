using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper;

public static class DataHelper
{
    public static HierarchyObjectList HierarchyObjects = new();
    public static int IdMax;

    public static (string, string) GenerateValue(HierarchyObject hierarchyObject)
    {
        return GenerateValue(hierarchyObject.Value);
    }

    public static (string, string) GenerateValue(string jsonString)
    {
        return GenerateValue(jsonString, null);
    }

    public static (string, string) GenerateValue(string jsonString, string? parentName)
    {
        string value = string.Empty;
        string objectType = string.Empty;

        try
        {
            objectType = "Container";
            JsonNode? jsonObject = JsonNode.Parse(jsonString);

            if (jsonObject.GetType() == typeof(JsonArray))
            {
                objectType = "Array";
                string component = string.Empty;

                if (parentName != null)
                    for (var i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        component = parentName + "[" + i + "]";
                        value = value + component + ", ";
                    }
                else
                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        JsonNode? jsonNode = jsonObject.AsArray()[i];

                        if (jsonNode != null)
                        {
                            if (jsonNode.GetType() == typeof(JsonObject))
                                for (var j = 0; j < jsonNode.AsObject().Count; j++)
                                {
                                    component = string.Empty;

                                    if (jsonNode[j] != null) component = "[" + jsonNode[j].GetPropertyName() + "]";

                                    value = value + component + ", ";
                                }

                            if (jsonNode.GetType().Name == "JsonValueOfElement") value = "[" + jsonNode.AsValue() + "]";
                        }
                    }
            }

            if (jsonObject.GetType() == typeof(JsonObject))
                for (var i = 0; i < jsonObject.AsObject().Count; i++)
                {
                    KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                    value = value + "[" + jsonKeyValuePair.Key + "], ";
                }

            if (jsonObject.GetType().Name == "JsonValueOfElement")
                for (var i = 0; i < jsonObject.AsArray().Count; i++)
                {
                    KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                    value = value + "[" + jsonKeyValuePair.Key + "], ";
                }
        }
        catch (Exception ex)
        {
            value = jsonString;
            objectType = "Element";
        }

        return (value, objectType);
    }

    public static List<string> GetFieldList(JArray jsonArray)
    {
        List<string> fieldList = new List<string>();

        foreach (var jsonToken in jsonArray)
        {
            //string jsonString = jsonToken.ToString().Trim().Replace(" ", "");
            //jsonString = jsonString.Trim().Replace("\r", "");
            //jsonString = jsonString.Trim().Replace("\n", "");
            //jsonString = jsonString.Trim().Replace("\t", "");

            string jsonString = RemoveEscapeCharacters(jsonToken.ToString());

            dynamic? dynamicObject = new object();
            dynamicObject = JsonSerializer.Deserialize<dynamic>(jsonString);
            dynamicObject = "[" + dynamicObject + "]";

            var childJsonArray = JArray.Parse(dynamicObject.ToString());

            if (childJsonArray != null && childJsonArray.Count > 0)
                foreach (JObject childJsonObject in childJsonArray)
                {
                    IJEnumerable<JToken> childJsonValues = childJsonObject.Values();

                    foreach (JToken? childJsonValue in childJsonValues)
                    {
                        fieldList.Add(childJsonValue.ToString());

                        if (childJsonValue != null && childJsonValue.HasValues)
                        {
                            var subArray = new JArray(childJsonValue);

                            GetFieldList(subArray);
                        }
                    }
                }
        }

        return fieldList;
    }

    public static HierarchyObjectList GetObjectHierarchyOld(int id, string name, string objectData, int level, int? parentId)
    {
        try
        {
            if (parentId == null)
            {
                (string, string) output = GenerateValue(objectData);

                level = 0;
                name = "Root";
                string value = output.Item1;
                string classID = "Container"; //hierarchyObject.ClassID = output.Item2;

                HierarchyObject hierarchyObject = new HierarchyObject(id, name, value, level, parentId, classID);
                HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
            }

            parentId = id;

            try
            {
                JsonNode? jDynamicObject = JsonNode.Parse(objectData);

                if (jDynamicObject != null)
                {
                    if (jDynamicObject.GetType() == typeof(JsonArray))
                    {
                        for (int i = 0; i < jDynamicObject.AsArray().Count; i++)
                        {
                            (string, string) output = GenerateValue(jDynamicObject.AsArray()[i].ToString(), name);

                            IdMax = IdMax + 1;
                            id = IdMax;
                            string subName = name + "[" + i + "]";
                            string value = output.Item1;
                            string classID = "Container"; //hierarchyObject.ClassID = output.Item2;

                            HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                            HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                            //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));

                            GetObjectHierarchyOld(hierarchyObject.ID, hierarchyObject.Name, jDynamicObject.AsArray()[i].ToString(), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                        }
                    }
                    if (jDynamicObject.GetType() == typeof(JsonObject))
                    {
                        parentId = id;

                        for (var i = 0; i < jDynamicObject.AsObject().Count; i++)
                        {
                            KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);
                            string subObjectString = JsonSerializer.Serialize(subObject.Value);

                            if (subObject.Value == null)
                            {
                                IdMax = IdMax + 1;
                                id = IdMax;
                                string subName = subObject.Key;
                                string value = string.Empty;
                                string classID = "Element";

                                HierarchyObject hierarchyObject = new HierarchyObject(id, subName, value, level + 1, parentId, classID);
                                HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                            }
                            else if (subObject.Value.GetType() == typeof(JsonObject))
                            {
                                (string, string) output = GenerateValue(jDynamicObject.AsObject()[i].ToString());

                                IdMax = IdMax + 1;
                                id = IdMax;
                                string subName = subObject.Key;
                                string value = output.Item1;
                                string classID = "Container"; //hierarchyObject.ClassID = output.Item2;

                                HierarchyObject hierarchyObject = new HierarchyObject(id, subName, value, level + 1, parentId, classID);
                                HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));

                                GetObjectHierarchyOld(hierarchyObject.ID, hierarchyObject.Name, JsonSerializer.Serialize(subObject.Value), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                            }
                            else if (subObject.Value.GetType().Name == "JsonValueOfElement")
                            {
                                IdMax = IdMax + 1;
                                id = IdMax;
                                string subName = subObject.Key;
                                string value = subObject.Value.ToString();
                                string classID = "Element";

                                HierarchyObject hierarchyObject = new HierarchyObject(id, subName, value, level + 1, parentId, classID);
                                HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                            }
                            else if (subObject.Value.GetType() == typeof(JsonArray))
                            {
                                if (subObject.Key != null)
                                {
                                    (string, string) output = GenerateValue(subObject.Value.ToString(), subObject.Key);

                                    IdMax = IdMax + 1;
                                    id = IdMax;
                                    string subName = subObject.Key;
                                    string value = output.Item1;
                                    string classID = output.Item2; //hierarchyObject.ClassID = "Container";

                                    HierarchyObject hierarchyObject = new HierarchyObject(id, subName, value, level + 1, parentId, classID);
                                    HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                    //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                }

                                if (subObject.Value != null && subObject.Value.AsArray().Any())
                                {
                                    parentId = IdMax;

                                    for (var j = 0; j < subObject.Value.AsArray().Count; j++)
                                    {
                                        (string, string) output = GenerateValue(subObject.Value.AsArray()[j].ToString());

                                        IdMax = IdMax + 1;
                                        id = IdMax;
                                        //int sublevel = level + 1;
                                        string subName = subObject.Key + "[" + j + "]";
                                        string value = output.Item1;
                                        string classID = output.Item2; //hierarchyObject.ClassID = "Container";

                                        HierarchyObject hierarchyObject = new HierarchyObject(id, subName, value, level + 2, parentId, classID);
                                        HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                        //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));

                                        GetObjectHierarchyOld(id, subName, subObject.Value.AsArray()[j].ToString(), level + 2, parentId);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("SubObject is Type Other");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return HierarchyObjects;
    }

    public static HierarchyObjectList GetObjectHierarchy(int id, string name, string objectData, int level, int? parentId)
    {
        try
        {
            try
            {
                if (parentId == null)
                {
                    (string, string) output = GenerateValue(objectData, "Root");

                    //level = 0;
                    //name = "Root";
                    string value = output.Item1;
                    string classID = output.Item2; //hierarchyObject.ClassID = Container;

                    HierarchyObject hierarchyObject = new HierarchyObject(id, name, value, level, parentId, classID);
                    HierarchyObjects.HierarchyObjects.Add(hierarchyObject);

                    parentId = id;
                    //IdMax = IdMax + 1;
                    ;

                    GetObjectHierarchy(IdMax, hierarchyObject.Name, objectData, level + 1, parentId);
                }
                else
                {
                    JsonNode? jDynamicObject = JsonNode.Parse(objectData);

                    if (jDynamicObject != null)
                    {
                        parentId = id;

                        if (jDynamicObject.GetType() == typeof(JsonArray))
                        {
                            for (int i = 0; i < jDynamicObject.AsArray().Count; i++)
                            {
                                (string, string) output = GenerateValue(jDynamicObject.AsArray()[i].ToString(), name);

                                IdMax = IdMax + 1;
                                string subName = name + "[" + i + "]";
                                string value = output.Item1;
                                string classID = "Container"; //hierarchyObject.ClassID = output.Item2;

                                HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                                HierarchyObjects.HierarchyObjects.Add(hierarchyObject);

                                GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, jDynamicObject.AsArray()[i].ToString(), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                            }
                        }

                        if (jDynamicObject.GetType() == typeof(JsonObject))
                        {
                            for (var i = 0; i < jDynamicObject.AsObject().Count; i++)
                            {
                                KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);

                                if (subObject.Value == null)
                                {
                                    IdMax = IdMax + 1;
                                    string subName = subObject.Key;
                                    string value = string.Empty;
                                    string classID = "Element";

                                    HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                                    HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                }
                                else if (subObject.Value.GetType() == typeof(JsonObject))
                                {
                                    (string, string) output = GenerateValue(jDynamicObject.AsObject()[i].ToString());

                                    IdMax = IdMax + 1;
                                    string subName = subObject.Key;
                                    string value = output.Item1;
                                    string classID = "Container"; //hierarchyObject.ClassID = output.Item2;

                                    HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                                    HierarchyObjects.HierarchyObjects.Add(hierarchyObject);

                                    GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, JsonSerializer.Serialize(subObject.Value), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                                }
                                else if (subObject.Value.GetType().Name == "JsonValueOfElement")
                                {
                                    (string, string) output = GenerateValue(subObject.Value.ToString(), subObject.Key);

                                    IdMax = IdMax + 1;
                                    string subName = subObject.Key;
                                    string value = subObject.Value.ToString();
                                    string classID = output.Item2;  //"Element";

                                    HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                                    HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                }
                                else if (subObject.Value.GetType() == typeof(JsonArray))
                                {
                                    if (subObject.Key != null)
                                    {
                                        (string, string) output = GenerateValue(subObject.Value.ToString(), subObject.Key);

                                        IdMax = IdMax + 1;
                                        string subName = subObject.Key;
                                        string value = output.Item1;
                                        string classID = output.Item2; //hierarchyObject.ClassID = "Container";

                                        HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 1, parentId, classID);
                                        HierarchyObjects.HierarchyObjects.Add(hierarchyObject);
                                    }

                                    if (subObject.Value != null && subObject.Value.AsArray().Any())
                                    {
                                        parentId = IdMax;

                                        for (var j = 0; j < subObject.Value.AsArray().Count; j++)
                                        {
                                            (string, string) output = GenerateValue(subObject.Value.AsArray()[j].ToString());

                                            IdMax = IdMax + 1;
                                            string subName = subObject.Key + "[" + j + "]";
                                            string value = output.Item1;
                                            string classID = output.Item2; //hierarchyObject.ClassID = "Container";

                                            HierarchyObject hierarchyObject = new HierarchyObject(IdMax, subName, value, level + 2, parentId, classID);
                                            HierarchyObjects.HierarchyObjects.Add(hierarchyObject);

                                            GetObjectHierarchy(IdMax, subName, subObject.Value.AsArray()[j].ToString(), level + 2, parentId);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("SubObject is Type Other");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return HierarchyObjects;
    }

    #region File Operations

    public static string GetHeaderLine(string lineData)
    {
        var headerLine = string.Empty;

        if (!string.IsNullOrEmpty(lineData)) headerLine = lineData.Split('\n').FirstOrDefault();

        headerLine = headerLine.Replace("\r", string.Empty);
        headerLine = headerLine.Replace("\n", string.Empty);

        return headerLine;
    }

    public static string GetDataSetName(string name)
    {
        var dataSetName = string.Empty;

        if (!string.IsNullOrEmpty(name))
        {
            char[] separator = { ',', '.' };
            string[] nameParts = name.Split(separator);
            dataSetName = nameParts.FirstOrDefault();

            dataSetName = dataSetName.Replace("–", string.Empty);
            dataSetName = dataSetName.Replace(" ", string.Empty);
            dataSetName = dataSetName.Replace("_", string.Empty);

            var matchingPattern = new Regex("[0-9].*");
            dataSetName = matchingPattern.Replace(dataSetName, string.Empty);
        }

        dataSetName = RemoveSpecialCharacters(dataSetName);

        return dataSetName;
    }

    #endregion File Operations

    #region CharacterOperations

    public static string RemoveFaultyCharacterSequences(object stringObject)
    {
        var objectString = stringObject.ToString().Trim();

        objectString = objectString.Replace(",}", "}");
        objectString = objectString.Replace(",]", "]");

        return objectString;
    }

    public static string RemoveSpecialCharacters(string name)
    {
        char[] separator = { ',', '.' };
        string[] fileParts = name.Split(separator);
        name = fileParts.FirstOrDefault();

        var normalizedString = string.Empty;

        foreach (var character in name)
            if (!char.IsLetterOrDigit(character))
            {
            }
            else
            {
                normalizedString += character.ToString();
            }

        return normalizedString;
    }

    public static string RemoveEscapeCharacters(object stringObject)
    {
        var objectString = stringObject.ToString().Trim();

        objectString = objectString.Replace("\r", "");
        objectString = objectString.Replace("\n", "");
        objectString = objectString.Replace("\t", "");
        objectString = objectString.Replace(" ", "");

        return objectString;
    }

    private static Dictionary<string, object> ConvertToDictionary(dynamic dynamicObject)
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        JsonDocument jsonDocument = (JsonDocument)dynamicObject;

        //Convert dynamic to JsonElement (if using System.Text.Json)
        JsonElement jsonElement = (JsonElement)dynamicObject;

        foreach (var property in jsonElement.EnumerateObject())
        {
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
        }

        return dictionary;
    }

    #endregion CharacterOperations



    // Custom comparer to treat nulls and empty strings as equal
    public class RowComparer : IEqualityComparer<object[]>
    {
        public bool Equals(object[] x, object[] y)
        {
            if (x == null || y == null)
                return x == y; // Both are null, so they're equal

            // Compare each item in the row arrays
            for (int i = 0; i < x.Length; i++)
            {
                // Treat null and empty strings as equal
                if (string.IsNullOrEmpty(x[i]?.ToString()) && string.IsNullOrEmpty(y[i]?.ToString()))
                    continue;

                if (!object.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(object[] obj)
        {
            // A simple hash code implementation, ensuring that rows with null or empty values have the same hash code
            int hash = 0;
            foreach (var item in obj)
            {
                // Null and empty values are treated as the same
                hash = hash * 31 + (item == null || string.IsNullOrEmpty(item.ToString()) ? 0 : item.GetHashCode());
            }

            return hash;
        }
    }





    //SORT OF DATATABLE IS ASSUMED
    public static int GetNonEmptyFieldsTotal(DataTable dataTable)
    {
        DataRow dataRow = dataTable.Rows[dataTable.Rows.Count - 1];

        int fieldTotal = GetNonEmptyFieldsTotal(dataRow);

        return fieldTotal;
    }

    public static int GetNonEmptyFieldsTotal(DataRow dataRow)
    {
        int fieldTotal = 0;

        for (int j = 0; j < dataRow.ItemArray.Length; j++)
        {
            if (!String.IsNullOrEmpty(dataRow[j].ToString()))
            {
                fieldTotal++;
            }
        }

        return fieldTotal;
    }

    public static int GetNonEmptyFieldsTotal(object?[] rowArray)
    {
        int fieldTotal = 0;

        for (int j = 0; j < rowArray.Length; j++)
        {
            if (!String.IsNullOrEmpty(rowArray[j].ToString()))
            {
                fieldTotal++;
            }
        }

        return fieldTotal;
    }

    public static DataTable GetDistinctRows(DataTable sourceTable)
    {
        int maxfieldTotal = GetNonEmptyFieldsTotal(sourceTable);

        DataTable newTable = sourceTable.Clone();

        List<object?[]> distinctRows = sourceTable.AsEnumerable().Select(row => row.ItemArray).Distinct(new RowComparer()).ToList(); // Apply the custom comparer

        foreach (object?[] rowArray in distinctRows)
        {
            int fieldTotal = GetNonEmptyFieldsTotal(rowArray);

            if (fieldTotal >= maxfieldTotal)
            {
                newTable.Rows.Add(rowArray); // Add each distinct row (as an array) to the new DataTable
            }
        }

        return newTable;
    }








    public static DataTable HierarchyObjectList_To_DataTable(HierarchyObjectList hierarchyObjectList)
    {
        DataTable dataTable = new DataTable();

        foreach (HierarchyObject h in hierarchyObjectList.HierarchyObjects)
        {
            DataColumn dataColumn = new DataColumn(h.ID.ToString(), typeof(string));
            dataTable.Columns.Add(dataColumn);
        }

        DataRow dataRow = dataTable.NewRow();

        foreach (HierarchyObject h in hierarchyObjectList.HierarchyObjects)
        {
            dataRow[h.ID.ToString()] = h.Value;
        }

        dataTable.Rows.Add(dataRow);

        return dataTable;
    }



    public static DataTable HierarchyObjectList_To_DataTableNew(HierarchyObjectList hierarchyObjectList)
    {
        DataTable dataTable = new DataTable();

        //1. SORT hierarchyObjectList first by ID, then by Level
        //2. Create Sub-DataTables for each Level, starting with highest Levels, and the Common Parent ID as the first column (Key)
        //3. Add the Values of each HierarchyObject to the corresponding Sub-DataTable
        //4. Merge the Sub-DataTables into a new DataTable, with the Next Common Parent ID as the new first column
        //4. Move to the next Level, and repeat until all Levels are processed, with the lowest Level being the last
        //5. Return the new DataTable



        var sortedHierarchyObjects = hierarchyObjectList.HierarchyObjects.OrderBy(h => h.Level).ThenBy(h => h.ID).ToList();
        int maxLevel = (int)sortedHierarchyObjects.Max(h => h.Level);
        int currentLevel = maxLevel;
        Dictionary<int, DataTable> levelDataTables = new Dictionary<int, DataTable>();
        // Create DataTables for each level
        for (int i = 0; i <= maxLevel; i++)
        {
            DataTable levelDataTable = new DataTable($"Level_{i}");
            levelDataTable.Columns.Add("ParentID", typeof(int?)); // Common Parent ID
            levelDataTables[i] = levelDataTable;
        }
        // Populate DataTables with HierarchyObjects
        foreach (var hierarchyObject in sortedHierarchyObjects)
        {
            // Get the DataTable for the current level
            DataTable currentDataTable = levelDataTables[(int)hierarchyObject.Level];
            // Create a new DataRow
            DataRow dataRow = currentDataTable.NewRow();
            dataRow["ParentID"] = hierarchyObject.ParentID;
            // Add the HierarchyObject's ID and Value to the DataRow
            dataRow[hierarchyObject.ID.ToString()] = hierarchyObject.Value;
            // Add the DataRow to the current DataTable
            currentDataTable.Rows.Add(dataRow);
        }
        // Merge DataTables into the final DataTable
        foreach (var kvp in levelDataTables)
        {
            DataTable levelDataTable = kvp.Value;
            if (dataTable.Columns.Count == 0)
            {
                // Initialize the main DataTable with the first level's columns
                dataTable = levelDataTable.Clone();
            }
            else
            {
                // Add columns from the current level DataTable to the main DataTable
                foreach (DataColumn column in levelDataTable.Columns)
                {
                    if (!dataTable.Columns.Contains(column.ColumnName))
                    {
                        dataTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }
            }
            // Merge rows from the current level DataTable into the main DataTable
            foreach (DataRow row in levelDataTable.Rows)
            {
                DataRow newRow = dataTable.NewRow();
                newRow.ItemArray = row.ItemArray;
                dataTable.Rows.Add(newRow);
            }
        }
        // Remove duplicate rows based on the ParentID and ID columns
        var distinctRows = dataTable.AsEnumerable()
            .GroupBy(row => new { ParentID = row.Field<int?>("ParentID"), ID = row.Field<string>("ID") })
            .Select(g => g.First())
            .CopyToDataTable();
        dataTable = distinctRows;
        // Ensure the ParentID column is the first column
        if (dataTable.Columns.Contains("ParentID"))
        {
            DataColumn parentIdColumn = dataTable.Columns["ParentID"];
            dataTable.Columns.Remove(parentIdColumn);
            dataTable.Columns.Add(parentIdColumn);
        }
        // Ensure the ID column is the second column
        if (dataTable.Columns.Contains("ID"))
        {
            DataColumn idColumn = dataTable.Columns["ID"];
            dataTable.Columns.Remove(idColumn);
            dataTable.Columns.Add(idColumn);
        }
        // Ensure the Value column is the third column
        if (dataTable.Columns.Contains("Value"))
        {
            DataColumn valueColumn = dataTable.Columns["Value"];
            dataTable.Columns.Remove(valueColumn);
            dataTable.Columns.Add(valueColumn);
        }
        // Ensure the ClassID column is the fourth column
        if (dataTable.Columns.Contains("ClassID"))
        {
            DataColumn classIdColumn = dataTable.Columns["ClassID"];
            dataTable.Columns.Remove(classIdColumn);
            dataTable.Columns.Add(classIdColumn);
        }
        // Ensure the Level column is the fifth column
        if (dataTable.Columns.Contains("Level"))
        {
            DataColumn levelColumn = dataTable.Columns["Level"];
            dataTable.Columns.Remove(levelColumn);
            dataTable.Columns.Add(levelColumn);
        }
        // Ensure the MetaDataID column is the sixth column
        if (dataTable.Columns.Contains("MetaDataID"))
        {
            DataColumn metaDataIdColumn = dataTable.Columns["MetaDataID"];
            dataTable.Columns.Remove(metaDataIdColumn);
            dataTable.Columns.Add(metaDataIdColumn);
        }
        // Ensure the Name column is the seventh column
        if (dataTable.Columns.Contains("Name"))
        {
            DataColumn nameColumn = dataTable.Columns["Name"];
            dataTable.Columns.Remove(nameColumn);
            dataTable.Columns.Add(nameColumn);
        }

        return dataTable;
    }


}