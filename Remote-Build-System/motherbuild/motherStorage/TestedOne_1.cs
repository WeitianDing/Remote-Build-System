using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild
{
  public class TestedOne_1
  {
    public void sayOne_1()
    {
      Console.Write("\n  TestedOne_1 here");
    }
        public string id()
        {
            Type t = this.GetType();
            return t.FullName;
        }
    }
}
