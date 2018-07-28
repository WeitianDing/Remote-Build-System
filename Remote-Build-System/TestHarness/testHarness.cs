///////////////////////////////////////////////////////////////////////
// THServer.cs                                                       //                                                                //
// ver 1.0                                                           //
// Language:    C#, 2017, .Net Framework 4.5                         //
// Platform:    Windows 10                                           //
// Application: CSE681 Project #4, Fall 2017                         //
// Author:      Weitian Ding , Syracuse University                   //
//              wding07@syr.edu                                      //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 *   This package can:
 * - receive test request msg
 * - send test request to repo
 * - display test result 
 */

/* Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * BlockingQueue.cs, FileManager.cs
 * tester.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 Dec 2017
 * - First released
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using System.Threading;
using System.IO;
using System.Reflection;
using TestHarness;

namespace TestHarness
{
   
    public class testHarness
    {
        Receiver recvr;
        Sender sndr = new Sender("http://localhost", 9999);
        testRequestHandler THQueue;

        int rcvrport = 8087;
        string THAddress = "http://localhost:8087/IMessagePassingComm";
        string repoAddress = "http://localhost:8082/IMessagePassingComm";
        string repoLogStorage = "../../../Repo/LogStorage";
      
        CommMessage rcvdMsg;
        CommMessage TRQueueMsg;
        delegate void NewMessage(CommMessage msg);
        event NewMessage OneNewMessage;
        delegate void NewTRMessage(CommMessage msg);
        event NewTRMessage OneNewTRMessage;

        string THStorage;
        string THResultStorage;

        testHarness()
        {
            THStorage = "../../../TestHarness/thStorage";
            THResultStorage = "../../../TestHarness/thResultStorage";
            if (!Directory.Exists(THStorage))
                Directory.CreateDirectory(THStorage);
            if (!Directory.Exists(THResultStorage))
                Directory.CreateDirectory(THResultStorage);

        }

        public class testRequestHandler
        {
            public static SWTools.BlockingQueue<CommMessage> testQ { get; set; } = null;
            public testRequestHandler()
            {
                if (testQ == null)
                    testQ = new SWTools.BlockingQueue<CommMessage>();
            }
            public CommMessage MessageOut()
            {

                CommMessage msg = testQ.deQ();
                return msg;
            }
            public void MessageIn(CommMessage msg)
            {
                testQ.enQ(msg);

            }
        }

        void ThreadProc()
        {
            while (true)
            {
                rcvdMsg = recvr.getMessage();
                OneNewMessage(rcvdMsg);
            }
        }
        void ThreadProcforTR()
        {
            while (true)
            {
                TRQueueMsg = THQueue.MessageOut();
                OneNewTRMessage(TRQueueMsg);
            }
        }
       
        void sendToRepo(string output, string fileName)
        {
            string fileName_ = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string resultName = fileName_ + "Result.txt";
            StreamWriter sW = new StreamWriter(@THResultStorage + "/" + resultName);
            sW.WriteLine(output);
            sW.Close();
            Console.Write("\n \n sending test result to repo \n ================");
            sndr.createSendChannel(repoAddress);
            if (sndr.postFile(resultName, THResultStorage, repoLogStorage))
            {
                Console.Write("\n send " + resultName + " to " + repoLogStorage + " successfully \n ======================");

            }
            else
            {
                Console.Write("\n sending fails \n ================");
            }

        }
      
        void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);         
                    }
                    else
                    {
                        File.Delete(i.FullName);
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        Thread rcvThrd;
        Thread TRThrd;
        void OneNewTRMessageHandler(CommMessage msg)
        {
            try
            {
                Tester tst = new Tester();
                Thread t = tst.SelectConfigAndRun(THStorage);
                t.Join();
                tst.ShowTestResults();
                tst.UnloadTestDomain();
                string output = tst.results_;
                sendToRepo(output, msg.command);
                DelectDir(THStorage);
                Console.WriteLine("\n");
                Console.Write(output);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in OnNewTRMessageHandler is {0}\n\n", ex.Message);
            }
        }

        void OnNewMessageHandler(CommMessage msg)
        {
            try
            {
                if ((msg.type == CommMessage.MessageType.build))
                {
                    msg.show();
                    THQueue.MessageIn(msg);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in OnNewMessageHandler is {0}\n\n", ex.Message);
            }
        }
        static void Main(string[] args)
        {
            Console.Title = "Test Harness";
            Console.Write("\n start listening \n ");
            try
            {    
                testHarness tHServer = new testHarness();
                tHServer.OneNewMessage += tHServer.OnNewMessageHandler;
                tHServer.OneNewTRMessage += tHServer.OneNewTRMessageHandler;
             
                tHServer.recvr = new Receiver();
                tHServer.recvr.start("http://localhost", tHServer.rcvrport);
                tHServer.rcvThrd = new Thread(new ThreadStart(tHServer.ThreadProc));
                tHServer.rcvThrd.IsBackground = true;
                tHServer.rcvThrd.Start();
            
                tHServer.THQueue = new testRequestHandler();
                tHServer.TRThrd = new Thread(new ThreadStart(tHServer.ThreadProcforTR));
                tHServer.TRThrd.IsBackground = true;
                tHServer.TRThrd.Start();
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason  in  main()  is {0}\n\n", ex.Message);
            }
        }
    }
}
