///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs                                                //
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
 * This packages can:
 * -select files
 * -create build request
 * -send build request
 * -receive msg
 */

/* Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * 
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using FileManager;
using System.Threading;
using MessagePassingComm;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Client_Gui
{
    
    public partial class MainWindow : Window
    {
        string repoAddress = "http://localhost:8082/IMessagePassingComm";
        string motherAddress = "http://localhost:8085/IMessagePassingComm";
        string clientAddress = "http://localhost:8080/IMessagePassingComm";

        string fileName;
        string fileSpec;
        string filePath;
        string testDriver;
        string RepoStorage = "../../../Repo/repoStorage/";
        int rcvrport = 8080;
        FileMgr fileMgr = new FileMgr();
        List<string> selectedFiles = new List<string>();
        List<CommMessage> tempList = new List<CommMessage>();

        CommMessage rcvdMsg;
        CommMessage GuiMsg = new CommMessage(CommMessage.MessageType.request);
        CommMessage XMLMsg;
        CommMessage BuildMsg = new CommMessage(CommMessage.MessageType.build);
        CommMessage closeRcvrMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
        CommMessage closeSndrMsg = new CommMessage(CommMessage.MessageType.closeSender);
        CommMessage closeChildMsg = new CommMessage(CommMessage.MessageType.closeChild);
        CommMessage closeMotherMsg = new CommMessage(CommMessage.MessageType.closeMother);

        Receiver Guircvr;
        Sender Guisndr = new Sender("http://localhost", 9999);

        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();

        Thread rcvThread = null;

        public MainWindow()
        {
           
            InitializeComponent();

            if (!Directory.Exists(RepoStorage))
                Directory.CreateDirectory(RepoStorage);
            fileMgr.storagePath = RepoStorage;
            initializeMessageDispatcher();
            Guircvr = new Receiver();
            Guircvr.start("http://localhost", rcvrport);
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();            
        }
        
       

        void initializeMessageDispatcher()
        {
          
            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {
                remoteFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFiles.Items.Add(file);
                }
            };
          
            messageDispatcher["receive"] = (CommMessage msg) =>
            {
                Notification.Text += "\n" + msg.driver + " and related files received";
            };
           
            messageDispatcher["XMLDone"] = (CommMessage msg) =>
            {
                Notification.Text += "\n" + "XML string built in repo";
            };           
           
            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
            {
                remoteFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFiles.Items.Add(file);
                }
            };           
        }

        void rcvThreadProc()
        {           
            Console.Write("\n  starting client's receive thread");
            while (true)
            {  
                CommMessage msg = Guircvr.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;
                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }
       
        private void remoteFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFiles.Clear();
            ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);   
            foreach (var files in remoteFiles.SelectedItems)
            {
                fileName = files.ToString();
                fileSpec = System.IO.Path.Combine(RepoStorage, fileName);
                filePath = System.IO.Path.GetFullPath(fileSpec);
                selectedFiles.Add(filePath);
            }
        }

        private void remoteFiles_SelectedItems(object sender, RoutedEventArgs e)
        {

        }


        private void Window_Closed(object sender, EventArgs e)
        {
           
        }
      
        private void testdriver_Click(object sender, RoutedEventArgs e)
        {
             XMLMsg = new CommMessage(CommMessage.MessageType.request);
            if (selectedFiles.Count() == 1)
            {
                testDriver = selectedFiles[0];
            }
            testDriver = System.IO.Path.GetFileName(testDriver);
            XMLMsg.author = "Weitian Ding";
            XMLMsg.driver = testDriver;
            XMLMsg.command = "XML";
            XMLMsg.from = clientAddress;
            XMLMsg.to = repoAddress;
            selectionList.Text += "\n" + testDriver + " selected";
            XMLMsg.show();
        }
    
        private void arguments_Click(object sender, RoutedEventArgs e)
        {
            List<string> arguments = new List<string>();
            foreach (var item in selectedFiles)
            {
                arguments.Add(System.IO.Path.GetFileName(item));
            }

            XMLMsg.author = "Weitian Ding";
            XMLMsg.driver = testDriver;
            XMLMsg.command = "XML";
            XMLMsg.from = clientAddress;
            XMLMsg.to = repoAddress;
            XMLMsg.arguments = arguments;
            string temp = "";
            foreach (var item in arguments)
            {
                temp += "\n" + item + " selected";
            }
            selectionList.Text += temp;
            XMLMsg.show();           
        }
      
        private void remoteFresh_Click(object sender, RoutedEventArgs e)
        {

            GuiMsg.author = "Weitian Ding";
            GuiMsg.driver = "";
            GuiMsg.command = "getTopFiles";

            GuiMsg.from = clientAddress;
            GuiMsg.to = repoAddress;
            GuiMsg.arguments.Clear();
            Guisndr.postMessage(GuiMsg);
        }
       
        private void confirm_Click(object sender, RoutedEventArgs e)
        {
            tempList.Add(XMLMsg);
            Notification.Text += "\n" + XMLMsg.driver +"  ";
            foreach (var item in XMLMsg.arguments)
            {
                Notification.Text+=  item  ;
            }
            
            Notification.Text += "\n confirm";           
            Console.Write("\n =====================");
            foreach (var item in tempList) {
                item.show();
            }
        }
       
        private void send_Click(object sender, RoutedEventArgs e)
        {
            foreach (var msg in tempList)
            {
                Guisndr.postMessage(msg);
                Thread.Sleep(200);
            }
            BuildMsg.author = "Weitian Ding";
            BuildMsg.driver = "";
            BuildMsg.command = "XMLend";
            BuildMsg.from = clientAddress;
            BuildMsg.to = repoAddress;
            BuildMsg.arguments.Clear();
            Guisndr.postMessage(BuildMsg);
            tempList= new List<CommMessage>();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
       
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }
        //confirm process pool and create it
        private void child_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int threadNum;
                if (IsTextAllowed(TextBox1.Text) == true)
                {
                    threadNum = Int32.Parse(TextBox1.Text);
                    if (threadNum <= 0)
                    {
                        TextBox1.Text = "Please enter integer bigger than 0";
                    }
                    if (threadNum < 11 && threadNum > 0)
                    {
                        
                        string motherName = "..\\..\\..\\MotherBuild\\bin\\Debug\\MotherBuild.exe";
                        Process.Start(motherName, threadNum.ToString());
                       Notification.Text += TextBox1.Text + " Child Builders Opened";                     
                    }
                    else
                    {
                        TextBox1.Text = "Please enter integer less than 10";
                    }
                }
                else
                {
                    TextBox1.Text = "Please enter integer only";
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n The error reason of open child builders is \n" + ex.Message);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
