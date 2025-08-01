using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

public class HierarchyObject
{
    public HierarchyObject() : this(0, string.Empty, string.Empty, 0, null, string.Empty) { }

    //public HierarchyObject(string path, JToken jToken, string classID)
    //{
    //    ID = 0;
    //    Name = path;
    //    Value = jToken.ToString();
    //    Level = 0;
    //    ParentID = null;
    //    ClassID = classID;
    //    MetaDataID = null;
    //    Fields = new Dictionary<string, Type>();
    //    Element = new KeyValuePair<string, JsonNode?>(path, jToken as JsonNode);
    //}

    public HierarchyObject(int id, string name, string value, int? level, int? parentId, string classId)
    {
        ID = id;
        Name = name;
        Value = value;
        Level = level;
        ParentID = parentId;
        ClassID = classId;
        MetaDataID = null;
        Fields = new Dictionary<string, Type>();
        Element = new KeyValuePair<string, JsonNode?>(string.Empty, null);
    }

    public int ID { get; set; }

    public string Name { get; set; }

    public string Bucket { get; set; }

    public string Value { get; set; }

    public int? Level { get; set; }

    public int? ParentID { get; set; }

    public string ClassID { get; set; }

    public int? MetaDataID { get; set; }

    public string RefVal { get; set; }


    public Dictionary<string, Type> Fields { get; set; }

    public KeyValuePair<string, JsonNode?> Element { get; set; }

    public void GenerateMetaDataID()
    {
        MetaDataID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
    }
}