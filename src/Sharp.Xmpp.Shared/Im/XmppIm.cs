using Sharp.Xmpp.Core;
using System;
using System.IO;

namespace Sharp.Xmpp.Im
{
    /// <summary>
    /// Implements the basic instant messaging (IM) and presence functionality.
    /// </summary>
    /// <remarks>For implementation details, refer to RFC 3921.</remarks>
    public class XmppIm
    {
        /// <summary>
        /// Provides access to the core facilities of XMPP.
        /// </summary>
        private XmppCore core;

        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Hostname property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Hostname property is being set
        /// and the value is the empty string.</exception>
        public string Hostname
        {
            get
            {
                return core.Hostname;
            }

            set
            {
                core.Hostname = value;
            }
        }

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The Port property is being
        /// set and the value is not between 0 and 65536.</exception>
        public int Port
        {
            get
            {
                return core.Port;
            }

            set
            {
                core.Port = value;
            }
        }

        /// <summary>
        /// The username with which to authenticate. In XMPP jargon this is known
        /// as the 'node' part of the JID.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Username property is being
        /// set and the value is null.</exception>
        /// <exception cref="ArgumentException">The Username property is being set
        /// and the value is the empty string.</exception>
        public string Username
        {
            get
            {
                return core.Username;
            }

            set
            {
                core.Username = value;
            }
        }

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        /// <exception cref="ArgumentNullException">The Password property is being
        /// set and the value is null.</exception>
        public string Password
        {
            get
            {
                return core.Password;
            }

            set
            {
                core.Password = value;
            }
        }

        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool Tls
        {
            get
            {
                return core.StartTls;
            }

            set
            {
                core.StartTls = value;
            }
        }

        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public string ValidateHost
        {
            get
            {
                return core.ValidationHost;
            }

            set
            {
                core.ValidationHost = value;
            }
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get
            {
                return core.IsEncrypted;
            }
        }

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid Jid
        {
            get
            {
                return core.Jid;
            }
        }

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public int DefaultTimeOut
        {
            get
            {
                return core.MillisecondsDefaultTimeout;
            }

            set
            {
                core.MillisecondsDefaultTimeout = value;
            }
        }

        /// <summary>
        /// Print XML stanzas for debugging purposes
        /// </summary>
        public bool DebugStanzas
        {
            get
            {
                return core.DebugStanzas;
            }

            set
            {
                core.DebugStanzas = value;
            }
        }

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected
        {
            get
            {
                return core.Connected;
            }
        }

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated
        {
            get
            {
                return core.Authenticated;
            }
        }

        /// <summary>
        /// A callback method to invoke when a request for a subscription is received
        /// from another XMPP user.
        /// </summary>
        public SubscriptionRequest SubscriptionRequest
        {
            get;
            set;
        }

        /// <summary>
        /// The event that is raised when a status notification from a contact has been
        /// received.
        /// </summary>
        public event EventHandler<StatusEventArgs> Status;

        /// <summary>
        /// The event that is raised when a chat message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been approved.
        /// </summary>
        public event EventHandler<SubscriptionApprovedEventArgs> SubscriptionApproved;

        /// <summary>
        /// The event that is raised when a subscription request made by the JID
        /// associated with this instance has been refused.
        /// </summary>
        public event EventHandler<SubscriptionRefusedEventArgs> SubscriptionRefused;

        /// <summary>
        /// The event that is raised when a user or resource has unsubscribed from
        /// receiving presence notifications of the JID associated with this instance.
        /// </summary>
        public event EventHandler<UnsubscribedEventArgs> Unsubscribed;

        /// <summary>
        /// The event that is raised when the roster of the user has been updated,
        /// i.e. a contact has been added, removed or updated.
        /// </summary>
        public event EventHandler<RosterUpdatedEventArgs> RosterUpdated;

        /// <summary>
        /// The event that is raised when an unrecoverable error condition occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// Initializes a new instance of the XmppIm.
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
        public XmppIm(string hostname, string username, string password, int port = 5222, bool tls = true, string validate = null)
        {
            core = new XmppCore(hostname, username, password, port, tls, validate);
            SetupEventHandlers();
        }

        /// <summary>
        /// Initializes a new instance of the XmppIm.
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
        public XmppIm(string hostname, int port = 5222, bool tls = true, string validate = null)
        {
            core = new XmppCore(hostname, port, tls, validate);
            SetupEventHandlers();
        }

        /// <summary>
        /// Establishes a connection to the XMPP server.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// a resource identifier will be assigned by the server.</param>
        /// <returns>The user's roster (contact list).</returns>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and the Tls property has
        /// been set to false.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public Roster Connect(string resource = null)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            // Call 'Initialize' method of each loaded extension.
            //foreach (XmppExtension ext in extensions)
            //{
            //    try
            //    {
            //        ext.Initialize();
            //    }
            //    catch (Exception e)
            //    {
            //        throw new XmppException("Initialization of " + ext.Xep + " failed.", e);
            //    }
            //}
            try
            {
                core.Connect(resource);

                // If no username has been providd, don't establish a session.
                if (Username == null)
                    return null;

                //// Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
                //EstablishSession();

                //// Retrieve user's roster as recommended (Refer to RFC 3921, Section 7.3).
                //Roster roster = GetRoster();

                //// Send initial presence.
                //SendPresence(new Presence());
                //return roster;

                return null;
            }
            catch (Exception e)
            {
                throw new IOException("Could not connect to the server", e);
            }
        }

        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and the Tls property has
        /// been set to false.</exception>
        /// <exception cref="IOException">There was a failure while writing to or reading
        /// from the network. If the InnerException is of type SocketExcption, use the
        /// ErrorCode property to obtain the specific socket error code.</exception>
        /// <exception cref="ObjectDisposedException">The XmppIm object has been
        /// disposed.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server, or resource binding failed, or the initialization
        /// of an XMPP extension failed.</exception>
        public void Autenticate(string username, string password)
        {
            username.ThrowIfNull("username");
            password.ThrowIfNull("password");
            core.Authenticate(username, password);

            // Establish a session (Refer to RFC 3921, Section 3. Session Establishment).
            //EstablishSession();

            // Retrieve user's roster as recommended (Refer to RFC 3921, Section 7.3).
            //Roster roster = GetRoster();

            // Send initial presence.
            //SendPresence(new Presence());
        }

        /// <summary>
        /// Sets up the event handlers for the events exposed by the XmppCore instance.
        /// </summary>
        private void SetupEventHandlers()
        {
            //core.Iq += (sender, e) => { OnIq(e.Stanza); };
            //core.Presence += (sender, e) =>
            //{
            //    // FIXME: Raise Error event if constructor raises exception?
            //    OnPresence(new Presence(e.Stanza));
            //};

            //core.Message += (sender, e) =>
            //{
            //    OnMessage(new Message(e.Stanza));
            //};

            core.Error += (sender, e) =>
            {
                Error.Raise(sender, new ErrorEventArgs(e.Exception));
            };
        }
    }
}