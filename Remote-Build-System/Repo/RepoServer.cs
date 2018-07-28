///////////////////////////////////////////////////////////////////////
// Repo.cs                                                           //
// ver 1.0                                                           //
// Language:    C#, 2017, .Net Framework 4.5                         //
// Platform:    Windows 10                                           //
// Application: CSE681 Project #4, Fall 2017                         //
// Author:      Weitian Ding , Syracuse University                   //
//              wding07@syr.edu                                      //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * get test needed files. 
 * build XML file. 
 * send files to build server.
 * has fortest function to run auto test
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
using FileManager;
using System.Xml.Linq;


namespace Repository
{
   public class Repo
    {

        Receiver recvr;
        Sender sndr = new Sender("http://localhost", 8082);

        int rcvrport = 8082;
        int clientport = 8080;
        int motherport = 8085;
        string motherAddress = "http://localhost:8085/IMessagePassingComm";
        string motherStorage = "../../../MotherBuild/motherStorage";
        string repoLogStorage = "../../../Repo/LogStorage";

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();
        FileMgr fileMgr = new FileMgr();
        List<CommMessage> tempList = new List<CommMessage>();
        string tempXML;

        public Repo()
        {
            fileMgr.storagePath = "../../../Repo/FileStorage";
            if (!Directory.Exists(fileMgr.storagePath))
                Directory.CreateDirectory(fileMgr.storagePath);
            if (!Directory.Exists(repoLogStorage))
                Directory.CreateDirectory(repoLogStorage);
        }

       
        bool transfer()
        {
            bool flag = true;
            try
            {                
                sndr.createSendChannel(motherAddress);
                Thread.Sleep(300);   
                foreach (var msg in tempList)
                {
                    msg.show();
                    Thread.Sleep(500);
                    if (sndr.postFile(msg.driver, fileMgr.storagePath, motherStorage) == true)
                    {
                        Console.Write("\n" + msg.driver);
                        Console.Write("\n send testdriver successfully");
                        Console.Write("\n  ====================");
                    }
                    else
                    {
                        flag = false;
                        Console.Write("\n" + msg.driver);
                        Console.Write("\n sending testdriver fails ");
                        Console.Write("\n  ====================");
                    }
                    foreach (var args in msg.arguments)
                    {
                        Thread.Sleep(500);
                        if (sndr.postFile(args, fileMgr.storagePath, motherStorage))
                        {
                            Console.Write("\n" + args);
                            Console.Write("\n send arguments successfully");
                            Console.Write("\n  ====================");
                        }
                        else
                        {
                            flag = false;
                            Console.Write("\n" + args);
                            Console.Write("\n sending arguments fails ");
                            Console.Write("\n  ====================");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n\n The error reason in transfer is {0}\n\n", ex.Message);
            }
            return flag;
        }

        public void fortest(CommMessage msg)
        {
            msg.show();
            sndr.postMessage(msg);
        }


        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {

                fileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = "http://localhost:" + clientport + "/IMessagePassingComm";
                reply.from = "http://localhost:" + rcvrport + "/IMessagePassingComm";
                reply.command = "getTopFiles";
                reply.arguments = fileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;
            Func<CommMessage, CommMessage> XML = (CommMessage msg) =>
            {
                tempList.Add(msg);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = "http://localhost:" + clientport + "/IMessagePassingComm";
                reply.from = "http://localhost:" + rcvrport + "/IMessagePassingComm";
                reply.command = "receive";
                reply.driver = Path.GetFileName(msg.driver);
                return reply;
            };
            messageDispatcher["XML"] = XML;
           
            Func<CommMessage, CommMessage> XMLend = (CommMessage msg) =>
            {
                fileMgr.doc= new XDocument();
                fileMgr.msgList = tempList;
                fileMgr.makeRequest();
                if (fileMgr.saveXml()){                
                    try{                    
                        CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                        reply.to = "http://localhost:" + clientport + "/IMessagePassingComm";
                        reply.from = "http://localhost:" + rcvrport + "/IMessagePassingComm";
                        reply.command = "XMLDone";
                        reply.show();
                        sndr.postMessage(reply);
                        if (transfer()){                        
                            tempXML = fileMgr.tempXML;
                            CommMessage request = new CommMessage(CommMessage.MessageType.build);
                            request.to = "http://localhost:" + motherport + "/IMessagePassingComm";
                            request.from = "http://localhost:" + rcvrport + "/IMessagePassingComm";
                            request.command = tempList.Count().ToString();
                            request.XML = tempXML;  
                            tempList = new List<CommMessage>();                     
                            return request;
                        }
                        else return null;
                    }
                    catch (Exception ex){                    
                        Console.Write("\n\n The error reason in XMLend is {0}\n\n", ex.Message);
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            };
            messageDispatcher["XMLend"] = XMLend;
        }


        static void Main(string[] args)
        {
            Console.Title = "Repo";
            Console.Write("\n start listening \n ");
            try
            {
                Repo Repo = new Repo();
                Repo.initializeDispatcher();
                Repo.recvr = new Receiver();
                Repo.recvr.start("http://localhost", Repo.rcvrport);

                while (true)
                {
                    CommMessage msg = Repo.recvr.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.type == CommMessage.MessageType.request)
                    {
                        if (msg.command == null)
                            continue;
                        CommMessage reply = Repo.messageDispatcher[msg.command](msg);
                        reply.show();
                        Repo.sndr.postMessage(reply);
                    }
                    if (msg.type == CommMessage.MessageType.build)
                    {
                        CommMessage request = Repo.messageDispatcher[msg.command](msg);
                        if (request != null)
                        {
                            request.show();
                            Repo.sndr.postMessage(request);
                        }
                    }
                    if (msg.type == CommMessage.MessageType.ready) {  
                        Repo.sndr.postMessage(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
