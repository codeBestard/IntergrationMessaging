using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageQueue.Messaging.Specification;

using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MessageQueue.Messaging.Extensions;
using System.IO;
using System.Threading;

namespace MessageQueue.Messaging.Implementation.Azure
{
    public sealed class ServiceBusMessageQueue : MessageQueueBase
    {
        private QueueClient        _queueClient;      

        
        private TopicClient        _topicClient;
        private SubscriptionClient _subscriptionClient;

        public override string Name => "Azure";

        public override void InitializeOutbound( string addressName, MessagePattern pattern, bool isTemporary )
        {
            base.InitializeProperties( Direction.Outbound, addressName, pattern, isTemporary );
            var connectionStr = GetConnectionString();
            
            var factory = MessagingFactory.CreateFromConnectionString( connectionStr );
            if ( pattern == MessagePattern.PublishSubscribe )
            {
                this._topicClient = factory.CreateTopicClient( MessageAddress.Address );
                return;
            }

            this._queueClient = factory.CreateQueueClient( MessageAddress.Address );
        }

        public override void InitializeInbound( string addressName, MessagePattern pattern, bool isTemporary )
        {
            base.InitializeProperties( Direction.Inbound, addressName, pattern, isTemporary );
            var connectionStr = GetConnectionString();
            
            var factory = MessagingFactory.CreateFromConnectionString( connectionStr );
            if ( pattern == MessagePattern.PublishSubscribe )
            {                
                this._subscriptionClient          = factory.CreateSubscriptionClient( topicPath: base.MessageAddress.Topic,
                                                                                      name     : base.MessageAddress.Subscription );
                return;
            }

            this._queueClient = factory.CreateQueueClient( MessageAddress.Address );
        }

        public override void Send( Message message )
        {
            var brokeredMessage = new BrokeredMessage( messageBodyStream: message.ToJsonStream(),
                                                       ownsStream       : true ); 

            if ( base.Pattern == MessagePattern.PublishSubscribe )
            {
                _topicClient.Send( brokeredMessage );
                return;
            }

            _queueClient.Send( brokeredMessage );
        }

        protected override async Task ListenInteral( Action<Message> onMessageReceived, CancellationToken cancellationToken )
        {
           
            var options = new OnMessageOptions
            {                             
                MaxConcurrentCalls = 5,   
                AutoComplete       = true
            };
            options.ExceptionReceived += LogError_Options_ExceptionReceived;

            if ( Pattern == MessagePattern.PublishSubscribe )
            {
                _subscriptionClient.OnMessage( callback       : message => Handle( message, onMessageReceived ),
                                              onMessageOptions: options );
            }
            else
            {
                _queueClient.OnMessage( callback       : message => Handle( message, onMessageReceived ),
                                       onMessageOptions: options );
            }
            cancellationToken.WaitHandle.WaitOne();

            await Task.CompletedTask;
        }

        private void LogError_Options_ExceptionReceived( object sender, ExceptionReceivedEventArgs e )
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( $"Error: sender{sender}, Exception: {e.Exception.Message}" );
            Console.ResetColor();
        }

        public override async Task Receive( Action<Message> onMessageReceived,
                                      CancellationToken cancellationToken,
                                      int maximumWaitMilliseconds = 0 )
        {
            TimeSpan blockTime = TimeSpan.FromMilliseconds( maximumWaitMilliseconds );

            var brokeredMessage = Pattern == MessagePattern.PublishSubscribe
                                    ? this._subscriptionClient.Receive( blockTime )        
                                    : this._queueClient.Receive( blockTime );             

            if ( cancellationToken != CancellationToken.None )
            {             
                await Task.Factory.StartNew( () => Handle( brokeredMessage, onMessageReceived ) , cancellationToken,
                                                TaskCreationOptions.AttachedToParent,
                                                TaskScheduler.Default );
            }
            else
            {
                Handle( brokeredMessage, onMessageReceived );
            }
        }

        private void Handle( BrokeredMessage brokeredMessage, Action<Message> onMessageReceived )
        {
            var messageStream = brokeredMessage.GetBody<Stream>();
            var message       = Message.FromJson( messageStream );
            onMessageReceived( message );
        }


        public override IMessageQueue GetReplyQueue( Message message )
        {
            var replyQueue = MessageQueueFactory.CreateOutbound( message.ResponseAddressName,
                                                                MessagePattern.RequestResponse,
                                                                isTemporary: true );
            return replyQueue;
        }
        
        private string GetConnectionString()
        {
            return RequirePropertyValue<string>( "connectionstring" );
        }

        protected override string CreateResponseQueueAddress()
        {
            var nameSpaceMananger = NamespaceManager.CreateFromConnectionString( GetConnectionString() );
            var tempQueueName     = Guid.NewGuid().ToString().Substring( 0, 6 );
            nameSpaceMananger.CreateQueue( tempQueueName );
            return tempQueueName;
        }

        public override void DeleteQueue()
        {
            var nameManager = NamespaceManager.CreateFromConnectionString( GetConnectionString() );
            if ( nameManager.QueueExists( MessageAddress.Address ) )
            {
                nameManager.DeleteQueue( MessageAddress.Address );
            }
        }

        protected override void Dispose( bool disposing )
        {
        }
    }
}
