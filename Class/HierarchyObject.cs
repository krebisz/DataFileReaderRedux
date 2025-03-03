using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace DataFileReader.Class
{
    public class HierarchyObject
    {
        public int ID { get; set; }

        public int? ParentID { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public int? Level { get; set; }

        public KeyValuePair<string, JsonNode?> Element { get; set; }

        public Dictionary<string, Type> Fields { get; set; }

        public int? MetaDataID { get; set; }

        public string ClassID { get; set; }

        public HierarchyObject()
        {
            ID = 0;
            Name = String.Empty;
            Value = String.Empty;
            Fields = new Dictionary<string, Type>();
            ClassID = String.Empty;
        }

        public HierarchyObject(int id, string value, int? level, int? parentId)
        {
            ID = id;
            Name = String.Empty;
            Value = value;
            Level = level;
            ParentID = parentId;
            ClassID = String.Empty;
            Fields = new Dictionary<string, Type>();
        }

        public HierarchyObject(int id, string name, string value, int? level, int? parentId)
        {
            ID = id;
            Name = name;
            Value = value;
            Level = level;
            ParentID = parentId;
            ClassID = String.Empty;
            Fields = new Dictionary<string, Type>();
        }



        public void GenerateMetaDataID()
        {
            MetaDataID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        }
    }



    public class HierarchyObjectList
    {
        public List<HierarchyObject> hierarchyObjectList { get; set; }

        public HierarchyObjectList()
        {
            hierarchyObjectList = new List<HierarchyObject>();
        }


        public void GenerateMetaIDs()
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

    }
}