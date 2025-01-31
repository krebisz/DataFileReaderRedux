using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Dynamic;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper
{
    public static class DataHelper
    {
        public static List<string> fieldlist = new List<string>();

        public static List<HierarchyObject> ObjectHierarchylist = new List<HierarchyObject>();

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

        //public static List<string> GetFieldList(JArray objectArray)
        //{
        //    List<string> fieldlist = new List<string>();

        //    try
        //    {
        //        foreach (var obj in objectArray)
        //        {
        //            string objectString = obj.ToString();
        //            string dynamicObject = objectString;

        //            //dynamicObject = FormatJSON(objectString, 1);
        //            dynamicObject = dynamicObject.Trim().Replace("\r", "");
        //            dynamicObject = dynamicObject.Trim().Replace("\n", "");
        //            dynamicObject = dynamicObject.Trim().Replace("\t", "");
        //            dynamicObject = dynamicObject.Trim().Replace("\\", "");
        //            //dynamicObject = dynamicObject.Trim().Replace(" ", "");

        //            dynamicObject = JsonSerializer.Deserialize<dynamic>(dynamicObject);

        //            JArray subObjectArray = JArray.Parse(dynamicObject);

        //            if (subObjectArray != null && subObjectArray.Count > 0)
        //            {
        //                foreach (JObject channelObj in subObjectArray)
        //                {
        //                    if (channelObj["source"].ToString().ToLower().Trim() == "")
        //                    {
        //                    }
        //                }
        //            }

        //            if (subObjectArray != null)
        //            {
        //                GetFieldList(subObjectArray);
        //            }

        //            fieldlist.Add(obj.ToString());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    return fieldlist;
        //}

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

        public static List<HierarchyObject> GetObjectHierarchy(string objectData, int? level, int? parentId)
        {
         
            FormatJSONObject(objectData);


            





            //JArray objectArray = JArray.Parse(objectData.ToString());

            //bool setNameByPath = false; //TO MAKE A PARAMETER

            //level = level is null ? 0 : level.Value;

            //if (level == 0)
            //{
            //    ObjectHierarchylist.Add(new HierarchyObject(0, "Root", "", level, parentId));
            //    parentId = 0;
            //}

            //level++;
            
            //try
            //{
            //    foreach (var obj in objectArray)
            //    {
            //        object dynamicObject = new object();
            //        dynamicObject = JsonSerializer.Deserialize<dynamic>(obj.ToString());
            //        dynamicObject = "[" + dynamicObject + "]";

            //        JArray subObjectArray = JArray.Parse(dynamicObject.ToString());

            //        if (subObjectArray != null && subObjectArray.Count > 0 && subObjectArray.First() != null)
            //        {
            //            if (subObjectArray.First().GetType() == typeof(JObject))
            //            {
            //                foreach (var subObject in subObjectArray)
            //                {
            //                    if (subObject.GetType() == typeof(JObject))
            //                    {
            //                        IJEnumerable<JToken> subObjectValue = subObject.Values();

            //                        foreach (var subValue in subObjectValue)
            //                        {
            //                            int i = ObjectHierarchylist.Max(x => x.ID) + 1;

            //                            if ((subValue.Path).ToString().Split('.').Length > 1)
            //                            {
            //                                string hierarchyObjectName = (subValue.Path).ToString().Split('.')[1];

            //                                if (setNameByPath)
            //                                {
            //                                    hierarchyObjectName = subValue.Path.ToString();
            //                                }

            //                                ObjectHierarchylist.Add(new HierarchyObject(i, hierarchyObjectName, subValue.ToString(), level, parentId));
            //                            }
            //                            else
            //                            {
            //                                ObjectHierarchylist.Add(new HierarchyObject(i, subValue.ToString(), level, parentId));
            //                            }

            //                            if (subValue != null && subValue.HasValues)
            //                            {
            //                                JArray subArray = new JArray(subValue);

            //                                parentId = i;

            //                                if (((JArray)subValue).Count > 1)
            //                                {
            //                                    GetObjectHierarchy(((JArray)subValue).ToString(), level, parentId);
            //                                    level--;
            //                                }
            //                            }
            //                        }
            //                    }
            //                    else if (subObject.GetType() == typeof(JArray))
            //                    {
            //                        JArray subArray = new JArray(subObject);

            //                        GetObjectHierarchy(subArray.ToString(), level, parentId);
            //                        level--;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}

            return ObjectHierarchylist;
        }

        public static bool HasSpecialChars(string dataString)
        {
            return dataString.Any(character => !char.IsLetterOrDigit(character));
        }

        public static bool IsSpecialCharacter(char character)
        {
            return char.IsLetterOrDigit(character);
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

        public static string RemoveFaultyCharacterSequences(object stringObject)
        {
            string objectString = stringObject.ToString().Trim();

            objectString = objectString.Replace(",}", "}");
            objectString = objectString.Replace(",]", "]");

            return objectString;
        }

        public static string FormatJSON(string unformattedData, int padOrStrip)
        {
            try
            {
                switch (padOrStrip)
                {
                    case 0:
                        {
                            int start = unformattedData.IndexOf('[');
                            int end = unformattedData.IndexOf("]");

                            unformattedData = unformattedData.Substring(start, end - start);
                            break;
                        }
                    case 1:
                        {
                            unformattedData = "[" + unformattedData + "]";
                            break;
                        }
                    case 2:
                        {
                            unformattedData = "{" + unformattedData + "}";
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            JArray returnedDataArray = JArray.Parse(unformattedData);
            return returnedDataArray.ToString();
        }












        public static string FormatJSONObject(string unformattedData)
        {
            string formattedData = RemoveEscapeCharacters(unformattedData);
            formattedData = RemoveFaultyCharacterSequences(formattedData);
            //formattedData = formattedData.Replace('[', '[');
            //formattedData = formattedData.Replace(']', ']');

            try
            {
                object dynamicObject = new object();
                dynamicObject = JsonSerializer.Deserialize<dynamic>(formattedData);

                string dynamicObjectString = dynamicObject.ToString().Replace(" ", "");
                int dynamicObjectStringLength = Math.Min(dynamicObjectString.Length, 20);

                string dynamicObjStrPrefix = dynamicObjectString.Substring(0, dynamicObjectStringLength);
                string dynamicObjStrSuffix = "";

                if (dynamicObjectString.Length > 20)
                {
                    int lengthDiff = Math.Min(dynamicObjectString.Length - 20, 3);
                    dynamicObjStrSuffix = dynamicObjectString.Substring(dynamicObjectString.Length - lengthDiff, lengthDiff);
                }

                if (dynamicObject.GetType() == typeof(JArray))
                {
                    Console.WriteLine($"Object is PASSED JArray: {dynamicObjStrPrefix + " ... " + dynamicObjStrSuffix}");
                }
                else if (dynamicObject.GetType() == typeof(JObject))
                {
                    Console.WriteLine($"Object is PASSED JObject: {dynamicObjStrPrefix + " ... " + dynamicObjStrSuffix}");
                }
                else if (dynamicObject.GetType() == typeof(JToken))
                {
                    Console.WriteLine($"Object is PASSED JToken: {dynamicObjStrPrefix + " ... " + dynamicObjStrSuffix}");
                }
                else if (dynamicObject.GetType() == typeof(JsonElement))
                {
                    Console.WriteLine($"Object is JsonElement: {dynamicObjStrPrefix + " ... " + dynamicObjStrSuffix}");



                    //JToken jToken = (JToken)dynamicObject;





                    if (((JsonElement)dynamicObject).ValueKind == JsonValueKind.Array)
                    {
                        JArray objectArray = JArray.Parse(dynamicObject.ToString());

                        for (int i = 0; i < objectArray.Count; i++)
                        {
                            FormatJSONObject(objectArray[i].ToString());
                        }
                    }
                    else
                    {
                        JObject jObject = JObject.Parse(dynamicObject.ToString());

                        //IJEnumerable<JToken> objectCollection = jObject.Children().Children();


                        //foreach (JToken child in jObject.Children())
                        //{
                        //    if (child.Children().Count() > 1)
                        //    {
                        //        string childObjectString = "{" + child.ToString() + "}";
                        //        FormatJSONObject(childObjectString);
                        //    }

                        //    Console.WriteLine($"Object is JToken: {child.ToString()}");
                        //}


                        //int i = 0;


                        if (jObject.Children().Any())
                        {
                            for (int i = 0;  i < jObject.Children().Count(); i++)
                            {
                                JToken childObject = jObject.Children().ElementAt(i);

                                string childObjectString = childObject.ToString();

                                if (!childObjectString.StartsWith('{'))
                                {
                                    childObjectString = "{" + childObjectString;
                                }

                                if (!childObjectString.EndsWith('}'))
                                {
                                    childObjectString = childObjectString + "}";
                                }


                                //i++;
                                FormatJSONObject(childObjectString);
                                //i--;

                                Console.WriteLine($"Object is JToken: {i.ToString() + ": " + jObject.ToString()}");
                            }

                                //Console.WriteLine($"Object is JToken: {i.ToString() + ": " + jObject.ToString()}");
                        }


                        //foreach (JToken child in objectCollection)
                        //{
                        //    FormatJSONObject(child.ToString());
                        //}

                        //for (int i = 0; i < objectCollection.Count(); i++)
                        //{
                        //    FormatJSONObject(objectCollection[0].ToString());
                        //}
                    }
                }
                else
                {
                    Console.WriteLine($"Object is Other: {dynamicObjStrPrefix + " ... " + dynamicObjStrSuffix}");


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

        //    try
        //    {
        //        object dynamicObject = new object();
        //        dynamicObject = JsonSerializer.Deserialize<dynamic>(formattedData);

        //        string dynamicObjectString = dynamicObject.ToString().Replace(" ", "");
        //        int dynamicObjectStringLength = Math.Min(dynamicObjectString.Length, 20);


        //        if (dynamicObject.GetType() == typeof(JArray))
        //        {
        //            Console.WriteLine($"Object is JArray: {(dynamicObject.ToString().Replace(" ", "")).Substring(0, dynamicObjectStringLength) + "..."}");
        //        }
        //        else if (dynamicObject.GetType() == typeof(JObject))
        //        {
        //            Console.WriteLine($"Object is JObject: {(dynamicObject.ToString().Replace(" ", "")).Substring(0, dynamicObjectStringLength) + "..."}");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Object is Other: {(dynamicObject.ToString().Replace(" ", "")).Substring(0, dynamicObjectStringLength) + "..."}");

        //            JsonElement jsonElement = (JsonElement)dynamicObject;

        //            if (jsonElement.ValueKind == JsonValueKind.Array)
        //            {
        //                JArray objectArray = JArray.Parse(jsonElement.ToString());
        //                Console.WriteLine($"Object is Now JArray: {(RemoveEscapeCharacters(objectArray.ToString()).Replace(" ", "")).Substring(0, dynamicObjectStringLength) + "..."}");

        //                foreach (JToken obj in objectArray)
        //                {
        //                    FormatJSONObject(obj.ToString());
        //                }
        //            }
        //            else if (jsonElement.ValueKind == JsonValueKind.String || jsonElement.ValueKind == JsonValueKind.Number || jsonElement.ValueKind == JsonValueKind.Object)
        //            {
        //                JObject jObject = JObject.Parse(jsonElement.ToString());
        //                Console.WriteLine($"Object is Now JObject: {(RemoveEscapeCharacters(jObject.ToString()).Replace(" ", "")).Substring(0, dynamicObjectStringLength) + "..."}");

        //                if (jObject != null && jObject.Count > 1)
        //                {
        //                    string JObjectString = jObject.ToString();
        //                    JObjectString = JObjectString.Substring(1);
        //                    JObjectString = JObjectString.Remove(JObjectString.Length - 1);
        //                    JObjectString = "[" + JObjectString.ToString() + "]";

        //                    FormatJSONObject(JObjectString);


        //                    //foreach (KeyValuePair<string, JToken?> obj in jObject)
        //                    //{
        //                    //    string JObjectString = obj.ToString();
        //                    //    JObjectString = JObjectString.Substring(1);
        //                    //    JObjectString = JObjectString.Remove(JObjectString.Length - 1);
        //                    //    JObjectString = "{" + JObjectString.ToString() + "}";

        //                    //    //FormatJSONObject(JObjectString);
        //                    //    //JObject object = token.ToObject<JObject>();

        //                    //    JToken jToken = obj.Value;

        //                    //    FormatJSONObject(jToken.ToString());
        //                    //}
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine("Error: Object cannot be Parsed.");
        //            }
        //        }
        //        //JArray objectArray = JArray.Parse("werw");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }

        //    return formattedData;
        //}



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



    }
}