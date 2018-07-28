///////////////////////////////////////////////////////////////////////
// Program.cs -   Build request handler queue                        //
// ver 1.0                                                           //
// Language:    C#, 2017, .Net Framework 4.5                         //
// Platform:    Windows 10                                           //
// Application: CSE681 Project #4, Fall 2017                         //
// Author:      Weitian Ding , Syracuse University                   //
//              wding07@syr.edu                                      //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 *   ---------------------------------------------------------
 *   -MessageOut: get the msg from a BRHandler instance
 *   -MessageIn: send the msg to a BRHandler instance

 */

/* Required Files:
 * ---------------
 * BlockingQueue.cs
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
using SWTools;
using MessagePassingComm;

namespace BRHandler
{
    public class BRHandler
    {
        public static SWTools.BlockingQueue<CommMessage> BRQ { get; set; } = null;
        public BRHandler()
        {
            if (BRQ == null)
                BRQ = new SWTools.BlockingQueue<CommMessage>();
        }
        public CommMessage MessageOut()
        {

            CommMessage msg = BRQ.deQ();
            return msg;
        }
        public void MessageIn(CommMessage msg)
        {
            BRQ.enQ(msg);

        }
    }
}
