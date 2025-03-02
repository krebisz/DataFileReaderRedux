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
            Fields = new Dictionary<string, Type>();
        }

        public HierarchyObject(int id, string value, int? level, int? parentId)
        {
            ID = id;
            Value = value;
            Level = level;
            ParentID = parentId;
            Fields = new Dictionary<string, Type>();
        }

        public HierarchyObject(int id, string name, string value, int? level, int? parentId)
        {
            ID = id;
            Name = name;
            Value = value;
            Level = level;
            ParentID = parentId;
            Fields = new Dictionary<string, Type>();
        }

        //public void GenerateID()
        //{
        //    Field = new Dictionary<string, string>();

        //    JsonNode? jDynamicObject = JsonNode.Parse(Value);

        //    for (int i = 0; i < jDynamicObject.AsObject().Count; i++)
        //    {
        //        KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);

        //        Field.Add(subObject.Key, subObject.Value.ToString());
        //    };

        //    ID = Field.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        //}

        public void GenerateMetaDataID()
        {
            //Fields = new Dictionary<string, string>();

            //JsonNode? jDynamicObject = JsonNode.Parse(Value);

            //for (int i = 0; i < jDynamicObject.AsObject().Count; i++)
            //{
            //    KeyValuePair<string, JsonNode?> subObject = jDynamicObject.AsObject().GetAt(i);

            //    Fields.Add(subObject.Key, subObject.Value);
            //};

            MetaDataID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        }

        public void GenerateHierarchyClassID(List<HierarchyObject> hierarchyList, int? Id = null)
        {
            if (Id == null)
            {
                Id = ID;
            }

            this.ClassID = this.ClassID + Id;
            //ID = Id.Value;

            HierarchyObject obj = hierarchyList.FirstOrDefault(x => x.ID == Id);

            if (obj != null)
            {
                GenerateHierarchyClassID(hierarchyList, obj.ParentID);
            }
        }

        public void GenerateClassID()
        {
        }

        public void GenerateID(KeyValuePair<string, JsonNode?> element)
        {
            //Field = element;
            //Field = Field.Add(element(x => x.Key, x => x.Value);
            //ID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        }
    }
}