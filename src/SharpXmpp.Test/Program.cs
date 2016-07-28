using Sharp.Xmpp.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpXmpp.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XmppClient xmppClient = new XmppClient("chuyennm.mecorp.local", "user17", "123");
            xmppClient.Connect();

            Console.ReadLine();
        }
    }
}
