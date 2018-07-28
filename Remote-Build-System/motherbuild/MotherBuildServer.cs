///////////////////////////////////////////////////////////////////////
// MotherBuildServer.cs                                              //
// ver 1.0                                                           //
// Language:    C#, 2017, .Net Framework 4.5                         //
// Platform:    Windows 10                                           //
// Application: CSE681 Project #4, Fall 2017                         //
// Author:      Weitian Ding , Syracuse University                   //
//              wding07@syr.edu                                      //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package can:
 * using Communication channel                 
 * to get and post msg                        
 * handle the process pool                     
 * receive files from Repo Mock 
 
 */

/* Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * BlockingQueue.cs
 * BRHandler.cs
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
using System.ServiceModel;
using System.Diagnostics;
using System.IO;
using SWTools;
using BRHandler;
using FileManager;

namespace Mother
{
    public class MotherBuild
    {

        ChildBuilderHandler RMQueue;
        BRHandler.BRHandler BRQueue;
        Thread rcvThrd;
        Thread RMThrd;
        Thread BRThrd;
        Receiver recvr;
        Sender sndr = new Sender("http://localhost", 9999);

        int sndrport = 8090;
        int rcvrport = 8085;
        int tempport;
        int threadNum;
        List<string> templist;
        string motherStorage;
        string motherAddress = "http://localhost:8085/IMessagePassingComm";


        CommMessage rcvdMsg;
        CommMessage RMQueueMsg;
        CommMessage BRQueueMsg;
        CommMessage cRcvrMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
        CommMessage cSndrMsg = new CommMessage(CommMessage.MessageType.closeSender);


        Dictionary<int, bool> ReadyList = null;


        delegate void NewRMMessage(CommMessage msg);
        event NewRMMessage OneNewRMMessage;
        delegate void NewBRMessage(CommMessage msg);
        event NewBRMessage OneNewBRMessage;
        delegate void NewMessage(CommMessage msg);
        event NewMessage OneNewMessage;


        MotherBuild(int Num)
        {
            createChildBuild(Num);
            motherStorage = "../../../MotherBuild/motherStorage";
            if (!Directory.Exists(motherStorage))
                Directory.CreateDirectory(motherStorage);
        }

        void createChildBuild(int num)
        {
            threadNum = num;

            ReadyList = new Dictionary<int, bool>();
            try
            {
                for (int i = 1; i <= threadNum; ++i)
                {
                    ReadyList.Add(i, true);
                    if (createProcess(i))
                    {
                        Console.Write("\n ==================Create " + "#" + i + "Child Builders==================");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in MotherBuildComm is {0}\n\n", ex.Message);
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

        void ThreadProcforRM()
        {
            while (true)
            {
                RMQueueMsg = RMQueue.MessageOut();
                OneNewRMMessage(RMQueueMsg);
            }
        }

        void ThreadProcforBR()
        {
            while (true)
            {
                BRQueueMsg = BRQueue.MessageOut();
                OneNewBRMessage(BRQueueMsg);
            }
        }

        void OneNewRMMessageHandler(CommMessage msg)
        {
            try
            {
                if (msg.type == CommMessage.MessageType.working)
                {
                    msg.show();
                    Console.Write("\n\n  ================== Child Builder" + "#" + msg.command + " is working ==================");
                    ReadyList[Int32.Parse(msg.command)] = false;
                }
                if (msg.type == CommMessage.MessageType.ready)
                {
                    msg.show();
                    Console.Write("\n\n ================== Child Builder" + "#" + msg.command + " is available ==================");
                    ReadyList[Int32.Parse(msg.command)] = true;
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason is {0}\n\n", ex.Message);
            }
        }

        void OneNewBRMessageHandler(CommMessage msg)
        {
            try
            {
                if (msg.type == CommMessage.MessageType.build)
                {
                    msg.show();
                    templist = new List<string>();
                    parseXML(msg);
                    foreach (var a in ReadyList)
                    {
                        if (a.Value == true)
                        {
                            tempport = sndrport + a.Key;
                            if (transfer())
                            {
                                msg.to = "http://localhost:" + tempport + "/IMessagePassingComm";
                                msg.from = motherAddress;
                                Console.Write("\n ================== Msg goes to: " + msg.to + "\n=================");
                                msg.show();
                                sndr.postMessage(msg); 
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in OneNewBRMessageHandler is {0}\n\n", ex.Message);
            }
        }

       
        void OneNewMessageHandler(CommMessage msg)
        {
            try
            {
                if (msg.type == CommMessage.MessageType.ready || msg.type == CommMessage.MessageType.working)
                {
                    RMQueue.MessageIn(msg);
                }
                if (msg.type == CommMessage.MessageType.request || msg.type == CommMessage.MessageType.closeChild || msg.type == CommMessage.MessageType.closeMother || msg.type == CommMessage.MessageType.build)
                {
                    BRQueue.MessageIn(msg);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in OneNewMessageHandler is {0}\n\n", ex.Message);
            }
        }
      
        void parseXML(CommMessage msg)
        {
            FileMgr fileMgr = new FileMgr();
            fileMgr.loadXml(msg.XML);
            int testNum = Int32.Parse(msg.command);
            for (int i = 0; i < testNum; i++)
            {
                string test = "test" + i;
                templist.Add(fileMgr.parse(test, "testDriver"));
                foreach (string item in fileMgr.parseList(test, "tested"))
                {
                    templist.Add(item);
                }
            }
        }

    
        public bool createProcess(int i)
        {
            Process proc = new Process();

            string fileName = "..\\..\\..\\ChildBuild\\bin\\Debug\\ChildBuild.exe";
            string absFileSpec = Path.GetFullPath(fileName);

            Console.Write("\n attempting to start {0}", absFileSpec);
            string commandline = i.ToString();
            try
            {
                Process.Start(fileName, commandline);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in createProcess is {0}\n\n", ex.Message);
                return false;
            }
            return true;
        }

      
        bool transfer()
        {
            bool flag = true;
            try
            {
                sndr.createSendChannel("http://localhost:" + tempport + "/IMessagePassingComm");
                Thread.Sleep(300);
                string childStorage = "../../../ChildBuild/ChildStorage_" + tempport;
                foreach (string item in templist)
                {
                    Thread.Sleep(400);
                    if (!sndr.postFile(item, motherStorage, childStorage))
                        flag = false;
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in transfer is {0}\n\n", ex.Message);
            }
            return flag;
        }


        static void Main(string[] args)
        {
            Console.Title = "Mother Builder";

            MotherBuild motherBuilder = new MotherBuild(Int32.Parse(args[0]));
            motherBuilder.OneNewMessage += motherBuilder.OneNewMessageHandler;
            motherBuilder.OneNewRMMessage += motherBuilder.OneNewRMMessageHandler;
            motherBuilder.OneNewBRMessage += motherBuilder.OneNewBRMessageHandler;

            try
            {
                motherBuilder.recvr = new Receiver();
                motherBuilder.recvr.start("http://localhost", motherBuilder.rcvrport);
                motherBuilder.rcvThrd = new Thread(new ThreadStart(motherBuilder.ThreadProc));
                motherBuilder.rcvThrd.IsBackground = true;
                motherBuilder.rcvThrd.Start();
                Console.Write("\n start listening \n ");
                Console.Write("====================The Number of Child Builders: " + motherBuilder.threadNum + "====================\n  ");

                motherBuilder.RMQueue = new ChildBuilderHandler();
                motherBuilder.RMThrd = new Thread(new ThreadStart(motherBuilder.ThreadProcforRM));
                motherBuilder.RMThrd.IsBackground = true;
                motherBuilder.RMThrd.Start();

                motherBuilder.BRQueue = new BRHandler.BRHandler();
                motherBuilder.BRThrd = new Thread(new ThreadStart(motherBuilder.ThreadProcforBR));
                motherBuilder.BRThrd.IsBackground = true;
                motherBuilder.BRThrd.Start();

            }

            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in main is {0}\n\n", ex.Message);
            }
            Console.ReadKey();
        }
        public class ChildBuilderHandler
        {
            public static SWTools.BlockingQueue<CommMessage> ChildQ { get; set; } = null;
            public ChildBuilderHandler()
            {
                if (ChildQ == null)
                    ChildQ = new SWTools.BlockingQueue<CommMessage>();
            }
            public CommMessage MessageOut()
            {

                CommMessage msg = ChildQ.deQ();
                return msg;
            }
            public void MessageIn(CommMessage msg)
            {
                ChildQ.enQ(msg);

            }
        }
    }
}
