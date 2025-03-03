using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper
{
    public static class DataHelper
    {
        public static List<string> fieldList = new List<string>();

        //public static List<HierarchyObject> HierarchyObjects = new List<HierarchyObject>();
        public static HierarchyObjectList HierarchyObjects = new HierarchyObjectList();

        public static int IdMax = 0;

        #region File Operations

        public static string GetHeaderLine(string fileData)
        {
            string headerLine = string.Empty;

            if (!String.IsNullOrEmpty(fileData))
            {
                headerLine = fileData.Split('\n').FirstOrDefault();
            }

            headerLine = headerLine.Replace("\r", string.Empty);
            headerLine = headerLine.Replace("\n", string.Empty);

            return headerLine;
        }

        public static string GetDataSetName(string name)
        {
            string dataSetName = string.Empty;

            if (!string.IsNullOrEmpty(name))
            {
                char[] separator = { ',', '.' };
                string[] fileParts = name.Split(separator);
                dataSetName = fileParts.FirstOrDefault();

                dataSetName = dataSetName.Replace("–", string.Empty);
                dataSetName = dataSetName.Replace(" ", string.Empty);
                dataSetName = dataSetName.Replace("_", string.Empty);

                Regex matchingPattern = new Regex("[0-9].*");
                dataSetName = matchingPattern.Replace(dataSetName, string.Empty);
            }

            dataSetName = RemoveSpecialCharacters(dataSetName);

            return dataSetName;
        }

        public static List<string> GetFieldList(JArray jsonArray)
        {
            //List<string> fieldlist = new List<string>();

            try
            {
                foreach (JToken jsonToken in jsonArray)
                {
                    string jsonString = jsonToken.ToString().Trim().Replace(" ", "");
                    //jsonString = jsonObject.ToString().Trim().Replace("\r", "");
                    //jsonString = jsonObject.ToString().Trim().Replace("\n", "");
                    //jsonString = jsonObject.ToString().Trim().Replace("\t", "");
                    jsonString = jsonString.Trim().Replace("\r", "");
                    jsonString = jsonString.Trim().Replace("\n", "");
                    jsonString = jsonString.Trim().Replace("\t", "");


                    object dynamicObject = new object();
                    dynamicObject = JsonSerializer.Deserialize<dynamic>(jsonString);
                    dynamicObject = "[" + dynamicObject + "]";

                    JArray childJsonArray = JArray.Parse(dynamicObject.ToString());

                    if (childJsonArray != null && childJsonArray.Count > 0)
                    {
                        foreach (JObject childJsonObject in childJsonArray)
                        {
                            IJEnumerable<JToken> childJsonValues = childJsonObject.Values();

                            foreach (var childJsonValue in childJsonValues)
                            {
                                fieldList.Add(childJsonValue.ToString());

                                if (childJsonValue != null && childJsonValue.HasValues)
                                {
                                    JArray subArray = new JArray(childJsonValue);

                                    GetFieldList(subArray);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return fieldList;
        }

        #endregion File Operations

        #region CharacterOperations

        public static string RemoveFaultyCharacterSequences(object stringObject)
        {
            string objectString = stringObject.ToString().Trim();

            objectString = objectString.Replace(",}", "}");
            objectString = objectString.Replace(",]", "]");

            return objectString;
        }

        public static string RemoveSpecialCharacters(string name)
        {
            char[] separator = { ',', '.' };
            string[] fileParts = name.Split(separator);
            name = fileParts.FirstOrDefault();

            string normalizedString = string.Empty;

            foreach (char character in name)
            {
                if (!char.IsLetterOrDigit(character))
                {
                }
                else
                {
                    normalizedString += character.ToString();
                }
            }

            return normalizedString;
        }

        public static string RemoveEscapeCharacters(object stringObject)
        {
            string objectString = stringObject.ToString().Trim();

            objectString = objectString.Replace("\r", "");
            objectString = objectString.Replace("\n", "");
            objectString = objectString.Replace("\t", "");
            objectString = objectString.Replace(" ", "");

            return objectString;
        }

        private static Dictionary<string, object> ConvertToDictionary(dynamic dynamicObject)
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

        #endregion CharacterOperations

        public static List<HierarchyObject> GetObjectHierarchy(int id, string name, string objectData, int level, int? parentId)
        {
            string formattedData = RemoveEscapeCharacters(objectData);
            formattedData = RemoveFaultyCharacterSequences(formattedData);

            if (parentId == null)
            {
                level = 0;
                name = "Root";
                HierarchyObject hierarchyObject = new HierarchyObject(id, name, formattedData, level, parentId);

                hierarchyObject.ClassID = "Container";

                (string, string) output = GenerateValue(hierarchyObject);
                hierarchyObject.Value = output.Item1;
                hierarchyObject.ClassID = output.Item2;

                HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);
            }
            else
            {
                //    id = (int)(parentId + 1);
                //    level++;
                //    HierarchyObject hierarchyObject = new HierarchyObject(id, objectName, formattedData, level, parentId);
                //    hierarchyObject.Value = GenerateValue(hierarchyObject.Value);
                //    //hierarchyObject.GenerateID();
                //    HierarchyObjects.Add(hierarchyObject);

                //    WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Blue);
            }

            parentId = id;

            try
            {
                JsonNode? jDynamicObject = JsonNode.Parse(formattedData);

                try
                {
                    if (jDynamicObject != null)
                    {
                        if (jDynamicObject.GetType() == typeof(JsonArray))
                        {
                            for (int i = 0; i < jDynamicObject.AsArray().Count; i++)
                            {
                                int sublevel = level + 1;
                                id = id + 1;
                                string subName = name + "[" + i + "]";
                                (string, string) output = GenerateValue(jDynamicObject.AsArray()[i].ToString(), name);

                                IdMax = IdMax + 1;

                                HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subName, output.Item1, sublevel, parentId));
                                hierarchyObject.ClassID = "Container";
                                //hierarchyObject.ClassID = output.Item2;

                                HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, jDynamicObject.AsArray()[i].ToString(), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                            }
                        }
                        if (jDynamicObject.GetType() == typeof(JsonObject))
                        {
                            parentId = id;

                            for (int i = 0; i < jDynamicObject.AsObject().Count; i++)
                            {
                                KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);
                                string subObjectString = JsonSerializer.Serialize(subObject.Value);
                                subObjectString = RemoveEscapeCharacters(subObjectString);
                                subObjectString = RemoveFaultyCharacterSequences(subObjectString);

                                if (subObject.Value == null)
                                {
                                    int sublevel = level + 1;
                                    id = id + 1;

                                    IdMax = IdMax + 1;
                                    HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, string.Empty, sublevel, parentId));
                                    hierarchyObject.ClassID = "Element";

                                    HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                    //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                }
                                else if (subObject.Value.GetType() == typeof(JsonObject))
                                {
                                    int sublevel = level + 1;
                                    id = id + 1;
                                    (string, string) output = GenerateValue(jDynamicObject.AsObject()[i].ToString());

                                    IdMax = IdMax + 1;

                                    HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, output.Item1, sublevel, parentId));
                                    //hierarchyObject.ClassID = output.Item2;
                                    hierarchyObject.ClassID = "Container";

                                    HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                    //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                    GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, JsonSerializer.Serialize(subObject.Value), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                                }
                                else if (subObject.Value.GetType().Name == "JsonValueOfElement")
                                {
                                    int sublevel = level + 1;
                                    id = id + 1;

                                    IdMax = IdMax + 1;
                                    HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, subObject.Value.ToString(), sublevel, parentId));
                                    hierarchyObject.ClassID = "Element";

                                    HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                    //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                }
                                else if (subObject.Value.GetType() == typeof(JsonArray))
                                {
                                    if (subObject.Key != null)
                                    {
                                        int sublevel = level + 1;
                                        IdMax = IdMax + 1;
                                        (string, string) output = GenerateValue(subObject.Value.ToString(), subObject.Key);

                                        HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, output.Item1, sublevel, parentId));
                                        hierarchyObject.ClassID = output.Item2;
                                        //hierarchyObject.ClassID = "Container";

                                        HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                        //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));
                                    }

                                    if (subObject.Value != null && subObject.Value.AsArray().Any())
                                    {
                                        parentId = IdMax;
                                        int sublevel = level + 2;

                                        for (int j = 0; j < subObject.Value.AsArray().Count; j++)
                                        {
                                            id = id + 1;
                                            string subName = subObject.Key + "[" + j + "]";
                                            (string, string) output = GenerateValue(subObject.Value.AsArray()[j].ToString());

                                            IdMax = IdMax + 1;

                                            HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subName, output.Item1, sublevel, parentId));
                                            hierarchyObject.ClassID = output.Item2;
                                            //hierarchyObject.ClassID = "Container";

                                            HierarchyObjects.hierarchyObjectList.Add(hierarchyObject);

                                            //WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), hierarchyObject.MetaDataID.ToString(), ConsoleOutputColour(hierarchyObject.ClassID));

                                            GetObjectHierarchy(IdMax, subName, subObject.Value.AsArray()[j].ToString(), sublevel, parentId);
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
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
            }

            return HierarchyObjects.hierarchyObjectList;
        }

        public static void GenerateObjectHierarchyMetaID(ref List<HierarchyObject> hierarchyObjectList)
        {
            foreach (HierarchyObject hierarchyObject in hierarchyObjectList)
            {
                System.Type type = hierarchyObject.Value.GetType();

                if (String.IsNullOrEmpty(hierarchyObject.Name))
                {
                    hierarchyObject.Name = Guid.NewGuid().ToString();
                }

                hierarchyObject.Fields.Add(hierarchyObject.Value, type);
                hierarchyObject.GenerateMetaDataID();
            }
        }


        public static (string, string) GenerateValue(string jsonString)
        {
            string value = string.Empty;
            string objectType = string.Empty;

            try
            {
                JsonNode? jsonObject = JsonNode.Parse(jsonString);

                if (jsonObject.GetType() == typeof(JsonArray))
                {
                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        JsonNode? jsonNode = jsonObject.AsArray()[i];

                        if (jsonNode != null)
                        {
                            if (jsonNode.GetType() == typeof(JsonObject))
                            {
                                for (int j = 0; j < jsonNode.AsObject().Count; j++)
                                {
                                    string component = string.Empty;

                                    if (jsonNode[j] != null)
                                    {
                                        component = "[" + jsonNode[j].GetPropertyName() + "]";
                                    }

                                    value = value + component + ", ";
                                }
                            }
                            if (jsonNode.GetType().Name == "JsonValueOfElement")
                            {
                                value = "[" + jsonNode.AsValue() + "]";
                            }
                        }
                    }
                }

                if (jsonObject.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < jsonObject.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }

                if (jsonObject.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }

                objectType = "Container";
            }
            catch (Exception ex)
            {
                //throw ex;
                value = jsonString;
                objectType = "Element";
            }

            return (value, objectType);
        }

        public static (string, string) GenerateValue(string jsonString, string parentName)
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

                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        component = parentName + "[" + i + "]";
                        value = value + component + ", ";
                    }
                }

                if (jsonObject.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < jsonObject.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }

                if (jsonObject.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                value = jsonString;
                objectType = "Element";
            }

            return (value, objectType);
        }

        public static (string, string) GenerateValue(HierarchyObject hierarchyObject)
        {
            string value = string.Empty;
            string objectType = string.Empty;

            try
            {
                objectType = "Container";

                JsonNode? jsonObject = JsonNode.Parse(hierarchyObject.Value);

                if (jsonObject.GetType() == typeof(JsonArray))
                {
                    objectType = "Array";

                    string component = string.Empty;

                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        component = hierarchyObject.Name + "[" + i + "]";
                        value = value + component + ", ";
                    }
                }

                if (jsonObject.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < jsonObject.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }

                if (jsonObject.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < jsonObject.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> jsonKeyValuePair = jsonObject.AsObject().GetAt(i);
                        value = value + "[" + jsonKeyValuePair.Key + "], ";
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                value = hierarchyObject.Value;
                objectType = "Element";
            }

            return (value, objectType);
        }

        public static string FormatJSONObject(string unformattedJsonString)
        {
            string jsonString = RemoveEscapeCharacters(unformattedJsonString);
            jsonString = RemoveFaultyCharacterSequences(jsonString);

            try
            {
                JsonNode? jsonObject = JsonNode.Parse(jsonString);

                if (jsonObject != null)
                {
                    if (jsonObject.GetType() == typeof(JsonArray))
                    {
                        //Console.WriteLine("Object is JsonArray at LEVEL: " + jsonObject);

                        for (int i = 0; i < jsonObject.AsArray().Count; i++)
                        {
                            FormatJSONObject(jsonObject.AsArray()[i].ToString());
                        }
                    }
                    if (jsonObject.GetType() == typeof(JsonObject))
                    {
                        //Console.WriteLine("Object is JsonObject");

                        for (int i = 0; i < jsonObject.AsObject().Count; i++)
                        {
                            KeyValuePair<string, JsonNode?> subObject = jsonObject.AsObject().GetAt(i);
                            string subObjectString = JsonSerializer.Serialize(subObject);

                            FormatJSONObject(subObjectString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return jsonString;
        }
    }
}