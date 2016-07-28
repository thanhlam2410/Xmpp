using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Client
{
    /// <summary>
    /// Implements an XMPP client providing basic instant messaging (IM) and
    /// presence functionality as well as various XMPP extension functionality.
    /// </summary>
    /// <remarks>
    /// This provides most of the functionality exposed by the XmppIm class but
    /// simplifies some of the more complicated aspects such as privacy lists and
    /// roster management. It also implements various XMPP protocol extensions.
    /// </remarks>
    public class XmppClient
    {
        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The instance of the XmppIm class used for implementing the basic messaging
        /// and presence funcionality.
        /// </summary>
        private XmppIm im;

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        public string Hostname
        {
            get
            {
                return im.Hostname;
            }

            set
            {
                im.Hostname = value;
            }
        }

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        public int Port
        {
            get
            {
                return im.Port;
            }

            set
            {
                im.Port = value;
            }
        }

        /// <summary>
        /// The username with which to authenticate. In XMPP jargon this is known
        /// as the 'node' part of the JID.
        /// </summary>
        public string Username
        {
            get
            {
                return im.Username;
            }

            set
            {
                im.Username = value;
            }
        }

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        public string Password
        {
            get
            {
                return im.Password;
            }

            set
            {
                im.Password = value;
            }
        }

        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool Tls
        {
            get
            {
                return im.Tls;
            }

            set
            {
                im.Tls = value;
            }
        }

        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public string ValidationHost
        {
            get
            {
                return im.ValidateHost;
            }

            set
            {
                im.ValidateHost = value;
            }
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get
            {
                return im.IsEncrypted;
            }
        }

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid Jid
        {
            get
            {
                return im.Jid;
            }
        }

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected
        {
            get
            {
                if (im != null)
                {
                    return im.Connected;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated
        {
            get
            {
                return im.Authenticated;
            }
        }

        /// <summary>
        /// The default IQ Set Time out in Milliseconds. -1 means no timeout
        /// </summary>
        public int DefaultTimeOut
        {
            get { return im.DefaultTimeOut; }
            set { im.DefaultTimeOut = value; }
        }

        /// <summary>
        /// If true prints XML stanzas
        /// </summary>
        public bool DebugStanzas
        {
            get { return im.DebugStanzas; }
            set { im.DebugStanzas = value; }
        }

        /// <summary>
        /// The underlying XmppIm instance.
        /// </summary>
        public XmppIm Im
        {
            get
            {
                return im;
            }
        }

        /// <summary>
        /// Initializes a new instance of the XmppClient class.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="username">The username with which to authenticate. In XMPP jargon
        /// this is known as the 'node' part of the JID.</param>
        /// <param name="password">The password with which to authenticate.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <exception cref="ArgumentNullException">The hostname parameter or the
        /// username parameter or the password parameter is null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter or the username
        /// parameter is the empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        /// <remarks>Use this constructor if you wish to connect to an XMPP server using
        /// an existing set of user credentials.</remarks>
        public XmppClient(string hostname, string username, string password, int port = 5222, bool tls = true, string validationHost = null)
        {
            im = new XmppIm(hostname, username, password, port, tls, validationHost);
            // Initialize the various extension modules.
            //LoadExtensions();
        }

        /// <summary>
        /// Initializes a new instance of the XmppClient class.
        /// </summary>
        /// <param name="hostname">The hostname of the XMPP server to connect to.</param>
        /// <param name="port">The port number of the XMPP service of the server.</param>
        /// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
        /// supports TLS/SSL-encryption.</param>
        /// <param name="validate">A delegate used for verifying the remote Secure Sockets
        /// Layer (SSL) certificate which is used for authentication. Can be null if not
        /// needed.</param>
        /// <exception cref="ArgumentNullException">The hostname parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The hostname parameter is the empty
        /// string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
        /// is not a valid port number.</exception>
        /// <remarks>Use this constructor if you wish to register an XMPP account using
        /// the in-band account registration process supported by some servers.</remarks>
        public XmppClient(string hostname, int port = 5222, bool tls = true, string validationHost = null)
        {
            im = new XmppIm(hostname, port, tls, validationHost);
            //LoadExtensions();
        }

        /// <summary>
        /// Establishes a connection to the XMPP server.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// a resource identifier will be assigned by the server.</param>
        /// <returns>The user's roster (contact list).</returns>
        /// <exception cref="System.Security.Authentication.AuthenticationException">An
        /// authentication error occured while trying to establish a secure connection, or
        /// the provided credentials were rejected by the server, or the server requires
        /// TLS/SSL and the Tls property has been set to false.</exception>
        /// <exception cref="System.IO.IOException">There was a failure while writing to or
        /// reading from the network. If the InnerException is of type SocketExcption, use
        /// the ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppClient object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void Connect(string resource = null)
        {
            im.Connect(resource);
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="System.Security.Authentication.AuthenticationException">
        /// An authentication error occured while trying to establish a secure connection,
        /// or the provided credentials were rejected by the server, or the server requires
        /// TLS/SSL and the Tls property has been set to false.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void Authenticate(string username, string password)
        {
            im.Autenticate(username, password);
        }
    }
}
