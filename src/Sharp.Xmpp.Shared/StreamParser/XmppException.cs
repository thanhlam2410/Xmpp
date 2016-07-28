using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.StreamParser
{
    public class XmppException : Exception
    {
        public XmppException(string message, string xml)
            : base(message)
        {

        }
    }
}
