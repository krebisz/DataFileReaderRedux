using System.Text.Json.Nodes;

namespace DataFileReader.Class;

public class HierarchyObject
{
    public HierarchyObject()
    {
        ID = 0;
        Name = string.Empty;
        Value = string.Empty;
        Level = 0;
        //ParentID = null;
        ClassID = string.Empty;
        //MetaDataID = null;
        Fields = new Dictionary<string, Type>();
        Element = new KeyValuePair<string, JsonNode?>();
    }

    public HierarchyObject(int id, string value, int? level, int? parentId)
    {
        ID = id;
        Name = string.Empty;
        Value = value;
        Level = level;
        ParentID = parentId;
        ClassID = string.Empty;
        //MetaDataID = null;
        Fields = new Dictionary<string, Type>();
        Element = new KeyValuePair<string, JsonNode?>();
    }

    public HierarchyObject(int id, string name, string value, int? level, int? parentId)
    {
        ID = id;
        Name = name;
        Value = value;
        Level = level;
        ParentID = parentId;
        ClassID = string.Empty;
        //MetaDataID = null;
        Fields = new Dictionary<string, Type>();
        Element = new KeyValuePair<string, JsonNode?>();
    }

    public HierarchyObject(int id, string name, string value, int? level, int? parentId, string classID)
    {
        ID = id;
        Name = name;
        Value = value;
        Level = level;
        ParentID = parentId;
        ClassID = classID;
        //MetaDataID = null;
        Fields = new Dictionary<string, Type>();
        Element = new KeyValuePair<string, JsonNode?>();
    }

    public int ID { get; set; }

    public string Name { get; set; }

    public string Value { get; set; }

    public int? Level { get; set; }

    public int? ParentID { get; set; }

    public string ClassID { get; set; }

    public int? MetaDataID { get; set; }

    public Dictionary<string, Type> Fields { get; set; }

    public KeyValuePair<string, JsonNode?> Element { get; set; }

    public void GenerateMetaDataID()
    {
        MetaDataID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
    }
}

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
}