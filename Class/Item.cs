using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFileReader.Class
{
    public class Item
    {
        public string Name;
        public string Value;

        public Item()
        {

        }

        public Item(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
