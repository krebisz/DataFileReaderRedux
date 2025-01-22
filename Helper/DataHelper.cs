using DataFileReader.Class;
using Newtonsoft.Json.Linq;
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

        public static List<HierarchyObject> GetObjectHierarchy(JArray objectArray, int? level)
        {
            //TO MAKE A PARAMETER
            bool setNameByPath = true;


            level = level is null ? 0 : level.Value;

            if (level == 0)
            {
                ObjectHierarchylist.Add(new HierarchyObject(0, "Root", "", level));
            }

            level++;

            try
            {
                foreach (var obj in objectArray)
                {
                    object dynamicObject = new object();

                    string objectString = obj.ToString().Trim().Replace(" ", "");
                    objectString = obj.ToString().Trim().Replace("\r", "");
                    objectString = obj.ToString().Trim().Replace("\n", "");
                    objectString = obj.ToString().Trim().Replace("\t", "");

                    dynamicObject = JsonSerializer.Deserialize<dynamic>(objectString);
                    dynamicObject = "[" + dynamicObject + "]";

                    JArray subObjectArray = JArray.Parse(dynamicObject.ToString());

                    if (subObjectArray != null && subObjectArray.Count > 0 && subObjectArray.First() != null)
                    {
                        if (subObjectArray.First().GetType() == typeof(JObject))
                        {
                            foreach (var subObject in subObjectArray)
                            {
                                if (subObject.GetType() == typeof(JObject))
                                {
                                    IJEnumerable<JToken> subObjectValue = subObject.Values();

                                    int i = 0;

                                    foreach (var subValue in subObjectValue)
                                    {
                                        if ((subValue.Path).ToString().Split('.').Length > 1)
                                        {
                                            string hierarchyObjectName = (subValue.Path).ToString().Split('.')[1];

                                            if (setNameByPath)
                                            {
                                                hierarchyObjectName = subValue.Path.ToString();
                                            }

                                            ObjectHierarchylist.Add(new HierarchyObject(i, hierarchyObjectName, subValue.ToString(), level));
                                        }
                                        else
                                        {
                                            ObjectHierarchylist.Add(new HierarchyObject(i, subValue.ToString(), level));
                                        }

                                        if (subValue != null && subValue.HasValues)
                                        {
                                            JArray subArray = new JArray(subValue);

                                            GetObjectHierarchy(subArray, level);
                                            level--;
                                        }

                                        i++;
                                    }
                                }
                                else if (subObject.GetType() == typeof(JArray))
                                {
                                    JArray subArray = new JArray(subObject);

                                    GetObjectHierarchy(subArray, level);
                                    level--;
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

            return ObjectHierarchylist;
        }

    }
}
