using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper
{
    public static class DataHelper
    {
        public static List<string> fieldlist = new List<string>();

        public static List<HierarchyObject> ObjectHierarchylist = new List<HierarchyObject>();

        public static int IdMax = 0;

        #region File Operations

        public static string GetFileExtension(string file)
        {
            string fileExtension = string.Empty;

            string[] fileParts = file.Split('.');

            int filePartsLength = fileParts.Length;

            if (filePartsLength > 0)
            {
                fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();
            }

            return fileExtension;
        }

        public static string GetFileName(string file)
        {
            string fileName = string.Empty;

            char[] separator = { '/', '\\' };
            string[] fileParts = file.Split(separator);

            int filePartsLength = fileParts.Length;

            if (filePartsLength > 0)
            {
                fileName = fileParts[filePartsLength - 1].Trim().ToLower();
            }

            fileName = RemoveSpecialCharacters(fileName);

            return fileName;
        }

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

        public static List<string> GetDistinctFileExtensions(List<string> fileList)
        {
            List<string> fileExtensions = new List<string>();

            foreach (string file in fileList)
            {
                string fileExtension = string.Empty;

                string[] fileParts = file.Split('.');

                int filePartsLength = fileParts.Length;

                if (filePartsLength > 0)
                {
                    fileExtension = fileParts[filePartsLength - 1].Trim().ToLower();
                }

                if (!fileExtensions.Contains(fileExtension))
                {
                    fileExtensions.Add(fileExtension);
                }
            }

            return fileExtensions;
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


        public static List<string> GetFieldList(JArray objectArray)
        {
            //List<string> fieldlist = new List<string>();

            try
            {
                foreach (var obj in objectArray)
                {
                    object dynamicObject = new object();

                    string fileData = obj.ToString().Trim().Replace(" ", "");
                    fileData = obj.ToString().Trim().Replace("\r", "");
                    fileData = obj.ToString().Trim().Replace("\n", "");
                    fileData = obj.ToString().Trim().Replace("\t", "");

                    dynamicObject = JsonSerializer.Deserialize<dynamic>(fileData);
                    dynamicObject = "[" + dynamicObject + "]";

                    JArray subObjectArray = JArray.Parse(dynamicObject.ToString());

                    if (subObjectArray != null && subObjectArray.Count > 0)
                    {
                        foreach (JObject subObject in subObjectArray)
                        {
                            IJEnumerable<JToken> subObjectValue = subObject.Values();

                            foreach (var subValue in subObjectValue)
                            {
                                fieldlist.Add(subValue.ToString());

                                if (subValue != null && subValue.HasValues)
                                {
                                    JArray subArray = new JArray(subValue);

                                    GetFieldList(subArray);
                                }
                            }

                            //if (subObject["source"].ToString().ToLower().Trim() == "")
                            //{
                            //}
                        }
                    }

                    //if (subObjectArray != null)
                    //{
                    //    GetFieldList(subObjectArray);
                    //}

                    //fieldlist.Add(obj.ToString());
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return fieldlist;
        }

        #endregion


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

        #endregion










        public static List<HierarchyObject> GetObjectHierarchy(int id, string name, string objectData, int level, int? parentId)
        {
            string formattedData = RemoveEscapeCharacters(objectData);
            formattedData = RemoveFaultyCharacterSequences(formattedData);

            if (parentId == null)
            {
                level = 0;
                name = "Root";
                HierarchyObject hierarchyObject = new HierarchyObject(id, name, formattedData, level, parentId);
                //hierarchyObject.Value = GenerateValue(hierarchyObject.Value);
                hierarchyObject.ClassID = "Container";

                hierarchyObject.Value = GenerateValue(hierarchyObject);


                ObjectHierarchylist.Add(hierarchyObject);
                WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, "", ConsoleColor.Blue);
            }
            else
            {
                //    id = (int)(parentId + 1);
                //    level++;
                //    HierarchyObject hierarchyObject = new HierarchyObject(id, objectName, formattedData, level, parentId);
                //    hierarchyObject.Value = GenerateValue(hierarchyObject.Value);
                //    //hierarchyObject.GenerateID();
                //    ObjectHierarchylist.Add(hierarchyObject);

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
                                string value = GenerateValue(name, jDynamicObject.AsArray()[i].ToString());
                                ////hierarchyObject.GenerateID();

                                IdMax = IdMax + 1;
                                HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subName, value, sublevel, parentId));
                                hierarchyObject.ClassID = "Container";
                                ObjectHierarchylist.Add(hierarchyObject);

                                WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Blue);

                                GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, jDynamicObject.AsArray()[i].ToString(), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                                //level = level - 1;
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
                                    ObjectHierarchylist.Add(hierarchyObject);

                                    WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Green);
                                }
                                else if (subObject.Value.GetType() == typeof(JsonObject))
                                {
                                    int sublevel = level + 1;
                                    id = id + 1;
                                    string value = GenerateValue(jDynamicObject.AsObject()[i].ToString());
                                    ////hierarchyObject.GenerateID();

                                    IdMax = IdMax + 1;
                                    HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, value, sublevel, parentId));
                                    hierarchyObject.ClassID = "Container";

                                    ObjectHierarchylist.Add(hierarchyObject);

                                    WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Blue);

                                    GetObjectHierarchy(hierarchyObject.ID, hierarchyObject.Name, JsonSerializer.Serialize(subObject.Value), (int)hierarchyObject.Level, hierarchyObject.ParentID);
                                    ////GetObjectHierarchy(hierarchyObject.Name, JsonSerializer.Serialize(subObject.Value), (int)hierarchyObject.Level, hierarchyObject.ID);
                                    //level = level - 1;
                                }
                                else if (subObject.Value.GetType().Name == "JsonValueOfElement")
                                {
                                    //Console.WriteLine("SubObject is Type JsonValue");
                                    int sublevel = level + 1;
                                    id = id + 1;

                                    IdMax = IdMax + 1;
                                    HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, subObject.Value.ToString(), sublevel, parentId));
                                    hierarchyObject.ClassID = "Element";
                                    ObjectHierarchylist.Add(hierarchyObject);

                                    WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Green);
                                }
                                else if (subObject.Value.GetType() == typeof(JsonArray))
                                {
                                    if (subObject.Key != null)
                                    {
                                        int sublevel = level + 1;
                                        IdMax = IdMax + 1;
                                        string value = GenerateValue(subObject.Key, subObject.Value.ToString());

                                        HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subObject.Key, value, sublevel, parentId));
                                        hierarchyObject.ClassID = "Container";

                                        ObjectHierarchylist.Add(hierarchyObject);

                                        WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Blue);
                                    }

                                    if (subObject.Value != null && subObject.Value.AsArray().Count() > 0)
                                    {
                                        parentId = IdMax;
                                        int sublevel = level + 2;

                                        for (int j = 0; j < subObject.Value.AsArray().Count; j++)
                                        {

                                            id = id + 1;
                                            string subName = subObject.Key + "[" + j + "]";
                                            string value = GenerateValue(subObject.Value.AsArray()[j].ToString());
                                            ////hierarchyObject.GenerateID();

                                            IdMax = IdMax + 1;
                                            HierarchyObject hierarchyObject = (new HierarchyObject(IdMax, subName, value, sublevel, parentId));
                                            hierarchyObject.ClassID = "Container";

                                            ObjectHierarchylist.Add(hierarchyObject);

                                            WriteToConsole(hierarchyObject.Name, hierarchyObject.ID.ToString(), hierarchyObject.Level.ToString(), hierarchyObject.Value, hierarchyObject.ParentID.ToString(), ConsoleColor.Blue);

                                            GetObjectHierarchy(IdMax, subName, subObject.Value.AsArray()[j].ToString(), sublevel, parentId);
                                            //level = level - 1;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("SubObject is Type Other");
                                }

                                ////parentId++;
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

            return ObjectHierarchylist;
        }










        public static void WriteToConsole(string key, string Id, string level, string value, string parent, ConsoleColor colour)
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
            Console.Write(value.PadRight(70));



            Console.WriteLine();
        }



        public static string GenerateValue(string jsonObject)
        {
            string value = string.Empty;

            try
            {
                var parsedValue = JsonNode.Parse(jsonObject);


                if (parsedValue.GetType() == typeof(JsonArray))
                {
                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        JsonNode? parsedObject = parsedValue.AsArray()[i];

                        if (parsedObject != null)
                        {
                            if (parsedObject.GetType() == typeof(JsonObject))
                            {
                                for (int j = 0; j < parsedObject.AsObject().Count; j++)
                                {
                                    string component = string.Empty;

                                    if (parsedObject[j] != null)
                                    {
                                        component = "[" + parsedObject[j].GetPropertyName() + "]";
                                    }

                                    value = value + component + ", ";
                                }

                                //KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsArray()[i].;
                                //value = value + "[" + objectKeyValuePair.Key + "], ";
                            }
                            if (parsedObject.GetType().Name == "JsonValueOfElement")
                            {
                                value = "[" + parsedObject.AsValue() + "]";


                                //for (int j = 0; j < parsedObject.AsArray().Count; j++)
                                //{
                                //    string component = string.Empty;

                                //    if (parsedObject[j] != null)
                                //    {
                                //        component = "[" + parsedObject[j].GetPropertyName() + "]";
                                //    }

                                //    value = value + component + ", ";
                                //}
                            }
                        }
                    }
                }

                if (parsedValue.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < parsedValue.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

                if (parsedValue.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

            }
            catch (Exception ex)
            {
                //throw ex;
                value = jsonObject;
            }

            return value;
        }







        public static string GenerateValue(string parentName, string jsonObject)
        {
            string value = string.Empty;

            try
            {
                var parsedValue = JsonNode.Parse(jsonObject);



                if (parsedValue.GetType() == typeof(JsonArray))
                {
                    string component = string.Empty;

                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        component = parentName + "[" + i + "]";
                        value = value + component + ", ";
                    }
                }


                if (parsedValue.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < parsedValue.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

                if (parsedValue.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

            }
            catch (Exception ex)
            {
                //throw ex;
                value = jsonObject;
            }

            return value;
        }













        public static string GenerateValue(HierarchyObject hierarchyObject)
        {
            string value = string.Empty;

            try
            {
                var parsedValue = JsonNode.Parse(hierarchyObject.Value);


                if (parsedValue.GetType() == typeof(JsonArray))
                {
                    string component = string.Empty;

                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        component = hierarchyObject.Name + "[" + i + "]";
                        value = value + component + ", ";
                    }
                }


                if (parsedValue.GetType() == typeof(JsonObject))
                {
                    for (int i = 0; i < parsedValue.AsObject().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

                if (parsedValue.GetType().Name == "JsonValueOfElement")
                {
                    for (int i = 0; i < parsedValue.AsArray().Count; i++)
                    {
                        KeyValuePair<string, JsonNode?> objectKeyValuePair = parsedValue.AsObject().GetAt(i);
                        value = value + "[" + objectKeyValuePair.Key + "], ";
                    }
                }

            }
            catch (Exception ex)
            {
                //throw ex;
                value = hierarchyObject.Value;
            }

            return value;
        }







        public static string FormatJSONObject(string unformattedData)
        {
            string formattedData = RemoveEscapeCharacters(unformattedData);
            formattedData = RemoveFaultyCharacterSequences(formattedData);

            int level = 0;

            try
            {
                JsonNode? jDynamicObject = JsonNode.Parse(formattedData);

                if (jDynamicObject != null)
                {
                    if (jDynamicObject.GetType() == typeof(JsonArray))
                    {
                        Console.WriteLine("Object is JsonArray at LEVEL: " + jDynamicObject);

                        for (int i = 0; i < jDynamicObject.AsArray().Count; i++)
                        {
                            FormatJSONObject(jDynamicObject.AsArray()[i].ToString());
                        }

                    }
                    if (jDynamicObject.GetType() == typeof(JsonObject))
                    {
                        Console.WriteLine("Object is JsonObject");

                        for (int i = 0; i < jDynamicObject.AsObject().Count; i++)
                        {
                            KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);
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

            return formattedData;
        }


























        //public static string FormatJSONObject(string unformattedData)
        //{
        //    string formattedData = RemoveEscapeCharacters(unformattedData);
        //    formattedData = RemoveFaultyCharacterSequences(formattedData);

        //    try
        //    {
        //        JsonNode? jDynamicObject = JsonNode.Parse(formattedData);

        //        if (jDynamicObject != null)
        //        {
        //            if (jDynamicObject.GetType() == typeof(JsonArray))
        //            {
        //                for (int i = 0; i < jDynamicObject.AsArray().Count; i++)
        //                {
        //                    FormatJSONObject(jDynamicObject.AsArray()[i].ToString());
        //                }

        //                Console.WriteLine($"Object is JsonArray: {jDynamicObject + " with " + jDynamicObject.AsArray().Count + " children"}");

        //            }
        //            if (jDynamicObject.GetType() == typeof(JsonObject))
        //            {
        //                //object k = (jDynamicObject as JsonObject).Deserialize<List<Item>>();

        //                for (int i = 0; i < jDynamicObject.AsObject().Count; i++) 
        //                {
        //                    KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);
        //                    string subObjectString = JsonSerializer.Serialize(subObject);

        //                    FormatJSONObject(subObjectString);
        //                }


        //                Console.WriteLine($"Object is JsonObject: {jDynamicObject}");
        //            }


        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }

        //    return formattedData;
        //}







    }
}