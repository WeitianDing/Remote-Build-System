///////////////////////////////////////////////////////////////////////
// FileManager.cs                                                    //
// ver 1.0                                                           //
// Language:    C#, 2017, .Net Framework 4.5                         //
// Platform:    Windows 10                                           //
// Application: CSE681 Project #4, Fall 2017                         //
// Author:      Weitian Ding , Syracuse University                   //
//              wding07@syr.edu                                      //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Creates and parses TestRequest XML messages using XDocument
 * get file list in storage path
 * build and parse test requests                  
 * load files                 
 * 
 *      
 * Required Files:
 * ---------------
 * IMessagePassingComm.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 dec 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using MessagePassingComm;
using System.Xml;

namespace FileManager
{
    public class FileMgr
    {

        public string currentPath { get; set; } = "";
        public Stack<string> pathStack { get; set; } = new Stack<string>();

        public List<string> files { get; set; } = new List<string>();
        public string storagePath { get; set; } = "";


        public string author { get; set; } = "Weitian Ding";
        public string dateTime { get; set; } = "";
        public string testDriver { get; set; } = "";
        public List<string> testedFiles { get; set; } = new List<string>();
        public XDocument doc { get; set; } = new XDocument();
        public List<CommMessage> msgList { get; set; } = new List<CommMessage>();
        public string tempXML { get; set; } = "";

        public void makeRequest()
        {
            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement authorElem = new XElement("author");
            authorElem.Add(author);
            testRequestElem.Add(authorElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            for (int i = 0; i < msgList.Count(); i++)
            {
                XElement testElem = new XElement("test" + i);
                testRequestElem.Add(testElem);

                XElement driverElem = new XElement("testDriver");
                driverElem.Add(msgList[i].driver);
                testElem.Add(driverElem);

                foreach (string file in msgList[i].arguments)
                {
                    XElement testedElem = new XElement("tested");
                    testedElem.Add(file);
                    testElem.Add(testedElem);
                }
            }
        }

        public FileMgr()
        {
            pathStack.Push(currentPath);
        }

        public bool loadXml(string path)
        {
            try
            {
                doc = XDocument.Parse(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        public bool saveXml()
        {
            try
            {
                tempXML = doc.ToString();
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        public string parse(string test, string propertyName)
        {
            IEnumerable<XElement> e = from el in doc.Descendants(test) select el;
            string parseStr = e.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "author":
                        author = parseStr;
                        break;
                    case "dateTime":
                        dateTime = parseStr;
                        break;
                    case "testDriver":
                        testDriver = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";

        }

        public List<string> parseList(string test, string propertyName)
        {
            List<string> values = new List<string>();
            IEnumerable<XElement> e = from el in doc.Descendants(test) select el;
            IEnumerable<XElement> parseElems = e.Descendants(propertyName);

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        private void getFilesHelper(string path, string pattern)
        {
            string[] tempFiles = Directory.GetFiles(path, pattern);
            for (int i = 0; i < tempFiles.Length; ++i)
            {
                tempFiles[i] = Path.GetFullPath(tempFiles[i]);
            }
            files.AddRange(tempFiles);

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                getFilesHelper(dir, pattern);
            }
        }

        public void getFiles(string pattern)
        {
            files.Clear();
            getFilesHelper(storagePath, pattern);
        }


        public List<string> getFiles()
        {
            List<string> files = new List<string>();
            string path = Path.Combine(storagePath, currentPath);
            string absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList<string>();
            for (int i = 0; i < files.Count(); ++i)
            {
                files[i] = Path.Combine(currentPath, Path.GetFileName(files[i]));
            }
            return files;
        }

        public bool setDir(string dir)
        {
            if (!Directory.Exists(dir))
                return false;
            currentPath = dir;
            return true;
        }

        static void Main(string[] args)
        {
        }
    }


}
