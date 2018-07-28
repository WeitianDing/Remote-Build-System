///////////////////////////////////////////////////////////////////////
// ChildBuildServer.cs -                                             //
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
 * - send working message and ready message to mother builder
 * - receive build request message
 * - send test request to testHarness
 * - build and generate DLL
 * - parse XML file
 *   
 */

/* Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * BlockingQueue.cs, FileManager.cs
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
using System.Diagnostics;
using System.IO;
using MessagePassingComm;
using System.Threading;
using System.IO;
using FileManager;

namespace Child
{
    class ChildBuild
    {

        Thread rcvThrd;
        Receiver recvr;
        Sender sndr = new Sender("http://localhost", 9999);

        int rcvrport = 8090;
        int rcvrport_number;
        bool global;
        string repoAddress = "http://localhost:8082/IMessagePassingComm";
        string motherAddress = "http://localhost:8085/IMessagePassingComm";
        string THAddress = "http://localhost:8087/IMessagePassingComm";

        string childstorage;
        string THStorage = "../../../TestHarness/thStorage";
        string logStorage = "../../../Repo/LogStorage";


        CommMessage rcvdMsg;
        CommMessage sendworkingMsg;
        CommMessage sendreadyMsg;
        CommMessage cRcvrMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
        CommMessage cSndrMsg = new CommMessage(CommMessage.MessageType.closeSender);
        FileMgr fileMgr;

        delegate void NewMessage(CommMessage msg);
        event NewMessage OnNewMessage;

        ChildBuild(int i)
        {
            rcvrport_number = i;
            int port = rcvrport + i;
            childstorage = "../../../ChildBuild/ChildStorage_" + port;
            if (!Directory.Exists(childstorage))
                Directory.CreateDirectory(childstorage);
        }


        void ThreadProc()
        {
            while (true)
            {
                rcvdMsg = recvr.getMessage();
                OnNewMessage(rcvdMsg);
            }
        }

        void parseXML(CommMessage msg)
        {
            fileMgr = new FileMgr();
            fileMgr.loadXml(msg.XML);
            int testNum = Int32.Parse(msg.command);

            for (int i = 0; i < testNum; i++)
            {
                string test = "test" + i;
                string testdriver = fileMgr.parse(test, "testDriver");
                string driverName_ = Path.GetFileNameWithoutExtension(testdriver);
                List<string> argslist = new List<string>();
                foreach (string item in fileMgr.parseList(test, "tested"))
                {
                    argslist.Add(item);
                }
                if (Build(testdriver, argslist))
                {
                    Console.Write("\n ==============Sending logs to repo ==================\n ");
                    logToRepo(driverName_);
                }
                else
                {
                    Console.Write("\n ==============Build files are missing==================\n ");
                }
                string dllname = driverName_ + ".dll";
                string dllpath = Path.Combine(childstorage, dllname);
                if (File.Exists(dllpath))
                {
                    Console.Write("\n==============" + testdriver + " builds successfully==============");
                    sndr.createSendChannel(THAddress);
                    Thread.Sleep(300);
                    Console.Write("\n ==============Sending dll to testharness ==================\n ");
                    if (sndr.postFile(dllname, childstorage, THStorage))
                    {
                        Console.Write("\n================== Send files successfully================== \n ");
                        CommMessage build = new CommMessage(CommMessage.MessageType.build);
                        build.from = "# " + rcvrport_number + " child builder";
                        build.to = THAddress;
                        build.command = dllname;
                        sndr.postMessage(build);
                    }
                    else
                    {
                        Console.Write("\n ================== Send file failed. ==================\n ");
                    }
                }
                else
                {
                    Console.Write("\n==============" + testdriver + " build fails==============");
                }
            }
            global = true;
        }


        bool Build(string testdriver, List<string> argslist)
        {
            bool flag = true;
            List<string> buildList = new List<string>();
            buildList = argslist;
            buildList.Insert(0, testdriver);
            fileMgr.storagePath = childstorage;
            fileMgr.getFiles("*.*");
            string csfile = "";
            foreach (string item in buildList)
            {
                csfile += " " + item;
            }
            Console.Write("\n ============== cs files are:   " + csfile + "==================\n ");
            List<string> storedFile = new List<string>();
            try
            {
                foreach (string file in fileMgr.files)
                {
                    string fileName = Path.GetFileName(file);
                    storedFile.Add(fileName);
                }
                if (buildList.All(b => storedFile.Any(a => a == (b))))
                {
                    Console.Write("\n=================== Start building " + csfile + "==================\n ");
                    Process p = new Process();
                    p.StartInfo.FileName = "cmd.exe";
                    string driverName_ = Path.GetFileNameWithoutExtension(testdriver);
                    p.StartInfo.Arguments = @"/Ccsc /target:library " + csfile;
                    p.StartInfo.WorkingDirectory = @childstorage;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                    p.WaitForExit();
                    string errors = p.StandardError.ReadToEnd();
                    string output = p.StandardOutput.ReadToEnd();
                    StreamWriter sW = new StreamWriter(@childstorage + "/" + driverName_ + "buildlog.txt");
                    sW.WriteLine(string.Concat(output, errors));
                    sW.Close();
                    Console.Write($"\n  Build Errors: {errors}");
                    Console.Write($"\n  Build Outout: {output}");
                    Console.Write("\n =================== Building ends ==================\n");
                }
                else flag = false; ;
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason  in  build()  is {0}\n\n", ex.Message);
            }
            return flag;
        }

        void logToRepo(string driverName_)
        {
            try
            {
                fileMgr.getFiles("*.*");
                foreach (string file in fileMgr.files)
                {
                    string fileName = Path.GetFileName(file);
                    string logname = driverName_ + "buildlog.txt";
                    if (fileName == logname)
                    {
                        sndr.createSendChannel(repoAddress);
                        Thread.Sleep(300);
                        if (sndr.postFile(logname, childstorage, logStorage))
                        {
                            Console.Write("\n ============== Send log to repo     logname: " + logname);
                        }
                        else
                        {
                            Console.Write("\n============== sending log fails==============");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason  in  logToRepo()  is {0}\n\n", ex.Message);
            }
        }

        void OnNewMessageHandler(CommMessage msg)
        {
            try
            {
                if (msg.type == CommMessage.MessageType.build)
                {
                    msg.show();
                    sendworkingMsg = new CommMessage(CommMessage.MessageType.working);
                    sendworkingMsg.command = rcvrport_number.ToString();
                    sendworkingMsg.from = "#" + rcvrport_number + " child builder";
                    sendworkingMsg.to = motherAddress;
                    sendworkingMsg.show();
                    Console.Write("\n ==============Childbuilder working message has sent ==================\n ");
                    sndr.postMessage(sendworkingMsg);
                    global = false;
                    parseXML(msg);
                    if (global)
                    {
                        sendreadyMsg = new CommMessage(CommMessage.MessageType.ready);
                        sendreadyMsg.command = rcvrport_number.ToString();
                        sendreadyMsg.from = "#" + rcvrport_number + " child builder";
                        sendreadyMsg.to = motherAddress;
                        sendreadyMsg.show();
                        Thread.Sleep(300);
                        Console.Write("\n ==============Childbuilder ready message has sent ==================\n ");
                        sndr.postMessage(sendreadyMsg);
                    }
                }
                if (msg.type == CommMessage.MessageType.closeChild)
                {
                    Console.Write("\n============== Childbuilder close message has received ==================\n ");
                    sndr.postMessage(cSndrMsg);
                    recvr.postMessage(cRcvrMsg);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in OnNewMessageHandler is {0}\n\n", ex.Message);
            }
        }


        static void Main(string[] args)
        {
            Console.Title = "ChildProc";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            try
            {
                if (args.Count() == 0)
                {
                    Console.Write("\n  please enter integer value on command line");
                    return;
                }
                else
                {
                    ChildBuild childBuilder = new ChildBuild(Int32.Parse(args[0]));
                    childBuilder.OnNewMessage += childBuilder.OnNewMessageHandler;
                    Console.Write("\n  Child Process", args[0]);
                    Console.Write("\n  Hello from child #{0}\n\n", args[0]);
                    Console.Write("\n ====================");

                    childBuilder.recvr = new Receiver();
                    int receiverport = childBuilder.rcvrport + childBuilder.rcvrport_number;
                    childBuilder.recvr.start("http://localhost", receiverport);

                    Console.Write("\n http://localhost:" + receiverport);
                    childBuilder.rcvThrd = new Thread(new ThreadStart(childBuilder.ThreadProc));
                    childBuilder.rcvThrd.IsBackground = true;
                    childBuilder.rcvThrd.Start();
                    Console.Write("\n start listening");
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason  in  main()  is {0}\n\n", ex.Message);
            }
        }
    }
}

