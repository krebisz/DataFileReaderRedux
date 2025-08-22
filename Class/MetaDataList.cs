using System.Data;
using System.Text;
using static DataFileReader.Helper.DataHelper;

namespace DataFileReader.Class
{
    public class MetaDataList
    {
        public MetaDataList()
        {
            MetaDataObjects = new List<MetaData>();
        }

        public MetaDataList(HierarchyObjectList HierarchyObjectList)
        {
            MetaDataObjects = new List<MetaData>();

            HierarchyObjectList.GenerateMetaIDs();

            foreach (var hierarchyObject in HierarchyObjectList.HierarchyObjects)
            {
                int? referenceValue = null;
                //hierarchyObject.ReferenceValue = GetMetaDataObjectReferenceValue(HierarchyObjectList.HierarchyObjects, hierarchyObject.ID, ref referenceValue).ToString();
                hierarchyObject.ReferenceValue = GetMetaDataObjectReferenceValue(HierarchyObjectList.HierarchyObjects, hierarchyObject.ID);
                if (string.IsNullOrEmpty(hierarchyObject.Name)) hierarchyObject.Name = Guid.NewGuid().ToString();

                var metaData = new MetaData();

                Type type = hierarchyObject.Value.GetType();

                metaData.Fields.Add(hierarchyObject.Value, type);
                metaData.GenerateID();

                metaData.Name = hierarchyObject.Name;
                metaData.Type = hierarchyObject.ClassID;
                metaData.ReferenceValue = hierarchyObject.ReferenceValue;

                if (metaData.Type != "Element")
                {
                    MetaData? existingMetaData = MetaDataObjects.FirstOrDefault(x => x.ReferenceValue == metaData.ReferenceValue);

                    //if (existingMetaData is null)
                    //{
                    MetaDataObjects.Add(metaData);
                    //}
                }
                else
                {
                    var existingElement = ElementsList.FirstOrDefault(x => x == metaData.Name);

                    if (existingElement is null)
                    {
                        ElementsList.Add(metaData.Name);
                    }
                }
            }
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

                if (hierarchyObject.ClassID == "Element")
                {
                    DataColumn? flattenedDataColumn = flattenedData.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == hierarchyObject.Name);

                    if (flattenedDataColumn != null)
                    {
                        flattenedDataRow[flattenedDataColumn] = hierarchyObject.Value;
                    }

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

        public static int? GetMetaDataObjectReferenceValue(List<HierarchyObject> HierarchyObjectList, int hierarchyObjectID, ref int? referenceValue)
        {
            if (referenceValue == null) referenceValue = 0;

            var hierarchyObject = HierarchyObjectList.FirstOrDefault(x => x.ID == hierarchyObjectID);

            if (hierarchyObject != null)
            {
                referenceValue = referenceValue + hierarchyObject.MetaDataID;

                if (hierarchyObject.ParentID != null && hierarchyObject.ParentID > -1) GetMetaDataObjectReferenceValue(HierarchyObjectList, (int)hierarchyObject.ParentID, ref referenceValue);
            }

            return referenceValue;
        }

        public static string GetMetaDataObjectReferenceValue(List<HierarchyObject> HierarchyObjectList, int ID)
        {
            string referenceValue = string.Empty;
            StringBuilder reference = new StringBuilder();

            HierarchyObject hierarchyObject = HierarchyObjectList.FirstOrDefault(x => x.ID == ID);
            HierarchyObject hierarchyObjectParent = HierarchyObjectList.FirstOrDefault(x => x.ID == hierarchyObject?.ParentID);

            if (hierarchyObjectParent != null && hierarchyObjectParent.ReferenceValue != null)
            {
                reference.Append(hierarchyObjectParent.ReferenceValue);
            }
            if (hierarchyObject != null && hierarchyObject.ID != null)
            {
                reference.Append(hierarchyObject.ID);
            }

            return reference.ToString();
        }
    }
}