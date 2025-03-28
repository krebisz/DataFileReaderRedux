using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DataFileReader.Class;
using Newtonsoft.Json.Linq;

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
            string jsonString = jsonToken.ToString().Trim().Replace(" ", "");
            jsonString = jsonString.Trim().Replace("\r", "");
            jsonString = jsonString.Trim().Replace("\n", "");
            jsonString = jsonString.Trim().Replace("\t", "");

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
}