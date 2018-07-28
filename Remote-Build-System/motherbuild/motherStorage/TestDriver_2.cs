using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild
{
    interface ITest
    {
        bool test();
        string id();
    }

    public class TestDriver : ITest
    {
        public bool test()
        {
            TestedOne_1 one = new TestedOne_1();
            one.sayOne_1();
            TestedTwo_1 two = new TestedTwo_1();
            two.sayTwo_1();
            return true;
        }
        public string id()
        {
            Type t = this.GetType();
            return t.FullName;
        }
        static void Main(string[] args)
        {
            Console.Write("TestDriver_1 running");
            TestDriver td = new TestDriver();
            td.test();
            //Console.Write("");
        }
    }
}
