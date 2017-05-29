using MessageQueue.Messaging.Extensions;
using System;
using System.IO;

namespace MessageQueue.Messaging
{
    public sealed class Message
    {
        public Type BodyType => Body.GetType();

        private Object _body;

        public object Body
        {
            get { return this._body; }
            set
            {
                _body = value;
                MessageType = _body.GetMessageType();
            }
        }

        public string ResponseAddressName { get; set; }

        public string MessageType { get; set; }

        public TBody BodyAs<TBody>()
        {
            return (TBody)Body;
        }

        public static Message FromJson(Stream jsonStream)
        {
            var message = jsonStream.ReadFromJson<Message>();
            message.Body = message.Body.ToString().ReadFromJson(message.MessageType);
            return message;
        }

        public static Message FromJson(string json)
        {
            var message = json.ReadFromJson<Message>();
            message.Body = message.Body.ToString().ReadFromJson(message.MessageType);
            return message;
        }
    }
}