using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Twino.MQ.Clients;
using Twino.MQ.Helpers;
using Twino.MQ.Options;
using Twino.MQ.Security;

namespace Twino.MQ.Channels
{
    /// <summary>
    /// Channel status
    /// </summary>
    public enum ChannelStatus
    {
        /// <summary>
        /// Channel queue messaging is running. Messages are accepted and sent to queueus.
        /// </summary>
        Running,

        /// <summary>
        /// Channel queue messages are accepted and queued but not pending
        /// </summary>
        Paused,

        /// <summary>
        /// Channel queue messages are not accepted.
        /// </summary>
        Stopped
    }

    /// <summary>
    /// Messaging Queue Channel
    /// </summary>
    public class Channel
    {
        #region Properties

        /// <summary>
        /// Unique channel name (not case-sensetive)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Server of the channel
        /// </summary>
        public MQServer Server { get; }

        /// <summary>
        /// Channel options
        /// </summary>
        public ChannelOptions Options { get; }

        /// <summary>
        /// Channel status
        /// </summary>
        public ChannelStatus Status { get; private set; }

        /// <summary>
        /// Channel authenticator.
        /// If null, server's default channel authenticator will be used.
        /// </summary>
        public IChannelAuthenticator Authenticator { get; }

        private readonly FlexArray<QueueContentType> _allowedContentTypes = new FlexArray<QueueContentType>(264, 250);

        /// <summary>
        /// Allowed content type in this channel
        /// </summary>
        public IEnumerable<QueueContentType> AllowedContentTypes => _allowedContentTypes.All();

        /// <summary>
        /// Channel event handler
        /// </summary>
        public IChannelEventHandler EventHandler { get; }

        /// <summary>
        /// Channel messaging delivery handler.
        /// If queue does not have it's own delivery handler, this one is used.
        /// </summary>
        public IMessageDeliveryHandler DeliveryHandler { get; }

        /// <summary>
        /// Active channel queues
        /// </summary>
        public IEnumerable<ChannelQueue> Queues => _queues.All();

        private readonly FlexArray<ChannelQueue> _queues;

        /// <summary>
        /// Clients in the channel
        /// </summary>
        public IEnumerable<ChannelClient> Clients => _clients.All();

        private readonly FlexArray<ChannelClient> _clients;

        #endregion

        #region Constructors

        internal Channel(MQServer server,
                         ChannelOptions options,
                         string name,
                         IChannelAuthenticator authenticator,
                         IChannelEventHandler eventHandler,
                         IMessageDeliveryHandler deliveryHandler)
        {
            Server = server;
            Options = options;
            Name = name;
            Status = ChannelStatus.Running;

            Authenticator = authenticator;
            EventHandler = eventHandler;
            DeliveryHandler = deliveryHandler;

            _queues = new FlexArray<ChannelQueue>(options.QueueCapacity);
            _clients = new FlexArray<ChannelClient>(server.Options.ClientCapacity);
        }

        #endregion

        #region Status Actions

        /// <summary>
        /// Sets status of the channel
        /// </summary>
        public async Task SetStatus(ChannelStatus status)
        {
            ChannelStatus old = Status;
            if (old == status)
                return;

            if (EventHandler != null)
            {
                bool allowed = await EventHandler.OnStatusChanged(this, old, status);
                if (!allowed)
                    return;
            }

            Status = status;
        }

        #endregion

        #region Queue Actions

        /// <summary>
        /// Creates new queue in the channel with default options and default handlers
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort contentType)
        {
            return await CreateQueue(contentType,
                                     Options,
                                     Server.DefaultQueueEventHandler,
                                     Server.DefaultDeliveryHandler);
        }


        /// <summary>
        /// Creates new queue in the channel with default handlers
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort contentType, ChannelQueueOptions options)
        {
            return await CreateQueue(contentType,
                                     options,
                                     Server.DefaultQueueEventHandler,
                                     Server.DefaultDeliveryHandler);
        }

        /// <summary>
        /// Creates new queue in the channel
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort contentType,
                                                    ChannelQueueOptions options,
                                                    IQueueEventHandler eventHandler,
                                                    IMessageDeliveryHandler deliveryHandler)
        {
            ChannelQueue queue = _queues.Find(x => x.ContentType == contentType);
            if (queue != null)
                throw new DuplicateNameException($"The channel has already a queue with same content type: {contentType}");

            queue = new ChannelQueue(this, contentType, options, eventHandler, deliveryHandler);
            _queues.Add(queue);

            if (EventHandler != null)
                await EventHandler.OnQueueCreated(queue, this);

            return queue;
        }

        /// <summary>
        /// Removes a queue from the channel
        /// </summary>
        public async Task RemoveQueue(ChannelQueue queue)
        {
            _queues.Remove(queue);

            if (EventHandler != null)
                await EventHandler.OnQueueRemoved(queue, this);
        }

        #endregion

        #region Client Actions

        /// <summary>
        /// Adds the client to the channel
        /// </summary>
        public async Task<bool> AddClient(MqClient client)
        {
            if (Authenticator != null)
            {
                bool allowed = await Authenticator.Authenticate(this, client);
                if (!allowed)
                    return false;
            }

            ChannelClient cc = new ChannelClient(this, client);
            _clients.Add(cc);

            if (EventHandler != null)
                await EventHandler.ClientJoined(cc);

            return true;
        }

        /// <summary>
        /// Removes client from the channel
        /// </summary>
        public async Task RemoveClient(ChannelClient client)
        {
            _clients.Remove(client);

            if (EventHandler != null)
                await EventHandler.ClientLeft(client);
        }

        /// <summary>
        /// Removes client from the channel
        /// </summary>
        public async Task RemoveClient(MqClient client)
        {
            ChannelClient cc = _clients.RemoveAndGet(x => x.Client == client);

            if (cc != null && EventHandler != null)
                await EventHandler.ClientLeft(cc);
        }

        #endregion
    }
}