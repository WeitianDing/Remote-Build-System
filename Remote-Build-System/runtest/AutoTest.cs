using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository;
using MessagePassingComm;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace runtest
{
    class AutoTest
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "AUTO TEST";
                Console.Write("Auto Test start");
                int threadNum = 3;
                string motherName = "..\\..\\..\\MotherBuild\\bin\\Debug\\MotherBuild.exe";
                Process.Start(motherName, threadNum.ToString());
                string repoName = "..\\..\\..\\Repo\\bin\\Debug\\Repo.exe";
                Process.Start(repoName);
                string thName = "..\\..\\..\\TestHarness\\bin\\Debug\\TestHarness.exe";
                Process.Start(thName);
                Thread.Sleep(600);
               
                Sender testSndr = new Sender("http://localhost", 9999);
                CommMessage testmsg = new CommMessage(CommMessage.MessageType.request);
                testmsg.author = "test";
                testmsg.command = "XML";
                testmsg.from = "test";
                testmsg.to = "http://localhost:8082/IMessagePassingComm";
                testmsg.driver = "TestDriver.cs";
                testmsg.arguments.Add("TestedOne.cs");
                testmsg.arguments.Add("TestedTwo.cs");
                testmsg.show();
                testSndr.postMessage(testmsg);
                Thread.Sleep(500);

                CommMessage testmsg_1 = new CommMessage(CommMessage.MessageType.request);
                testmsg_1.author = "test_1";
                testmsg_1.command = "XML";
                testmsg_1.from = "test";
                testmsg_1.to = "http://localhost:8082/IMessagePassingComm";
                testmsg_1.driver = "TestDriver_1.cs";
                testmsg_1.arguments.Add("TestedOne_1.cs");
                testmsg_1.arguments.Add("TestedTwo_1.cs");
                testmsg_1.show();
                testSndr.postMessage(testmsg_1);
                Thread.Sleep(500);

                CommMessage testmsg_2 = new CommMessage(CommMessage.MessageType.request);
                testmsg_2.author = "test_2";
                testmsg_2.command = "XML";
                testmsg_2.from = "test";
                testmsg_2.to = "http://localhost:8082/IMessagePassingComm";
                testmsg_2.driver = "TestDriver_2.cs";
                testmsg_2.arguments.Add("TestedOne_2.cs");
                testmsg_2.arguments.Add("TestedTwo_2.cs");
                testmsg_2.show();
                testSndr.postMessage(testmsg_2);
                Thread.Sleep(500);

                CommMessage testmsg_end = new CommMessage(CommMessage.MessageType.build);
                testmsg_end.command = "XMLend";
                testmsg_end.to = "http://localhost:8082/IMessagePassingComm";
                testmsg_end.show();
                testSndr.postMessage(testmsg_end);
            }
            catch (Exception ex){
                Console.Write("\n\n The error reason in auto test is {0}\n\n", ex.Message);
            }

        }
    }
}
