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

    public class MetaDataList
    {
        public MetaDataList()
        {
            MetaDataObjects = new List<MetaData>();
        }

        public MetaDataList(List<MetaData> metaDataList)
        {
            MetaDataObjects = metaDataList;
        }

        public List<MetaData> MetaDataObjects { get; set; }

        public List<string> ElementsList = new List<string>();

        public DataTable FlattenData(HierarchyObjectList hierarchyObjectList)
        {
            return FlattenData(hierarchyObjectList.HierarchyObjects);
        }

        public DataTable FlattenData(List<HierarchyObject> hierarchyObjects)
        {
            DataTable flattenedData = new DataTable();

            if (ElementsList != null && ElementsList.Count > 0)
            {
                for (int i = 0; i < ElementsList.Count; i++)
                {
                    DataColumn column = new DataColumn(ElementsList.ElementAt(i).ToString(), typeof(string));
                    flattenedData.Columns.Add(column);
                }
            }

            string[] dataFields = new string[flattenedData.Columns.Count];
            string[] currentDataFields = new string[flattenedData.Columns.Count];

            //HIERARCHYOBJECTS SHOULD BE GUARANTEED TO BE SORTED BEFORE THE FOLLOWING:
            foreach (HierarchyObject hierarchyObject in hierarchyObjects)
            {
                DataRow flattenedDataRow = flattenedData.NewRow();
                //string? myData = flattenedDataRow.Field<string>(hierarchyObject.Name);

                if (hierarchyObject.ClassID == "Element")
                {
                    DataColumn? flattenedDataColumn = flattenedData.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == hierarchyObject.Name);

                    if (flattenedDataColumn != null)
                    {
                        flattenedDataRow[flattenedDataColumn] = hierarchyObject.Value;
                    }

                    //var myColumn = flattenedDataRow[]<DataColumn>().SingleOrDefault(col => col.ColumnName == "myColumnName");

                    flattenedData.Rows.Add(flattenedDataRow);
                }
            }

            for (int i = 0; i < flattenedData.Rows.Count; i++)
            {
                for (int j = 0; j < flattenedData.Columns.Count; j++)
                {
                    currentDataFields[j] = flattenedData.Rows[i][j].ToString();

                    if (!String.IsNullOrEmpty(currentDataFields[j]))
                    {
                        dataFields[j] = currentDataFields[j];
                    }
                    else
                    {
                        currentDataFields[j] = dataFields[j];
                        flattenedData.Rows[i][j] = currentDataFields[j];
                    }
                }
            }

            DataTable distinctTable = GetDistinctRows(flattenedData);

            return distinctTable;
        }

        //SORT OF DATATABLE IS ASSUMED
        public static int GetNonEmptyFieldsTotal(DataTable dataTable)
        {
            DataRow dataRow = dataTable.Rows[dataTable.Rows.Count - 1];

            int fieldTotal = GetNonEmptyFieldsTotal(dataRow);

            return fieldTotal;
        }

        public static int GetNonEmptyFieldsTotal(DataRow dataRow)
        {
            int fieldTotal = 0;

            for (int j = 0; j < dataRow.ItemArray.Length; j++)
            {
                if (!String.IsNullOrEmpty(dataRow[j].ToString()))
                {
                    fieldTotal++;
                }
            }

            return fieldTotal;
        }

        public static int GetNonEmptyFieldsTotal(object?[] rowArray)
        {
            int fieldTotal = 0;

            for (int j = 0; j < rowArray.Length; j++)
            {
                if (!String.IsNullOrEmpty(rowArray[j].ToString()))
                {
                    fieldTotal++;
                }
            }

            return fieldTotal;
        }

        public static DataTable GetDistinctRows(DataTable sourceTable)
        {
            int maxfieldTotal = GetNonEmptyFieldsTotal(sourceTable);

            DataTable newTable = sourceTable.Clone();

            List<object?[]> distinctRows = sourceTable.AsEnumerable().Select(row => row.ItemArray).Distinct(new RowComparer()).ToList(); // Apply the custom comparer

            foreach (object?[] rowArray in distinctRows)
            {
                int fieldTotal = GetNonEmptyFieldsTotal(rowArray);

                if (fieldTotal >= maxfieldTotal)
                {
                    newTable.Rows.Add(rowArray); // Add each distinct row (as an array) to the new DataTable
                }
            }

            return newTable;
        }
    }

    // Custom comparer to treat nulls and empty strings as equal
    public class RowComparer : IEqualityComparer<object[]>
    {
        public bool Equals(object[] x, object[] y)
        {
            if (x == null || y == null)
                return x == y; // Both are null, so they're equal

            // Compare each item in the row arrays
            for (int i = 0; i < x.Length; i++)
            {
                // Treat null and empty strings as equal
                if (string.IsNullOrEmpty(x[i]?.ToString()) && string.IsNullOrEmpty(y[i]?.ToString()))
                    continue;

                if (!object.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(object[] obj)
        {
            // A simple hash code implementation, ensuring that rows with null or empty values have the same hash code
            int hash = 0;
            foreach (var item in obj)
            {
                // Null and empty values are treated as the same
                hash = hash * 31 + (item == null || string.IsNullOrEmpty(item.ToString()) ? 0 : item.GetHashCode());
            }

            return hash;
        }
    }
}