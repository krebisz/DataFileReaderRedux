namespace DataFileReader.Class
{
    public class Item
    {
        public string Name;
        public string Value;

        public Item()
        {
            Name = String.Empty;
            Value = String.Empty;
        }

        public Item(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}