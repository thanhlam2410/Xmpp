using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Core
{
    public enum TlsError
    {
        ServerFailure,
        ValidationError,
        TlsRequired
    }

    public static class TlsErrorExtension
    {
        public static string GetMessage(this TlsError error)
        {
            string message = string.Empty;

            switch (error)
            {
                case TlsError.ServerFailure:
                    message = "Server response failure";
                    break;
                case TlsError.ValidationError:
                    message = "Cannot established TLS connection with given Validation Host";
                    break;
                case TlsError.TlsRequired:
                    message = "The server requires TLS/SSL.";
                    break;
                default:
                    break;
            }

            return message;
        }
    }
}
