using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace DataFileReader.Class;

public class HierarchyObjectList
{
    public HierarchyObjectList()
    {
        HierarchyObjects = new List<HierarchyObject>();
    }

    public List<HierarchyObject> HierarchyObjects { get; set; }

    public void GenerateMetaIDs()
    {
        foreach (var hierarchyObject in HierarchyObjects)
        {
            var type = hierarchyObject.Value.GetType();

            if (string.IsNullOrEmpty(hierarchyObject.Name)) hierarchyObject.Name = Guid.NewGuid().ToString();

            hierarchyObject.Fields.Add(hierarchyObject.Value, type);
            hierarchyObject.GenerateMetaDataID();
        }
    }

    public void Add(string path, JToken jToken, string classID)
    {
        HierarchyObject hierarchyObject = new HierarchyObject();

        string[] pathParts = path.Split('.');

        hierarchyObject.ID = HierarchyObjects.Count + 1; // Simple ID generation
        hierarchyObject.Name = pathParts.Last();
        hierarchyObject.ParentID = FindParentID(pathParts); // Default parent ID, can be adjusted later
        hierarchyObject.Level = FindLevel(hierarchyObject.ParentID); // Default level, can be adjusted later
        hierarchyObject.ClassID = classID;
        hierarchyObject.Path = path;


        if (classID == "Container")
        {
            foreach (var child in jToken.Children<JProperty>())
            {
                string childName = child.Name; // This is the property name
                hierarchyObject.Value += $"{childName}; ";
            }
        }
        else if (classID == "Array")
        {
            for (int i = 0; i < jToken.Children().Count(); i++)
            {
                hierarchyObject.Value += $"{hierarchyObject.Name}[{i}]; ";
            }
        }
        else
        {
            hierarchyObject.Value = jToken.ToString();
        }

        hierarchyObject.MetaDataID = null; // Default MetaDataID, can be set later
        hierarchyObject.Fields = new Dictionary<string, Type>();
        hierarchyObject.GenerateMetaDataID();

        if (!isDuplicate(hierarchyObject))
        {
            HierarchyObjects.Add(hierarchyObject);
        }
    }

    public int? FindParentID(string[] pathParts)
    {
        string objectName = pathParts.Last();
        string? parentName = pathParts.Length > 1 ? pathParts[pathParts.Length - 2] : null;

        if (parentName is null)
        {
            string[] parts = Regex.Split(objectName, @"\[.*?\]");
            // If the last part is an index, we need to find the parent name from the previous part
            if (parts.Length > 1)
            {
                parentName = parts[parts.Length - 2];
            }
        }
        if (parentName is null)
        {
            return null; // No parent name found
        }

        return HierarchyObjects.FirstOrDefault(h => h.Name == parentName).ID;
    }

    public int? FindLevel(int? parentID)
    {
        int level = 0;

        if (parentID != null)
        {
            level = (int)HierarchyObjects.FirstOrDefault(h => h.ID == parentID).Level + 1;
        }

        return level;
    }

    public bool isDuplicate(HierarchyObject hierarchyObject)
    {
        //return HierarchyObjects.Any(h => h.MetaDataID == hierarchyObject.MetaDataID && h.Name == hierarchyObject.Name && h.ParentID == hierarchyObject.ParentID);
        return HierarchyObjects.Any(h => h.MetaDataID == hierarchyObject.MetaDataID && h.Name == hierarchyObject.Name && h.ParentID == hierarchyObject.ParentID && hierarchyObject.Path == h.Path);
    }
}