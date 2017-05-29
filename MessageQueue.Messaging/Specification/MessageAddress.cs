using System;
using System.Linq;

namespace MessageQueue.Messaging.Specification
{
    public sealed class MessageAddress : IEquatable<MessageAddress>
    {
        private string _topic;
        private string _subscription;

        public string Address { get; }

        public string Topic
        {
            get
            {
                SplitTopicSubscription();
                return _topic;
            }
        }
        public string Subscription
        {
            get
            {
                SplitTopicSubscription();
                return _subscription;
            }
        }


        public MessageAddress( string addressName )
        {
            if ( string.IsNullOrWhiteSpace( addressName ) ) throw new ArgumentNullException( nameof( addressName ) );

            this.Address = addressName;
        }

        private void SplitTopicSubscription()
        {
            if ( !String.IsNullOrWhiteSpace( _topic ) || !String.IsNullOrWhiteSpace( _subscription ) )
                return;

            (_topic, _subscription) = GetTopicSubScription( Address );

            (string topic, string subscription) GetTopicSubScription( string messageAddress )
            {
                const char colon = ':';

                if ( messageAddress.IndexOf( colon ) < 0 )
                    return (string.Empty, string.Empty);

                var addressParts     = messageAddress.Split( colon );              
                var topPath          = addressParts[0];
                var subscriptionName = addressParts[1];

                return (topPath, subscriptionName);
            }
        }
  

        public MessageAddress Build( string addressName )
        {
            return new MessageAddress( addressName );
        }

        public override string ToString()
        {
            return this.Address;
        }

        public bool Equals( MessageAddress other )
        {
            if ( ReferenceEquals( other, null ) )
            {
                return false;
            }

            var result = this.Address.Equals( other.Address, StringComparison.OrdinalIgnoreCase );
            return result;
        }

        public override bool Equals( object obj )
        {
            var result = Equals( obj as MessageAddress );
            return result;
        }

        public override int GetHashCode()
        {
            var result = this.Address.GetHashCode();
            return result;
        }

        public static Boolean operator ==( MessageAddress target, MessageAddress other )
        {
            if ( ReferenceEquals( target, other ) )
            {
                return true;
            }

            if ( ReferenceEquals( target, null ) )
            {
                return false;
            }

            var result = target.Equals( other );

            return result;
        }

        public static Boolean operator !=( MessageAddress target, MessageAddress other )
        {
            var result = !( target == other );
            return result;
        }
    }

}
