using System.Data;
using static DataFileReader.Helper.DataHelper;

namespace DataFileReader.Class
{
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
    }
}