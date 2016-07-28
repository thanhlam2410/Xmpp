using Sharp.Xmpp.Core.Sasl;
using Sharp.Xmpp.StreamParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Sharp.Xmpp.Core
{
    /// <summary>
    /// Implements the core features of the XMPP protocol.
    /// </summary>
    /// <remarks>For implementation details, refer to RFC 3920.</remarks>
    public class XmppCore : IDisposable
    {
        #region Fields
        /// <summary>
        /// The TCP connection to the XMPP server.
        /// </summary>
        private SocketClient _client;

        /// <summary>
        /// The parser instance used for parsing incoming XMPP XML-stream data.
        /// </summary>
        private XmppParser _parser;
        #endregion

        #region Properties
        /// <summary>
        /// If true the session will be TLS/SSL-encrypted if the server supports it.
        /// </summary>
        public bool StartTls { get; set; }

        /// <summary>
        /// If true, server supports ssl stream
        /// </summary>
        public bool IsSupport { get; set; }

        /// <summary>
        /// If true, server requires stream must be secure with TLS
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// If true, connnection is secured
        /// </summary>
        public bool IsSslStream { get; set; }
        #endregion
        
        /// <summary>
        /// True if the instance has been disposed of.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Used for creating unique IQ stanza ids.
        /// </summary>
        private int id;

        /// <summary>
        /// The port number of the XMPP service of the server.
        /// </summary>
        private int port;

        /// <summary>
        /// The hostname of the XMPP server to connect to.
        /// </summary>
        private string hostname;

        /// <summary>
        /// The username with which to authenticate.
        /// </summary>
        private string username;

        /// <summary>
        /// The password with which to authenticate.
        /// </summary>
        private string password;

        /// <summary>
        /// The resource to use for binding.
        /// </summary>
        private string resource;

        /// <summary>
        /// Write lock for the network stream.
        /// </summary>
        private readonly object stanzaQueueLock = new object();

        /// <summary>
        /// The default Time Out for IQ Requests
        /// </summary>
        private int millisecondsDefaultTimeout = -1;

        /// <summary>
        /// The default value for debugging stanzas is false
        /// </summary>
        private bool debugStanzas = false;

        /// <summary>
        /// A thread-safe dictionary of wait handles for pending IQ requests.
        /// </summary>
        private Dictionary<string, AutoResetEvent> waitHandles = new Dictionary<string, AutoResetEvent>();

        /// <summary>
        /// A thread-safe dictionary of IQ responses for pending IQ requests.
        /// </summary>
        private Dictionary<string, Iq> iqResponses = new Dictionary<string, Iq>();

        /// <summary>
        /// A thread-safe dictionary of callback methods for asynchronous IQ requests.
        /// </summary>
        private Dictionary<string, Action<string, Iq>> iqCallbacks = new Dictionary<string, Action<string, Iq>>();

        /// <summary>
        /// A cancellation token source that is set when the listener threads shuts
        /// down due to an exception.
        /// </summary>
        private CancellationTokenSource cancelIq = new CancellationTokenSource();

        /// <summary>
        /// A FIFO of stanzas waiting to be processed.
        /// </summary>
        private Queue<Stanza> stanzaQueue = new Queue<Stanza>();

        /// <summary>
        /// A FIFO of message waiting to be processed.
        /// </summary>
        private Queue<XmppTree> messageQueue = new Queue<XmppTree>();

        /// <summary>
        /// A cancellation token source for cancelling the dispatcher, if neccessary.
        /// </summary>
        private CancellationTokenSource cancelDispatch = new CancellationTokenSource();

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
                return hostname;
            }

            set
            {
                value.ThrowIfNullOrEmpty("Hostname");
                hostname = value;
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
                return port;
            }

            set
            {
                value.ThrowIfOutOfRange("Port", 0, 65536);
                port = value;
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
                return username;
            }
            set
            {
                value.ThrowIfNullOrEmpty("Username");
                username = value;
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
                return password;
            }

            set
            {
                value.ThrowIfNull("Password");
                password = value;
            }
        }

        /// <summary>
        /// The resource to use for binding.
        /// </summary>
        public string Resource
        {
            get
            {
                return resource;
            }

            set
            {
                resource = value;
            }
        }

        /// <summary>
        /// The Default IQ Set /Request message timeout
        /// </summary>
        public int MillisecondsDefaultTimeout
        {
            get { return millisecondsDefaultTimeout; }
            set { millisecondsDefaultTimeout = value; }
        }

        /// <summary>
        /// Print XML stanzas for debugging purposes
        /// </summary>
        public bool DebugStanzas
        {
            get { return debugStanzas; }
            set { debugStanzas = value; }
        }
        
        /// <summary>
        /// A delegate used for verifying the remote Secure Sockets Layer (SSL)
        /// certificate which is used for authentication.
        /// </summary>
        public string ValidationHost
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether the session with the server is TLS/SSL encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get;
            private set;
        }

        /// <summary>
        /// The address of the Xmpp entity.
        /// </summary>
        public Jid Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// The default language of the XML stream.
        /// </summary>
        public CultureInfo Language
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines whether the instance is connected to the XMPP server.
        /// </summary>
        public bool Connected
        {
            get;
            private set;
        }

        /// <summary>
        /// Determines whether the instance has been authenticated.
        /// </summary>
        public bool Authenticated
        {
            get;
            private set;
        }

        /// <summary>
        /// The event that is raised when an unrecoverable error condition occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// The event that is raised when an IQ-request stanza has been received.
        /// </summary>
        public event EventHandler<IqEventArgs> Iq;

        /// <summary>
        /// The event that is raised when a Message stanza has been received.
        /// </summary>
        public event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// The event that is raised when a Presence stanza has been received.
        /// </summary>
        public event EventHandler<PresenceEventArgs> Presence;

        /// <summary>
        /// Initializes a new instance of the XmppCore class.
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
        public XmppCore(string hostname, string username, string password, int port = 5222, bool tls = true, string validationHost = null)
        {
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            StartTls = tls;
            ValidationHost = validationHost;
        }

        /// <summary>
        /// Initializes a new instance of the XmppCore class.
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
        public XmppCore(string hostname, int port = 5222, bool tls = true, string validationHost = null)
        {
            Hostname = hostname;
            Port = port;
            StartTls = tls;
            ValidationHost = validationHost;
        }

        #region Methods - Common
        /// <summary>
        /// Sends the specified string to the server.
        /// </summary>
        /// <param name="xml">The string to send.</param>
        /// <exception cref="ArgumentNullException">The xml parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network.</exception>
        protected virtual async void Send(string xml)
        {
            xml.ThrowIfNull("xml");

            try
            {
                await _client.Send(xml);

                if (debugStanzas)
                {
                    Debug.WriteLine(xml);
                }
            }
            catch (IOException e)
            {
                Connected = false;
                throw new XmppDisconnectionException(e.Message, e);
            }
        }

        /// <summary>
        /// Read data from server and return xml
        /// </summary>
        protected virtual async Task<XElement> Read()
        {
            string xml = await _client.Read();
            return XElement.Parse(xml);
        }

        /// <summary>
        /// Read data from server and return xml
        /// </summary>
        protected virtual async Task<string> ReadString()
        {
            string xml = await _client.Read();
            return xml;
        }

        /// <summary>
        /// Serializes and sends the specified XML element to the server and
        /// subsequently waits for a response.
        /// </summary>
        /// <param name="element">The XML element to send.</param>
        /// <param name="expected">A list of element names that are expected. If
        /// provided, and the read element does not match any of the provided names,
        /// an XmmpException is thrown.</param>
        /// <returns>The XML element read from the stream.</returns>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="ArgumentNullException">The element parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network, or there was a failure while reading from the network.</exception>
        protected virtual async Task<XElement> SendAndReceive(string xml, params string[] expected)
        {
            Send(xml);

            try
            {
                while (true)
                {
                    string data = await ReadString();

                    if (string.IsNullOrEmpty(data))
                    {
                        return null;
                    }

                    var xmppData = _parser.Write(data);

                    foreach (var tree in xmppData)
                    {
                        if (expected.Contains(tree.TreeName))
                        {
                            if (tree.TreeName == "stream:features")
                            {
                                tree.Xml = tree.Xml.Replace("stream:", "");
                            }

                            return XElement.Parse(tree.Xml);
                        }
                    }
                }
            }
            catch (XmppDisconnectionException e)
            {
                Connected = false;
                throw e;
            }
        }
        #endregion

        #region Methods - Init
        /// <summary>
        /// Establishes a connection to the XMPP server.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// it is assigned by the server.</param>
        /// <exception cref="SocketException">An error occurred while accessing the socket
        /// used for establishing the connection to the XMPP server. Use the ErrorCode
        /// property to obtain the specific error code.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        /// <exception cref="XmppException">An XMPP error occurred while negotiating the
        /// XML stream with the server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <remarks>If a username has been supplied, this method automatically performs
        /// authentication.</remarks>
        public async void Connect(string resource = null)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            Resource = resource;

            try
            {
                _parser = new XmppParser();
                _client = new SocketClient();
                _client.OnConnect += Client_OnConnect;

                await _client.Connect(Hostname, Port);
            }
            catch (XmlException e)
            {
                throw new XmppException("The XML stream could not be negotiated.", e);
            }
        }
        
        /// <summary>
        /// Disconnects from the XMPP server.
        /// </summary>
        private void Disconnect()
        {
            if (!Connected)
                return;
            // Close the XML stream.
            Send("</stream:stream>");
            Connected = false;
            Authenticated = false;
        }

        /// <summary>
        /// Handle socket client "connected" event
        /// </summary>
        private void Client_OnConnect()
        {
            _client.OnConnect -= Client_OnConnect;

            // We are connected.
            Connected = true;

            // Sets up the connection which includes TLS and possibly SASL negotiation.
            SetupConnection();

            // Set up the listener and dispatcher tasks.
            //Task.Factory.StartNew(ReadXmlStream, TaskCreationOptions.LongRunning);
            //Task.Factory.StartNew(HandleXmppMessage, TaskCreationOptions.LongRunning);
            //Task.Factory.StartNew(DispatchEvents, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Negotiates an XML stream over which XML stanzas can be sent.
        /// </summary>
        /// <param name="resource">The resource identifier to bind with. If this is null,
        /// it is assigned by the server.</param>
        /// <exception cref="XmppException">The resource binding process failed.</exception>
        /// <exception cref="XmlException">Invalid or unexpected XML data has been
        /// received from the XMPP server.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        private async void SetupConnection()
        {
            var featureElement = await InitializeStream();

            CheckTlsSupport(featureElement);
            StartTlsStream();
        }

        /// <summary>
        /// Initiates an XML stream with the specified entity.
        /// </summary>
        private async Task<XElement> InitializeStream()
        {
            var xml = Xml.Element("http://etherx.jabber.org/streams", "stream", "stream")
                    .Attr("to", hostname)
                    .Attr("version", "1.0")
                    .Attr("xmlns", "jabber:client")
                    .Attr(XNamespace.Xml, "lang", CultureInfo.CurrentCulture.Name);

            XElement featureElement = await SendAndReceive(xml.ToXmlString(xmlDeclaration: true, leaveOpen: true), "stream:features");
            return featureElement;
        }

        public void CheckTlsSupport(XElement features)
        {
            XElement startTls = features.GetNodeName("starttls");

            //Secure the stream with Transport Layer Secure protocol
            if (startTls != null)
            {
                IsSupport = true;

                if (startTls.GetNodeName("required") != null)
                {
                    IsRequired = true;
                }
            }
        }
        
        private async void StartTlsStream()
        {
            //Already Ssl stream
            if (IsSslStream)
            {
                return;
            }

            //Secure the stream with Transport Layer Secure protocol
            if (IsSupport)
            {
                // Server supports TLS/SSL via STARTTLS.
                // TLS is mandatory and user opted out of it.
                if (IsRequired && !StartTls)
                {
                    OnTlsError(TlsError.TlsRequired);
                    return;
                }

                if (StartTls)
                {
                    // Send STARTTLS command and ensure the server acknowledges the request.
                    var xmlTls = Xml.Element("urn:ietf:params:xml:ns:xmpp-tls", "starttls");
                    var proceedTls = await SendAndReceive(xmlTls.ToXmlString(), "proceed");

                    // Complete TLS negotiation and switch to secure stream.
                    if (proceedTls != null)
                    {
                        bool isSuccess = await _client.StartTLS(string.IsNullOrEmpty(ValidationHost) ? Hostname : ValidationHost);

                        if (!isSuccess)
                        {
                            OnTlsError(TlsError.ValidationError);
                            return;
                        }
                    }
                    else
                    {
                        OnTlsError(TlsError.ServerFailure);
                        return;
                    }
                }
            }

            OnTlsCompleted();
        }
        
        protected virtual void OnTlsError(TlsError error)
        {
            Debug.WriteLine("TLS Error: " + error.GetMessage());
            _client.Close();
        }

        protected virtual async void OnTlsCompleted()
        {
            var features = await InitializeStream();
            Authenticate(features);
        }
        #endregion

        #region Methods - Authenticate
        /// <summary>
        /// Authenticates with the XMPP server using the specified username and
        /// password.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <exception cref="ArgumentNullException">The username parameter or the
        /// password parameter is null.</exception>
        /// <exception cref="SocketException">An error occurred while accessing the socket
        /// used for establishing the connection to the XMPP server. Use the ErrorCode
        /// property to obtain the specific error code.</exception>
        /// <exception cref="AuthenticationException">An authentication error occured while
        /// trying to establish a secure connection, or the provided credentials were
        /// rejected by the server, or the server requires TLS/SSL and TLS has been
        /// turned off.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="XmppException">Authentication has already been performed, or
        /// an XMPP error occurred while negotiating the XML stream with the
        /// server.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        public void Authenticate(string username, string password)
        {
            AssertValid();
            username.ThrowIfNull("username");
            password.ThrowIfNull("password");

            if (Authenticated)
            {
                throw new XmppException("Authentication has already been performed.");
            }

            // Unfortunately, SASL authentication does not follow the standard XMPP
            // IQ-semantics. At this stage it really is easier to simply perform a
            // reconnect.
            Username = username;
            Password = password;
            Disconnect();
            Connect(this.resource);
        }

        /// <summary>
        /// Performs SASL authentication.
        /// </summary>
        /// <param name="mechanisms">An enumerable collection of SASL mechanisms
        /// supported by the server.</param>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password to authenticate with.</param>
        /// <param name="hostname">The hostname of the XMPP server.</param>
        /// <returns>The 'stream:features' XML element as received from the
        /// receiving entity upon establishment of a new XML stream.</returns>
        /// <remarks>Refer to RFC 3920, Section 6 (Use of SASL).</remarks>
        /// <exception cref="SaslException">A SASL error condition occured.</exception>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="CultureNotFoundException">The culture specified by the
        /// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private async void Authenticate(XElement features)
        {
            // If no Username has been provided, don't perform authentication.
            if (Username == null)
                return;

            // Construct a list of SASL mechanisms supported by the server.
            var mechanisms = features.GetNodeName("mechanisms");
            if (mechanisms == null)
            {
                OnSASLFailed("No SASL mechanisms advertised.");
            }

            var mech = mechanisms.FirstNode as XElement;
            var list = new HashSet<string>();

            while (mech != null)
            {
                list.Add(mech.Value);
                mech = mech.NextNode as XElement;
            }

            await Authenticate(list);

            // Continue with SASL authentication.
            //try
            //{
            //    //FIXME: How is the client's JID constructed if the server does not support
            //    // resource binding?
            //    if (features.GetNodeName("bind") != null)
            //        Jid = await BindResource(resource);
            //}
            //catch (SaslException e)
            //{
            //    throw new AuthenticationException(e.Message);
            //}
        }

        /// <summary>
        /// Performs SASL authentication.
        /// </summary>
        private async Task<XElement> Authenticate(IEnumerable<string> mechanisms)
        {
            string name = SelectMechanism(mechanisms);

            if (string.IsNullOrEmpty(name))
            {
                OnSASLFailed("No supported SASL mechanism found.");
            }

            SaslMechanism m = SaslFactory.Create(name);
            m.Properties.Add("Username", username);
            m.Properties.Add("Password", password);

            var xml = Xml.Element("auth")
                .Attr("xmlns", "urn:ietf:params:xml:ns:xmpp-sasl")
                .Attr("mechanism", name)
                .Text(m.HasInitial ? m.GetResponse(string.Empty) : string.Empty);

            XElement authResult = await SendAndReceive(xml.ToXmlString(), "challenge", "success", "failure");
            
            if (authResult.Name == "failure")
                OnSASLFailed("SASL authentication failed.");

            if (authResult.Name == "success" && m.IsCompleted)
            {
                Authenticated = true;
            }

            // Server has successfully authenticated us, but mechanism still needs
            // to verify server's signature.
            string response = m.GetResponse(authResult.Value);

            // If the response is the empty string, the server's signature has been
            // verified.
            if (authResult.Name == "success")
            {
                if (response == string.Empty)
                {
                    Authenticated = true;
                }
                else
                {
                    throw new SaslException("Could not verify server's signature.");
                }
            }

            xml = Xml.Element("response", "urn:ietf:params:xml:ns:xmpp-sasl").Text(response);

            //Send(xml);

            // Finally, initiate a new XML-stream.
            return await InitializeStream();
        }

        /// <summary>
        /// Selects the best SASL mechanism that we support from the list of mechanisms
        /// advertised by the server.
        /// </summary>
        /// <param name="mechanisms">An enumerable collection of SASL mechanisms
        /// advertised by the server.</param>
        /// <returns>The IANA name of the selcted SASL mechanism.</returns>
        /// <exception cref="SaslException">No supported mechanism could be found in
        /// the list of mechanisms advertised by the server.</exception>
        private string SelectMechanism(IEnumerable<string> mechanisms)
        {
            // Precedence: SCRAM-SHA-1, DIGEST-MD5, PLAIN.
            string[] m = new string[] { "SCRAM-SHA-1", "DIGEST-MD5", "PLAIN" };

            for (int i = 0; i < m.Length; i++)
            {
                if (mechanisms.Contains(m[i], StringComparer.CurrentCultureIgnoreCase))
                    return m[i];
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Performs resource binding and returns the 'full JID' with which this
        /// session associated.
        /// </summary>
        /// <param name="resourceName">The resource identifier to bind to. If this
        /// is null, the server generates a random identifier.</param>
        /// <returns>The full JID to which this session has been bound.</returns>
        /// <remarks>Refer to RFC 3920, Section 7 (Resource Binding).</remarks>
        /// <exception cref="XmppException">The resource binding process
        /// failed due to an erroneous server response.</exception>
        /// <exception cref="XmlException">The XML parser has encountered invalid
        /// or unexpected XML data.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure while reading from the network.</exception>
        private async Task<Jid> BindResource(string resourceName = null)
        {
            var xml = Xml.Element("iq")
                .Attr("type", "set")
                .Attr("id", "bind-0");

            var bind = Xml.Element("bind", "urn:ietf:params:xml:ns:xmpp-bind");

            if (resourceName != null)
                bind.Child(Xml.Element("resource").Text(resourceName));

            xml.Child(bind);

            XElement res = await SendAndReceive(xml.ToXmlString(), "iq");

            if (res.GetNodeName("bind") == null || res.GetNodeName("bind").GetNodeName("jid") == null)
                throw new XmppException("Erroneous server response.");

            return new Jid(res.GetNodeName("bind").GetNodeName("jid").Value);
        }

        protected virtual void OnSASLFailed(string message)
        {
            Debug.WriteLine("SASL Error: " + message);
        }
        #endregion

        /// <summary>
        /// Sends a Message stanza with the specified attributes and content to the
        /// server.
        /// </summary>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="id">The ID of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendMessage(Jid to = null, Jid from = null, XElement data = null, string id = null, CultureInfo language = null)
        {
            AssertValid();
            Send(new Message(to, from, data, id, language));
        }

        /// <summary>
        /// Sends the specified message stanza to the server.
        /// </summary>
        /// <param name="message">The message stanza to send to the server.</param>
        /// <exception cref="ArgumentNullException">The message parameter is
        /// null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendMessage(Message message)
        {
            AssertValid();
            message.ThrowIfNull("message");
            Send(message);
        }

        /// <summary>
        /// Sends a Presence stanza with the specified attributes and content to the
        /// server.
        /// </summary>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="id">The ID of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendPresence(Jid to = null, Jid from = null, string id = null, CultureInfo language = null, params XElement[] data)
        {
            AssertValid();
            Send(new Presence(to, from, id, language, data));
        }

        /// <summary>
        /// Sends the specified presence stanza to the server.
        /// </summary>
        /// <param name="presence">The presence stanza to send to the server.</param>
        /// <exception cref="ArgumentNullException">The presence parameter
        /// is null.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void SendPresence(Presence presence)
        {
            AssertValid();
            presence.ThrowIfNull("presence");
            Send(presence);
        }

        /// <summary>
        /// Performs an IQ set/get request and blocks until the response IQ comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait
        /// for the arrival of the IQ response or -1 to wait indefinitely.</param>
        /// <returns>The IQ response sent by the server.</returns>
        /// <exception cref="ArgumentException">The type parameter is not
        /// IqType.Set or IqType.Get.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
        /// is a negative number other than -1, which represents an indefinite
        /// timeout.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure reading from the network.</exception>
        /// <exception cref="TimeoutException">A timeout was specified and it
        /// expired.</exception>
        public Iq IqRequest(IqType type, Jid to = null, Jid from = null, XElement data = null, CultureInfo language = null, int millisecondsTimeout = -1)
        {
            AssertValid();
            return IqRequest(new Iq(type, null, to, from, data, language), millisecondsTimeout);
        }

        /// <summary>
        /// Performs an IQ set/get request and blocks until the response IQ comes in.
        /// </summary>
        /// <param name="request">The IQ request to send.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait
        /// for the arrival of the IQ response or -1 to wait indefinitely.</param>
        /// <returns>The IQ response sent by the server.</returns>
        /// <exception cref="ArgumentNullException">The request parameter is null.</exception>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
        /// is a negative number other than -1, which represents an indefinite
        /// timeout.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network, or there was a failure reading from the network.</exception>
        /// <exception cref="TimeoutException">A timeout was specified and it
        /// expired.</exception>
        public Iq IqRequest(Iq request, int millisecondsTimeout = -1)
        {
            lock(this)
            {
                int timeOut = -1;
                AssertValid();
                request.ThrowIfNull("request");
                if (request.Type != IqType.Set && request.Type != IqType.Get)
                    throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
                if (millisecondsTimeout == -1)
                {
                    timeOut = millisecondsDefaultTimeout;
                }
                else timeOut = millisecondsTimeout;

                // Generate a unique ID for the IQ request.
                request.Id = GetId();
                AutoResetEvent ev = new AutoResetEvent(false);
                Send(request);
                // Wait for event to be signaled by task that processes the incoming
                // XML stream.
                waitHandles[request.Id] = ev;
                int index = WaitHandle.WaitAny(new WaitHandle[] { ev, cancelIq.Token.WaitHandle },
                    timeOut);
                if (index == WaitHandle.WaitTimeout)
                {
                    //An entity that receives an IQ request of type "get" or "set" MUST reply with an IQ response of type
                    //"result" or "error" (the response MUST preserve the 'id' attribute of the request).
                    //http://xmpp.org/rfcs/rfc3920.html#stanzas
                    //if (request.Type == IqType.Set || request.Type == IqType.Get)

                    //Make sure that its a request towards the server and not towards any client
                    var ping = request.Data.GetNodeName("ping");

                    if (request.To.Domain == Jid.Domain && (request.To.Node == null || request.To.Node == "") && (ping != null && ping.BaseUri == "urn:xmpp:ping"))
                    {
                        Connected = false;
                        var e = new XmppDisconnectionException("Timeout Disconnection happened at IqRequest");
                        if (!disposed)
                            Error.Raise(this, new ErrorEventArgs(e));
                        //throw new TimeoutException();
                    }

                    //This check is somehow not really needed doue to the IQ must be either set or get
                }
                // Reader task errored out.
                if (index == 1)
                    throw new IOException("The incoming XML stream could not read.");
                // Fetch response stanza.
                Iq response;

                if (iqResponses.TakeOut(request.Id, out response))
                    return response;
                // Shouldn't happen.

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Performs an IQ set/get request asynchronously and optionally invokes a
        /// callback method when the IQ response comes in.
        /// </summary>
        /// <param name="type">The type of the request. This must be either
        /// IqType.Set or IqType.Get.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <param name="callback">A callback method which is invoked once the
        /// IQ response from the server comes in.</param>
        /// <returns>The ID value of the pending IQ stanza request.</returns>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public string IqRequestAsync(IqType type, Jid to = null, Jid from = null,
            XElement data = null, CultureInfo language = null,
            Action<string, Iq> callback = null)
        {
            AssertValid();
            return IqRequestAsync(new Iq(type, null, to, from, data, language), callback);
        }

        /// <summary>
        /// Performs an IQ set/get request asynchronously and optionally invokes a
        /// callback method when the IQ response comes in.
        /// </summary>
        /// <param name="request">The IQ request to send.</param>
        /// <param name="callback">A callback method which is invoked once the
        /// IQ response from the server comes in.</param>
        /// <returns>The ID value of the pending IQ stanza request.</returns>
        /// <exception cref="ArgumentNullException">The request parameter is null.</exception>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Set
        /// or IqType.Get.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public string IqRequestAsync(Iq request, Action<string, Iq> callback = null)
        {
            AssertValid();
            request.ThrowIfNull("request");
            if (request.Type != IqType.Set && request.Type != IqType.Get)
                throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
            request.Id = GetId();
            // Register the callback.
            if (callback != null)
                iqCallbacks[request.Id] = callback;
            Send(request);
            return request.Id;
        }

        /// <summary>
        /// Sends an IQ response for the IQ request with the specified id.
        /// </summary>
        /// <param name="type">The type of the response. This must be either
        /// IqType.Result or IqType.Error.</param>
        /// <param name="id">The id of the IQ request.</param>
        /// <param name="to">The JID of the intended recipient for the stanza.</param>
        /// <param name="from">The JID of the sender.</param>
        /// <param name="data">he content of the stanza.</param>
        /// <param name="language">The language of the XML character data of
        /// the stanza.</param>
        /// <exception cref="ArgumentException">The type parameter is not IqType.Result
        /// or IqType.Error.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void IqResponse(IqType type, string id, Jid to = null, Jid from = null,
            XElement data = null, CultureInfo language = null)
        {
            AssertValid();
            IqResponse(new Iq(type, id, to, from, data, null));
        }

        /// <summary>
        /// Sends an IQ response for the IQ request with the specified id.
        /// </summary>
        /// <param name="response">The IQ response to send.</param>
        /// <exception cref="ArgumentNullException">The response parameter is
        /// null.</exception>
        /// <exception cref="ArgumentException">The Type property of the response
        /// parameter is not IqType.Result or IqType.Error.</exception>
        /// <exception cref="ObjectDisposedException">The XmppCore object has been
        /// disposed.</exception>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void IqResponse(Iq response)
        {
            AssertValid();
            response.ThrowIfNull("response");
            if (response.Type != IqType.Result && response.Type != IqType.Error)
                throw new ArgumentException("The IQ type must be either 'result' or 'error'.");
            Send(response);
        }

        /// <summary>
        /// Closes the connection with the XMPP server. This automatically disposes
        /// of the object.
        /// </summary>
        /// <exception cref="InvalidOperationException">The XmppCore instance is not
        /// connected to a remote host.</exception>
        /// <exception cref="IOException">There was a failure while writing to the
        /// network.</exception>
        public void Close()
        {
            //FIXME, instead of asert valid I have ifs, only for the closing
            //AssertValid();
            // Close the XML stream.
            if (Connected) Disconnect();
            if (!disposed) Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppCore class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the XmppCore
        /// class, optionally disposing of managed resource.
        /// </summary>
        /// <param name="disposing">true to dispose of managed resources, otherwise
        /// false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                // Indicate that the instance has been disposed.
                disposed = true;

                // Get rid of managed resources.
                if (disposing)
                {
                    _parser = null;

                    if (_client != null)
                        _client.Close();

                    _client = null;
                }
                // Get rid of unmanaged resources.
            }
        }

        /// <summary>
        /// Asserts the instance has not been disposed of and is connected to the
        /// XMPP server.
        /// </summary>
        private void AssertValid()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            //FIXME-FIXED: if it is not connected it will be found out by a lower
            //level exception. Dont throw an exception about connection

            if (!Connected)
            {
                System.Diagnostics.Debug.WriteLine("Assert Valid: Client is disconnected, however no exception is thrown");
                //throw new InvalidOperationException("Not connected to XMPP server.");
            }
            //FIXME
        }
        
        /// <summary>
        /// Sends the specified stanza to the server.
        /// </summary>
        /// <param name="stanza">The stanza to send.</param>
        /// <exception cref="ArgumentNullException">The stanza parameter is null.</exception>
        /// <exception cref="IOException">There was a failure while writing to
        /// the network.</exception>
        private void Send(Stanza stanza)
        {
            stanza.ThrowIfNull("stanza");
            Send(stanza.ToString());
        }
        
        /// <summary>
        /// Listens for incoming XML stanzas and raises the appropriate events.
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. In case of an
        /// exception, the Error event is raised and the thread is shutdown.</remarks>
        private async void ReadXmlStream()
        {
            try
            {
                while (true)
                {
                    string data = await ReadString();
                    
                    var now = DateTime.Now;
                    var xmppData = _parser.Write(data);

                    lock (this)
                    {
                        foreach (var xmpp in xmppData)
                        {
                            messageQueue.Enqueue(xmpp);
                        }
                    }

                    //XElement elem = parser.NextElement(element, "iq", "message", "presence");

                    //// Parse element and dispatch.
                    //switch (elem.Name.LocalName)
                    //{
                    //    case "iq":
                    //        Iq iq = new Iq(elem);
                    //        if (iq.IsRequest)
                    //            stanzaQueue.Add(iq);
                    //        else
                    //            HandleIqResponse(iq);
                    //        break;

                    //    case "message":
                    //        stanzaQueue.Add(new Message(elem));
                    //        break;

                    //    case "presence":
                    //        stanzaQueue.Add(new Presence(elem));
                    //        break;
                    //}


                }
            }
            catch (Exception e)
            {
                // Shut down the dispatcher task.
                cancelDispatch.Cancel();
                cancelDispatch = new CancellationTokenSource();

                // Unblock any threads blocking on pending IQ requests.
                cancelIq.Cancel();
                cancelIq = new CancellationTokenSource();

                //Add the failed connection
                if ((e is IOException) || (e is XmppDisconnectionException))
                {
                    Connected = false;
                    var ex = new XmppDisconnectionException(e.ToString());
                    e = ex;
                }

                // Raise the error event.
                if (!disposed)
                    Error.Raise(this, new ErrorEventArgs(e));
            }
        }

        /// <summary>
        /// Continously removes stanzas from the FIFO of incoming stanzas and raises
        /// the respective events.
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. All stanza events
        /// are streamlined and execute in the context of this thread.</remarks>
        private void DispatchEvents()
        {
            while (true)
            {
                lock (this)
                {
                    try
                    {
                        Stanza stanza = stanzaQueue.Dequeue();
                        if (debugStanzas) Debug.WriteLine(stanza.ToString());
                        if (stanza is Iq)
                            Iq.Raise(this, new IqEventArgs(stanza as Iq));
                        else if (stanza is Message)
                            Message.Raise(this, new MessageEventArgs(stanza as Message));
                        else if (stanza is Presence)
                            Presence.Raise(this, new PresenceEventArgs(stanza as Presence));
                    }
                    catch (OperationCanceledException)
                    {
                        // Quit the task if it's been cancelled.
                        return;
                    }
                    catch (Exception e)
                    {
                        // FIXME: What should we do if an exception is thrown in one of the
                        // event handlers?
                        System.Diagnostics.Debug.WriteLine("Error in XMPP Core: " + e.StackTrace + e.ToString());
                        //throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Continously removes message from the FIFO of incoming xmpp message and parse into
        /// the respective object types
        /// </summary>
        /// <remarks>This runs in the context of a separate thread. All stanza events
        /// are streamlined and execute in the context of this thread.</remarks>
        private void HandleXmppMessage()
        {
            while (true)
            {
                XmppTree xmppTree = null;

                lock (this)
                {
                    if (messageQueue.Count > 0)
                    {
                        xmppTree = messageQueue.Dequeue();
                    }
                }

                if (xmppTree != null)
                {
                    
                }
                
                //XElement elem = parser.NextElement(element, "iq", "message", "presence");

                //// Parse element and dispatch.
                //switch (elem.Name.LocalName)
                //{
                //    case "iq":
                //        Iq iq = new Iq(elem);
                //        if (iq.IsRequest)
                //            stanzaQueue.Add(iq);
                //        else
                //            HandleIqResponse(iq);
                //        break;

                //    case "message":
                //        stanzaQueue.Add(new Message(elem));
                //        break;

                //    case "presence":
                //        stanzaQueue.Add(new Presence(elem));
                //        break;
                //}


            }
        }

        /// <summary>
        /// Handles incoming IQ responses for previously issued IQ requests.
        /// </summary>
        /// <param name="iq">The received IQ response stanza.</param>
        private void HandleIqResponse(Iq iq)
        {
            lock (this)
            {
                string id = iq.Id;
                AutoResetEvent ev;
                Action<string, Iq> cb;
                iqResponses[id] = iq;

                // Signal the event if it's a blocking call.
                if (waitHandles.TakeOut(id, out ev))
                {
                    ev.Set();
                }

                // Call the callback if it's an asynchronous call.
                else if (iqCallbacks.TakeOut(id, out cb))
                {
                    Task.Factory.StartNew(() => { cb(id, iq); });
                }
            }
        }

        /// <summary>
        /// Generates a unique id.
        /// </summary>
        /// <returns>A unique id.</returns>
        private string GetId()
        {
            Interlocked.Increment(ref id);
            return id.ToString();
        }
    }
}