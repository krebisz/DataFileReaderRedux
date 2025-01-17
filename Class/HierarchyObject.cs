using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFileReader.Class
{
    public class HierarchyObject
    {
        public string Name { get; set; }
        public int? Level { get; set; }
        public HierarchyObject() 
        { 
        
        }

        public HierarchyObject(string name, int? level)
        {
            Name = name;
            Level = level;
        }
    }
}
