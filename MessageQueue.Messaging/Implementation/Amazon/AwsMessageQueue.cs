using System;
using System.Collections.Generic;
using System.Linq;
using MessageQueue.Messaging.Specification;
using MessageQueue.Messaging.Extensions;

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using SQSModelNameSpace = Amazon.SQS.Model;

using Amazon.SimpleNotificationService;
using SNSModelNameSpace = Amazon.SimpleNotificationService.Model;
using System.Threading.Tasks;
using System.Threading;

namespace MessageQueue.Messaging.Implementation.Amazon
{
    public sealed class AwsMessageQueue : MessageQueueBase
    {
        private const string                          _accessKey = "accesskey";
        private const string                          _secretKey = "secretkey";
        private const string                          _regionEndpoint = "regionendpoint";
        private AmazonSQSClient                       _sqsClient;
        private AmazonSimpleNotificationServiceClient _snsClient;

        public override string Name => "AWS";

        private (string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint) AccessConfig => GetAccessConfiguration();

        private RegionEndpoint GetRegionEndpoint( string name )
        {
            return RegionEndpoint.EnumerableAllRegions
                                .First( endpoint =>
                                    endpoint.SystemName.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }

        private (string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint) GetAccessConfiguration()
        {
            return (RequirePropertyValue<string>( _accessKey ),
                    RequirePropertyValue<string>( _secretKey ),
                    GetRegionEndpoint(RequirePropertyValue<string>( _regionEndpoint ) ));
        }
                

        public override void InitializeOutbound( string addressName, MessagePattern pattern, bool isTemporary = false )
        {
            base.InitializeProperties( Direction.Outbound, addressName, pattern, isTemporary );
            
            if ( pattern == MessagePattern.PublishSubscribe )
            {
                _snsClient = new AmazonSimpleNotificationServiceClient( AccessConfig.accessKeyId,
                                                                        AccessConfig.secretAccessKey,
                                                                        AccessConfig.regionEndpoint );
                return;
            }
            _sqsClient = new AmazonSQSClient( AccessConfig.accessKeyId,
                                              AccessConfig.secretAccessKey,
                                              AccessConfig.regionEndpoint );
        }

        public override void InitializeInbound( string addressName, MessagePattern pattern, bool isTemporary = false )
        {
            base.InitializeProperties( Direction.Inbound, addressName, pattern, isTemporary );
            
            _sqsClient = new AmazonSQSClient( AccessConfig.accessKeyId,
                                              AccessConfig.secretAccessKey,
                                              AccessConfig.regionEndpoint );
        }

        public override async Task Receive( Action<Message> onMessageReceived,
                                        CancellationToken cancellationToken,
                                        int maximumWaitMilliseconds = 0 )
        {
            var receivedRequest = new ReceiveMessageRequest
            {
                QueueUrl            = MessageAddress.Address,
                WaitTimeSeconds     = maximumWaitMilliseconds / 1000,
                MaxNumberOfMessages = 5
            };

            var receivedReponse = _sqsClient.ReceiveMessage( receivedRequest );
            

            if ( receivedReponse.Messages.Any() )
            {  
                receivedReponse.Messages.Where( sqsMessage => sqsMessage != null )
                                        .ToList()
                                        .ForEach( sqsMessage =>
                                         {
                                             if ( cancellationToken != CancellationToken.None )
                                             {  
                                                Task.Factory.StartNew( () => Handle( sqsMessage, onMessageReceived ), cancellationToken,
                                                                                TaskCreationOptions.AttachedToParent,
                                                                                TaskScheduler.Default );
                                             }
                                             else
                                             { 
                                                 Handle( sqsMessage, onMessageReceived );                                                 
                                             }
                                         } );
            }            
        }

        private void Handle( SQSModelNameSpace.Message sqsMessage, Action<Message> onMessageReceived )
        {
            var message = Message.FromJson( sqsMessage.Body );
            onMessageReceived( message );

            DeleteMessage( sqsMessage );
        }

        private void DeleteMessage( SQSModelNameSpace::Message message )
        {
            var deleteMessageRequest = new DeleteMessageRequest
            {
                QueueUrl      = MessageAddress.Address,
                ReceiptHandle = message.ReceiptHandle
            };

            var deleteMessageResponse = _sqsClient.DeleteMessage( deleteMessageRequest );

            if ( deleteMessageResponse.HttpStatusCode != System.Net.HttpStatusCode.OK )
            {
                // throw and log something if failed
            }
        }

        public override void Send( Message message )
        {
            if ( Pattern == MessagePattern.PublishSubscribe )
            {
                var publishRequest      = new SNSModelNameSpace::PublishRequest();
                publishRequest.Message  = message.ToJsonString();
                publishRequest.TopicArn = MessageAddress.Address;
                _snsClient.Publish( publishRequest );
            }
            else
            {
                var sendRequest = new SendMessageRequest()
                {
                    MessageBody = message.ToJsonString(),
                    QueueUrl    = MessageAddress.Address
                };
                _sqsClient.SendMessage( sendRequest );
            }
        }


        protected override string CreateResponseQueueAddress()
        {
            var createQueueRequest = new CreateQueueRequest()
            {
                QueueName = Guid.NewGuid().ToString().Substring( 0, 6 )
            };
     
            var createQueueResponse = _sqsClient.CreateQueue( createQueueRequest );

            return createQueueResponse.QueueUrl;  
        }

        public override IMessageQueue GetReplyQueue( Message message )
        {
            var replyQueue = MessageQueueFactory.CreateOutbound( message.ResponseAddressName,
                                                                 MessagePattern.RequestResponse,
                                                                 isTemporary: true );
            return replyQueue;
        }



        public override void DeleteQueue()
        {
            var deleteQueueRequest = new DeleteQueueRequest( MessageAddress.Address );
            var deleteQueueResponse = _sqsClient.DeleteQueue( deleteQueueRequest );

            if ( deleteQueueResponse.HttpStatusCode != System.Net.HttpStatusCode.OK )
            {
                // throw or log something
            }
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing && _sqsClient != null )
            {
                _sqsClient.Dispose();
            }
        }


    }
}
