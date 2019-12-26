using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Twino.Core
{
    /// <summary>
    /// Function definition for parameterless web sockets
    /// </summary>
    public delegate void ClientMessageHandler<TMessage>(ClientSocketBase<TMessage> client, TMessage message);

    public abstract class ClientSocketBase<TMessage> : SocketBase
    {
        #region Properties

        /// <summary>
        /// Maximum time to wait response message
        /// </summary>
        public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Client certificate for SSL client connections.
        /// If null, Twino uses default certificate.
        /// </summary>
        protected X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Connection data, path, method, properties (if http used directly or indirectly, properties may be header key/values)
        /// </summary>
        public ConnectionData Data { get; } = new ConnectionData();

        /// <summary>
        /// Triggered when a message is received from the network stream
        /// </summary>
        public event ClientMessageHandler<TMessage> MessageReceived;

        /// <summary>
        /// If client is working on passive mode,
        /// we will need to send ping messages to recognize if it's disconnected or just server is not sending messages
        /// </summary>
        private Timer _pingTimer;

        #endregion

        #region Keep Alive

        /// <summary>
        /// Creates new ping timer
        /// </summary>
        private void CreatePingTimer()
        {
            if (_pingTimer != null)
                DestroyPingTimer();

            _pingTimer = new Timer(s =>
            {
                if (!IsConnected)
                {
                    DestroyPingTimer();
                    return;
                }

                if (DateTime.UtcNow - LastAliveDate > PingInterval)
                {
                    LastAliveDate = DateTime.UtcNow;
                    Ping();
                }
            }, null, 5000, 5000);
        }

        /// <summary>
        /// Destroyes ping timer and releases all sources
        /// </summary>
        private void DestroyPingTimer()
        {
            if (_pingTimer == null)
                return;

            _pingTimer.Dispose();
            _pingTimer = null;
        }

        protected override void OnConnected()
        {
            CreatePingTimer();
            LastAliveDate = DateTime.UtcNow;

            base.OnConnected();
        }

        #endregion

        #region Connect

        /// <summary>
        /// Connects to specified url.
        /// Url can be like;
        /// ws://10.20.30.40
        /// wss://10.20.30.40/path
        /// tmqs://domain.com
        /// tmq://domain.com:154/path
        /// </summary>
        public void Connect(string uri)
        {
            DnsResolver resolver = new DnsResolver();
            DnsInfo info = resolver.Resolve(uri);

            Connect(info);
        }

        /// <summary>
        /// Connects to specified url.
        /// Url can be like;
        /// ws://10.20.30.40
        /// wss://10.20.30.40/path
        /// tmqs://domain.com
        /// tmq://domain.com:154/path
        /// </summary>
        public async Task ConnectAsync(string uri)
        {
            DnsResolver resolver = new DnsResolver();
            DnsInfo info = resolver.Resolve(uri);

            await ConnectAsync(info);
        }

        /// <summary>
        /// Connects to an IP address on specified port.
        /// If secure is true, connects with SSL
        /// </summary>
        public void Connect(string ip, int port, bool secure)
        {
            DnsInfo info = new DnsInfo
                           {
                               IPAddress = ip,
                               Hostname = ip,
                               Port = port,
                               Path = "/",
                               Protocol = Protocol.WebSocket,
                               SSL = secure
                           };

            IsSsl = secure;
            Connect(info);
        }

        /// <summary>
        /// Connects to an IP address on specified port.
        /// If secure is true, connects with SSL
        /// </summary>
        public async Task ConnectAsync(string ip, int port, bool secure)
        {
            DnsInfo info = new DnsInfo
                           {
                               IPAddress = ip,
                               Hostname = ip,
                               Port = port,
                               Path = "/",
                               Protocol = Protocol.WebSocket,
                               SSL = secure
                           };

            IsSsl = secure;
            await ConnectAsync(info);
        }

        /// <summary>
        /// Connects to the server
        /// </summary>
        public abstract void Connect(DnsInfo host);

        /// <summary>
        /// Connects to the server
        /// </summary>
        public abstract Task ConnectAsync(DnsInfo host);

        #endregion

        /// <summary>
        /// Starts to read from the TCP socket
        /// </summary>
        protected abstract Task Read();

        /// <summary>
        /// certificate invocation always true method
        /// </summary>
        protected static bool CertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// If message received event should be triggered to derived classes, this method can be used.
        /// </summary>
        protected void SetOnMessageReceived(TMessage message)
        {
            LastAliveDate = DateTime.UtcNow;
            MessageReceived?.Invoke(this, message);
        }
    }
}