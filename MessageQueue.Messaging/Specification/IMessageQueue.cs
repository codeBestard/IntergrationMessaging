using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MessageQueue.Messaging.Specification
{
    public interface IMessageQueue : IDisposable
    {
        string Name { get; }
        MessageAddress MessageAddress { get; }

        bool IsTemporary { get; }
        IDictionary<string, object> Properties { get; }
        void InitializeOutbound(string addressName, MessagePattern pattern, bool isTemporary = false);
        void InitializeInbound(string addressName, MessagePattern pattern, bool isTemporary = false);

        void Send(Message message);

        Task Listen(Action<Message> onMessageReceived, CancellationToken callcellationToken);

        void Receive(Action<Message> onMessageReceived, int maximumWaitMilliseconds = 0);

        MessageAddress GetAddress(string addressName );

        IMessageQueue GetResponseQueue();

        IMessageQueue GetReplyQueue(Message message);

        void DeleteQueue();        
    }
}