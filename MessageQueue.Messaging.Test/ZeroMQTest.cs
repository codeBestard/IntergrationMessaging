using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using MessageQueue.Messaging.Specification;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace MessageQueue.Messaging.Test
{
    [TestClass]
    public class ZeroMQTest
    {
        [TestMethod]
        public void Fire_And_Forget()
        {
            using ( var sender = MessageQueueFactory.CreateOutbound( "ff", MessagePattern.FireAndForget ) )
            using ( var receiver = MessageQueueFactory.CreateInbound( "ff", MessagePattern.FireAndForget ) )
            {
                var outgoingMessage = new Message
                {
                    Body = new[] { "hello world" }
                };
                sender.Send( outgoingMessage );


                string result = string.Empty;
                receiver.Receive( incomingMessage => result = incomingMessage.BodyAs<string[]>()[0] );

                result.Should().Be( "hello world" );
            }
        }

        [TestMethod]
        public void Fire_And_Forget_Async()
        {

            using ( var sender = MessageQueueFactory.CreateOutbound( "ffasync", MessagePattern.FireAndForget ) )
            using ( var receiver = MessageQueueFactory.CreateInbound( "ffasync", MessagePattern.FireAndForget ) )
            {
                var outgoingMessage = new Message
                {
                    Body = new[] { "hello world" }
                };

                sender.Send( outgoingMessage );

                var cancellationTokenSource = new CancellationTokenSource( 2000 );
                var cancellationToken       = cancellationTokenSource.Token;                
                string result               = string.Empty;

                var task =  receiver.Listen( incomingMessage =>
                            {
                                result = incomingMessage.BodyAs<string[]>()[0];                
                            },
                cancellationToken );
                task.Wait();
                cancellationTokenSource.Cancel();

                result.Should().Be( "hello world" );
                Trace.WriteLine( result );
            }
        }
    

        [TestMethod]
        public void Reqest_Resonse()
        {

            using ( var sender = MessageQueueFactory.CreateOutbound( "ff", MessagePattern.RequestResponse ) )
            using ( var receiver = MessageQueueFactory.CreateInbound( "ff", MessagePattern.RequestResponse ) )
            {
                
                var responseQueue = sender.GetResponseQueue();

                var outgoingMessage = new Message
                {
                    Body = new[] { "how are you?" },
                    ResponseAddressName = responseQueue.MessageAddress.Address
                };
                sender.Send( outgoingMessage );


                
                var responseMessage = new Message
                {
                    Body = new[] { "I am fine, thank you." }
                };
                receiver.Receive( incomingMessage =>
                {
                    var replyQueue = receiver.GetReplyQueue( incomingMessage );
                    replyQueue.Send( responseMessage );
                } );


                
                string result = string.Empty;
                responseQueue.Receive( incomingMessage => result = incomingMessage.BodyAs<string[]>()[0] );

                result.Should().Be( "I am fine, thank you." );
            }

        }


        [TestMethod]
        public void Pusblish_Subscribe()
        {
            using ( var publisher = MessageQueueFactory.CreateOutbound( "topic", MessagePattern.PublishSubscribe ) )
            using ( var subscriber1 = MessageQueueFactory.CreateInbound( "subscription1", MessagePattern.PublishSubscribe ) )
            using ( var subscriber2 = MessageQueueFactory.CreateInbound( "subscription2", MessagePattern.PublishSubscribe ) )
            using ( var subscriber3 = MessageQueueFactory.CreateInbound( "subscription3", MessagePattern.PublishSubscribe ) )
            {
                var outgoingMessage = new Message
                {
                    Body = new[] { "hello world" }
                };

                publisher.Send( outgoingMessage );


                string result1, result2, result3;
                result1 = result2 = result3 = string.Empty;

                subscriber1.Receive( incomingMessage => result1 = incomingMessage.BodyAs<string[]>()[0] );
                subscriber2.Receive( incomingMessage => result2 = incomingMessage.BodyAs<string[]>()[0] );
                subscriber3.Receive( incomingMessage => result3 = incomingMessage.BodyAs<string[]>()[0] );

                result1.Should().Be( "hello world" );
                result2.Should().Be( "hello world" );
                result3.Should().Be( "hello world" );
            }
        }
    }
}
