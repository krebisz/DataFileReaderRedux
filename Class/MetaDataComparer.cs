namespace DataFileReader.Class
{
    public class MetaDataComparer : IEqualityComparer<MetaData>
    {
        public bool Equals(MetaData? x, MetaData? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Fields.Count == y.Fields.Count && !x.Fields.Except(y.Fields).Any();
        }

        public int GetHashCode(MetaData obj)
        {
            return obj.Fields.OrderBy(kv => kv.Key).Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode()));
        }
    }
}