using System.Data;

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

            GetFields(headerLine, ',');
            GenerateID();
        }

        public MetaData(string name, string headerLine, int parentID)
        {
            ID = 0;
            ParentID = parentID;
            Name = name;
            Fields = new Dictionary<string, Type>();

            GetFields(headerLine, ',');
            GenerateID();
        }

        public MetaData(string name, string headerLine, char delimiter)
        {
            ID = 0;
            Name = name;
            Fields = new Dictionary<string, Type>();

            GetFields(headerLine, delimiter);
            GenerateID();
        }

        public void GenerateID()
        {
            ID = Fields.OrderBy(field => field.Key).Aggregate(0, (hash, field) => HashCode.Combine(hash, field.Key.GetHashCode(), field.Value.GetHashCode()));
        }

        public void GetFields(string headerLine, char delimiter)
        {
            string[] fieldNames = headerLine.Split(delimiter);

            foreach (string fieldName in fieldNames)
            {
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    Fields[fieldName.Replace(" ", string.Empty).Trim()] = typeof(string); // Default to type string
                }
            }
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

    //// Custom comparer to treat nulls and empty strings as equal
    //public class RowComparer : IEqualityComparer<object[]>
    //{
    //    public bool Equals(object[] x, object[] y)
    //    {
    //        if (x == null || y == null)
    //            return x == y; // Both are null, so they're equal

    //        // Compare each item in the row arrays
    //        for (int i = 0; i < x.Length; i++)
    //        {
    //            // Treat null and empty strings as equal
    //            if (string.IsNullOrEmpty(x[i]?.ToString()) && string.IsNullOrEmpty(y[i]?.ToString()))
    //                continue;

    //            if (!object.Equals(x[i], y[i]))
    //                return false;
    //        }

    //        return true;
    //    }

    //    public int GetHashCode(object[] obj)
    //    {
    //        // A simple hash code implementation, ensuring that rows with null or empty values have the same hash code
    //        int hash = 0;
    //        foreach (var item in obj)
    //        {
    //            // Null and empty values are treated as the same
    //            hash = hash * 31 + (item == null || string.IsNullOrEmpty(item.ToString()) ? 0 : item.GetHashCode());
    //        }

    //        return hash;
    //    }
    //}
}