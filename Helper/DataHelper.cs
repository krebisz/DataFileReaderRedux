using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper
{
    public static class DataHelper
    {
        public static List<string> fieldlist = new List<string>();

        public static List<HierarchyObject> ObjectHierarchylist = new List<HierarchyObject>();


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










        public static List<HierarchyObject> GetObjectHierarchy(string objectData, int level, int? parentId)
        {
            string formattedData = RemoveEscapeCharacters(objectData);
            formattedData = RemoveFaultyCharacterSequences(formattedData);

            if (parentId == null) 
            {
                level = 0;
                ObjectHierarchylist.Add(new HierarchyObject(0, "Root", formattedData, level, parentId));
            }

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
                            //GetObjectHierarchy(jDynamicObject.AsArray()[i].ToString());
                        }

                    }
                    if (jDynamicObject.GetType() == typeof(JsonObject))
                    {
                        Console.WriteLine("Object is JsonObject");

                        for (int i = 0; i < jDynamicObject.AsObject().Count; i++)
                        {
                            KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);
                            string subObjectString = JsonSerializer.Serialize(subObject);

                            //GetObjectHierarchy(subObjectString);
                        }


                    }


                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }






            return ObjectHierarchylist;
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