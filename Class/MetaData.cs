namespace DataFileReader.Class
{
    public class MetaData
    {
        public int ID { get; set; }

        public int ParentID { get; set; }

        public string Name { get; set; }

        public string RefVal { get; set; }

        public string Type { get; set; }



        public Dictionary<string, Type> Fields { get; set; }

        public MetaData()
        {
            ID = 0;
            ParentID = -1;
            Name = string.Empty;
            Fields = new Dictionary<string, Type>();
        }

        public MetaData(string name)
        {
            ID = 0;
            ParentID = -1;
            Name = name;
            Fields = new Dictionary<string, Type>();
        }

        public MetaData(string name, string headerLine)
        {
            ID = 0;
            ParentID = -1;
            Name = name;
            Fields = new Dictionary<string, Type>();

            string[] fieldNames = headerLine.Split(',');

            foreach (string fieldName in fieldNames)
            {
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    Fields[fieldName.Replace(" ", string.Empty).Trim()] = typeof(string); // Default to type string
                }
            }

            GenerateID();
        }

        public MetaData(string name, string headerLine, int parentID)
        {
            ID = 0;
            ParentID = parentID;
            Name = name;
            Fields = new Dictionary<string, Type>();

            string[] fieldNames = headerLine.Split(',');

            foreach (string fieldName in fieldNames)
            {
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    Fields[fieldName.Replace(" ", string.Empty).Trim()] = typeof(string); // Default to type string
                }
            }

            GenerateID();
        }

        public MetaData(string name, string headerLine, char delimiter)
        {
            ID = 0;
            Name = name;
            Fields = new Dictionary<string, Type>();

            string[] fieldNames = headerLine.Split(delimiter);

            foreach (string fieldName in fieldNames)
            {
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    Fields[fieldName.Replace(" ", string.Empty).Trim()] = typeof(string); // Default to type string
                }
            }

            GenerateID();
        }

        public void GenerateID()
        {
            ID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        }

        public void UpdateFieldType(string fieldName, Type fieldType)
        {
            if (Fields.ContainsKey(fieldName))
            {
                Fields[fieldName] = fieldType;
            }
            else
            {
                throw new KeyNotFoundException($"Field '{fieldName}' not found in mappings.");
            }
        }
    }
}