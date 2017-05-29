
using MessageQueue.Messaging.Specification;
using System.Threading;


namespace MessageQueue.Handler
{
    public sealed class QueueListener
    {
        private CancellationTokenSource _cancellationTokenSource;

        public void Start( string queueName )
        {
            
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private void StartListening( string name, MessagePattern pattern )
        {            
        }

       
    }
}
