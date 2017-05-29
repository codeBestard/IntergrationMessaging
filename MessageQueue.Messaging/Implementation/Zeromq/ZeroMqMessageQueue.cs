using MessageQueue.Messaging.Extensions;
using MessageQueue.Messaging.Specification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZMQ;

namespace MessageQueue.Messaging.Implementation.Zeromq
{
    public sealed class ZeroMqMessageQueue : MessageQueueBase
    {
        private Socket _socket;

        private static readonly Lazy<Context> lazyContext = new Lazy<Context>( () => new Context() );
        private Context _context => lazyContext.Value;

        public override string Name => "ZeroMQ";


        public override void InitializeOutbound( string addressName, MessagePattern pattern, bool isTemporary = false)
        {
            base.InitializeProperties( Direction.Outbound, addressName, pattern, false );

            switch ( Pattern )
            {
                case MessagePattern reqrep when ( MessagePattern.RequestResponse == reqrep ):
                    _socket = _context.Socket( SocketType.REQ );
                    _socket.Connect( MessageAddress.Address );
                    break;

                case MessagePattern ff when ( MessagePattern.FireAndForget == ff ):
                    _socket = _context.Socket( SocketType.PUSH );
                    _socket.Connect( MessageAddress.Address );
                    break;

                case MessagePattern pubsub when ( MessagePattern.PublishSubscribe == pubsub ):
                    _socket = _context.Socket( SocketType.PUB );
                    _socket.Bind( MessageAddress.Address );
                    break;
            }
        }


        public override void InitializeInbound( string addressName, MessagePattern pattern, bool isTemporary = false)
        {
            base.InitializeProperties( Direction.Inbound, addressName, pattern, false );

            switch ( Pattern )
            {
                case MessagePattern reqrep when ( reqrep == MessagePattern.RequestResponse ):
                    _socket = _context.Socket( SocketType.REP );
                    _socket.Bind( MessageAddress.Address );
                    break;

                case MessagePattern ff when ( ff == MessagePattern.FireAndForget ):
                    _socket = _context.Socket( SocketType.PULL );
                    _socket.Bind( MessageAddress.Address );
                    break;

                case MessagePattern pubsub when ( pubsub == MessagePattern.PublishSubscribe ):
                    _socket = _context.Socket( SocketType.SUB );
                    _socket.Connect( MessageAddress.Address );
                    _socket.Subscribe( "", Encoding.UTF8 );
                    break;
            }
        }

        public override void Send( Message message )
        {
            var json = message.ToJsonString();
            _socket.Send( json, Encoding.UTF8 );
        }

        public override async Task Receive( Action<Message> onMessageReceived,
                                      CancellationToken cancellationToken,
                                      int maximumWaitMilliseconds = 0 )
        {
            var inboundMessage = maximumWaitMilliseconds > 0
                                    ? _socket.Recv( Encoding.UTF8, maximumWaitMilliseconds )     
                                    : _socket.Recv( Encoding.UTF8 );                           

            var message = Message.FromJson( inboundMessage );

            if ( cancellationToken != CancellationToken.None && Pattern != MessagePattern.RequestResponse )
            {
                await Task.Factory.StartNew( () => onMessageReceived( message ), cancellationToken, TaskCreationOptions.AttachedToParent, TaskScheduler.Default );    
            }
            else
            {
                onMessageReceived( message );                     
            }
        }


        public override IMessageQueue GetResponseQueue()
        {
            if ( !( Pattern == MessagePattern.RequestResponse && Direction == Direction.Outbound ) )
                throw new InvalidOperationException( "Cannot get a response queue except for outbound request-response" );

            return this;
        }

        public override IMessageQueue GetReplyQueue( Message message )
        {
            if ( !( Pattern == MessagePattern.RequestResponse && Direction == Direction.Inbound ) )
                throw new InvalidOperationException( "Cannot get a reply queue except for inbound request-response" );

            return this;
        }

        [Obsolete( "Cannot create a response queue address for ZeroMQ")]
        protected override string CreateResponseQueueAddress()
        {
            throw new InvalidOperationException( "Cannot create a response queue address for ZeroMQ" );
        }

        public override void DeleteQueue()
        {
            // do nothing            
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing && _socket != null )
            {
                _socket.Dispose();
            }
        }
    }
}