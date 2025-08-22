using DataFileReader.Class;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DataFileReader.Helper
{
    public static class JsoonHelper
    {
        public static List<string> GetFieldList(JArray jsonArray)
        {
            List<string> fieldList = new List<string>();

            foreach (var jsonToken in jsonArray)
            {
                string jsonString = DataHelper.RemoveEscapeCharacters(jsonToken.ToString());

                dynamic? dynamicObject = new object();
                dynamicObject = JsonSerializer.Deserialize<dynamic>(jsonString);
                dynamicObject = "[" + dynamicObject + "]";

                var childJsonArray = JArray.Parse(dynamicObject.ToString());

                if (childJsonArray != null && childJsonArray.Count > 0)
                {
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
            }

            return fieldList;
        }

        public static void CreateHierarchyObjectList(ref HierarchyObjectList hierarchyObjectList, JToken token, string path = "Root")
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                    hierarchyObjectList.Add(path, token, "Container");
                    CreateHierarchyObjectList(ref hierarchyObjectList, prop.Value, currentPath);
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    string currentPath = $"{path}[{i}]";
                    hierarchyObjectList.Add(path, token, "Array");
                    CreateHierarchyObjectList(ref hierarchyObjectList, array[i], currentPath);
                }
            }
            else
            {
                // Primitive value (string, number, bool, etc.)
                hierarchyObjectList.Add(path, token, "Element");
            }
        }
    }
}
