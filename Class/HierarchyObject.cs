using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFileReader.Class
{
    public class HierarchyObject
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public int? Level { get; set; }
        public HierarchyObject() 
        { 
            ID = 0;
        }

        public HierarchyObject(int id, string value, int? level)
        {
            ID = id;
            Value = value;
            Level = level;
        }

        public HierarchyObject(int id, string name, string value, int? level)
        {
            ID = id;
            Name = name;
            Value = value;
            Level = level;
        }
    }
}
