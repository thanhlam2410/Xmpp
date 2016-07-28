using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Sharp.Xmpp.Core
{
    public class SocketClient
    {
        #region Fields
        private StreamSocket socket;
        #endregion

        #region Events
        public event Action OnConnect;
        public event Action<string> ConnectError;
        #endregion

        #region Methods
        public SocketClient()
        {
            socket = new StreamSocket();
        }

        /// <summary>
        /// CONNECT TO SERVER
        /// </summary>
        /// <param name="host">Host name/IP address</param>
        /// <param name="port">Port number</param>
        /// <param name="message">Message to server</param>
        /// <returns>Response from server</returns>
        public async Task Connect(string host, int port)
        {
            HostName hostName = new HostName(host);
            
            // Set NoDelay to false so that the Nagle algorithm is not disabled
            socket.Control.NoDelay = false;

            try
            {
                // Connect to the server
                await socket.ConnectAsync(hostName, port.ToString());

                if (OnConnect != null)
                {
                    OnConnect.Invoke();
                }
            }
            catch (Exception exception)
            {
                SocketErrorStatus status = SocketError.GetStatus(exception.HResult);

                if (ConnectError != null)
                {
                    ConnectError.Invoke(status.ToString());
                }
            }
        }

        /// <summary>
        /// SEND DATA
        /// </summary>
        /// <param name="message">Message to server</param>
        /// <returns>void</returns>
        public async Task Send(string message)
        {
            Debug.WriteLine(message);

            // Create the data writer object backed by the in-memory stream. 
            using (DataWriter writer = new DataWriter(socket.OutputStream))
            {
                // Set the Unicode character encoding for the output stream
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                // Specify the byte order of a stream.
                writer.ByteOrder = ByteOrder.LittleEndian;

                // Gets the size of UTF-8 string.
                writer.MeasureString(message);

                // Write a string value to the output stream.
                writer.WriteString(message);

                // Send the contents of the writer to the backing stream.
                try
                {
                    await writer.StoreAsync();

                    await writer.FlushAsync();

                    // In order to prolong the lifetime of the stream, detach it from the DataWriter
                    writer.DetachStream();
                }
                catch (Exception exception)
                {
                    SocketErrorStatus status = SocketError.GetStatus(exception.HResult);

                    if (ConnectError != null)
                    {
                        ConnectError.Invoke(status.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// SEND DATA
        /// </summary>
        /// <param name="message">Message to server</param>
        /// <returns>void</returns>
        public async Task Send(byte[] message)
        {
            // Create the data writer object backed by the in-memory stream. 
            using (DataWriter writer = new DataWriter(socket.OutputStream))
            {
                // Set the Unicode character encoding for the output stream
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                // Specify the byte order of a stream.
                writer.ByteOrder = ByteOrder.LittleEndian;

                // Write a string value to the output stream.
                writer.WriteBytes(message);

                // Send the contents of the writer to the backing stream.
                try
                {
                    await writer.StoreAsync();
                    await writer.FlushAsync();

                    // In order to prolong the lifetime of the stream, detach it from the DataWriter
                    writer.DetachStream();
                }
                catch (Exception exception)
                {
                    SocketErrorStatus status = SocketError.GetStatus(exception.HResult);

                    if (ConnectError != null)
                    {
                        ConnectError.Invoke(status.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// READ RESPONSE
        /// </summary>
        /// <returns>Response from server</returns>
        public async Task<string> Read()
        {
            using (DataReader reader = new DataReader(socket.InputStream))
            {
                StringBuilder strBuilder = new StringBuilder();

                // Set the DataReader to only wait for available data (so that we don't have to know the data size)
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // The encoding and byte order need to match the settings of the writer we previously used.
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;

                // Send the contents of the writer to the backing stream. 
                // Get the size of the buffer that has not been read.
                await reader.LoadAsync(4096);

                // Keep reading until we consume the complete stream.
                while (reader.UnconsumedBufferLength > 0)
                {
                    strBuilder.Append(reader.ReadString(reader.UnconsumedBufferLength));
                }

                reader.DetachStream();

                Debug.WriteLine(strBuilder.ToString());
                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// READ RESPONSE
        /// </summary>
        /// <returns>Response from server</returns>
        public async Task<byte[]> ReadBytes()
        {
            using (DataReader reader = new DataReader(socket.InputStream))
            {
                StringBuilder strBuilder = new StringBuilder();

                // Set the DataReader to only wait for available data (so that we don't have to know the data size)
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // The encoding and byte order need to match the settings of the writer we previously used.
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = ByteOrder.LittleEndian;

                // Send the contents of the writer to the backing stream. 
                // Get the size of the buffer that has not been read.
                uint dataCount = await reader.LoadAsync(4096);

                //Create data buffer
                byte[] data = new byte[0];

                // Keep reading until we consume the complete stream.
                if (dataCount > 0 && reader.UnconsumedBufferLength > 0)
                {
                    data = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(data);
                }
                
                reader.DetachStream();
                return data;
            }
        }

        /// <summary>
        /// Convert current stream to SSL using Transport-Layer Secure
        /// </summary>
        public async Task<bool> StartTLS(string validationHost)
        {
            try
            {
                await socket.UpgradeToSslAsync(SocketProtectionLevel.Ssl, new HostName(validationHost));
                return true;
            }
            catch (Exception exception)
            {
                SocketErrorStatus status = SocketError.GetStatus(exception.HResult);
                Debug.WriteLine(status.ToString() + ": " + exception.Message);
                
                return false;
            }
        }

        /// <summary>
        /// Close and dispose socket stream
        /// </summary>
        public void Close()
        {
            socket.Dispose();
        }
        #endregion
    }
}
