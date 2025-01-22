namespace DataFileReader.Class
{
    public class HierarchyObject
    {
        public int ID { get; set; }

        public int? ParentID { get; set; }


        public string Name { get; set; }

        public string Value { get; set; }

        public int? Level { get; set; }

        public HierarchyObject()
        {
            ID = 0;
        }

        public HierarchyObject(int id, string value, int? level, int? parentId)
        {
            ID = id;
            Value = value;
            Level = level;
            ParentID = parentId;
        }

        public HierarchyObject(int id, string name, string value, int? level, int? parentId)
        {
            ID = id;
            Name = name;
            Value = value;
            Level = level;
            ParentID = parentId;
        }
    }
}