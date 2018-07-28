/////////////////////////////////////////////////////////////////////
// Tester.cs - Test Harness Test Aggregator                        //
// ver 1.1                                                         //
//                                                                 //
// Platform:    Dell Dimension 8300, Windows XP Pro, SP 2.0        //
// Application: CSE784 - Software Studio, Final Project Prototype  //
// Author:      Jim Fawcett, Syracuse University, CST 2-187        //
//              jfawcett@twcny.rr.com, (315) 443-3948              //
/////////////////////////////////////////////////////////////////////
/*
 * Module Operations:
 * ==================
 * This module provides operations to Create a child Application Domain,
 * load libraries into it, and run tests on all loaded libraries that
 * support the ITest interface. 
 * 
 * In order to load libraries without requiring the Tester to bind to
 * the types they declare, a Loader library is defined that is loaded
 * into the child domain, and loads each of the test libraries from
 * within the child. 
 * 
 * Test configurations are defined by the set of all libraries found
 * in a configuration directory.  Each configuration runs on its own
 * thread.  Test results are returned as a private XML string.
 * 
 * Public Interface:
 * =================
 * Tester tstr = new Tester();
 * Thread t = tstr.SelectConfigAndRun("TestLibraries");
 * tstr.ShowTestResults();
 * tstr.UnloadTestDomain();
 */
/*
 * Build Process:
 * ==============
 * Files Required:
 *   Tester.cs, Loader.cs, ITest.cs, Test1.cs, Tested1.cs, ...
 * Compiler Command:
 *   csc /t:Library Loader.cs
 *   csc /t:Library ITest.cs
 *   csc /t:Library Test1.cs, Tested1.cs, ...
 *   csc /t:exe     Tester.cs
 * Deployment:
 *   Loader.dll --> TesterDir/Loader
 *   ITest.dll  --> TesterDir/TestLibraries
 *   Test dlls  --> TesterDir/TestLibraries
 *   where TesterDir is the folder containing Tester.exe
 *  
 * Maintence History:
 * ==================
 * ver 1.1 : 17 Oct 05
 *   - uses new version of Loader.cs.  Otherwise unchanged.
 * ver 1.0 : 09 Oct 05
 *   - first release
 * 
 */
//
using System;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Security.Policy;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;

namespace TestHarness
{
    public class Tester
    {
        string pathToTestLibs_ = "TestLibraries";  // ITest.dll and test libs
        string pathToLoader_ = "Loader";           // Loader.dll
        AppDomain ad;
         public string results_;

        //----< Constructor placeholder >--------------------------------

        public Tester()
        {
        }
        //----< Create AppDomain in which to run tests >-----------------

        void CreateAppDomain()
        {
            AppDomainSetup domainInfo = new AppDomainSetup();
            domainInfo.ApplicationName = "TestDomain";
            Evidence evidence = AppDomain.CurrentDomain.Evidence;
            ad = AppDomain.CreateDomain("TestDomain", evidence, domainInfo);
            ad.AppendPrivatePath(pathToLoader_);
        }
        //----< Load Loader and tests, run tests, unload AppDomain >-----

        void LoadAndRun()
        {
            Console.Write("\n\n  Loading and instantiating Loader in TestDomain");
            Console.Write("\n ------------------------------------------------");
            ad.Load("Loader");
            ObjectHandle oh = ad.CreateInstance("loader", "TestHarness.Loader");
            Loader ldr = oh.Unwrap() as Loader;
            ldr.SetPath(pathToTestLibs_);
            ldr.LoadTests();
            results_ = ldr.RunTests();
        }
        //
        //----< Run tests in configDir >---------------------------------

        void runTests()
        {
            try
            {
                CreateAppDomain();
                LoadAndRun();
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
            }
            Console.Write("\n");
        }
        //----< unload Child AppDomain >---------------------------------

       public void UnloadTestDomain()
        {
            AppDomain.Unload(ad);
        }
        //
        //----< show test results >--------------------------------------

       public void  ShowTestResults()
        {
            Console.Write("\n  Test Results returned to Tester");
            Console.Write("\n ---------------------------------\n");

            Console.Write("\n  {0}\n", results_);
            //if (results_ == null)
            //  results_ = "<TestResults>no results for this test</TestResults>";
            StringReader tr = new StringReader(results_);
            XmlTextReader xtr = new XmlTextReader(tr);
            xtr.MoveToContent();
            if (xtr.Name != "TestResults")
                throw new Exception("invalid test results: " + results_);
            int count = 0;
            string name = "", text = "";
            while (xtr.Read())
            {
                if (xtr.NodeType == XmlNodeType.Element)
                    name = xtr.Name;
                if (xtr.NodeType == XmlNodeType.Text)
                {
                    if (xtr.Value == "True")
                        text = "passed";
                    else
                        text = "failed";
                    ++count;
                    Console.Write("\n  Test #{0}: {1} - {2}", count, text, name);
                    results_ += "\r\n Test#" + count + " : " + text + " - " + name;
                }
            }
            ///////////////////////////////////////////////////////
            // alternate way to display results
            // count = 0;
            // while(true)
            // {
            //   ++count;
            //   string id = (string)ad.GetData(count.ToString());
            //   if(id == null)
            //     break;
            //   Console.Write("\n  {0,20} : passed = {1}",id,ad.GetData(id));
            // }

            Console.Write("\n\n");
            
        }
        //
        //----< run configuration on its own thread >--------------------

        public Thread SelectConfigAndRun(string configDir)
        {
            pathToTestLibs_ = configDir;
            Thread t = new Thread(new ThreadStart(this.runTests));
            t.Start();
            return t;
        }
        //----< demonstrate Test Harness Prototype >---------------------

        static void Main(string[] args)
        {
            Console.Write(
              "\n  Tester, ver 1.1 - Demonstrates Prototype TestHarness"
            );
            Console.Write(
              "\n ======================================================"
            );
            Tester tstr = new Tester();
            Thread t = tstr.SelectConfigAndRun("TestLibraries");
            t.Join();
            tstr.ShowTestResults();
            tstr.UnloadTestDomain();
            //      Console.ReadLine();
        }
    }
}
