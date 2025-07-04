namespace DataFileReader.Class;

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