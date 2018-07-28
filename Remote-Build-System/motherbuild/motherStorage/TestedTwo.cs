using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild
{
    public class TestedTwo
    {
        public void sayTwo()
        {
            Console.Write("\n  TestedTwo here");
        }
        public string id()
        {
            Type t = this.GetType();
            return t.FullName;
        }
    }
}
