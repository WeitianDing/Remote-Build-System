using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild
{
    public class TestedTwo_1
    {
        public void sayTwo_1()
        {
            Console.Write("\n  TestedTwo_1 here");
        }
        public string id()
        {
            Type t = this.GetType();
            return t.FullName;
        }
    }

}
