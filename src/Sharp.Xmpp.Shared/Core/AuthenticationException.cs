using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Core
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message)
            : base(message)
        {

        }
    }
}
