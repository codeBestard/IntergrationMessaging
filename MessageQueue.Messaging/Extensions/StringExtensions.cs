using Newtonsoft.Json;
using System;

namespace MessageQueue.Messaging.Extensions
{
    public static class StringExtensions
    {
        public static object ReadFromJson(this string json, string messageType)
        {
            var type = Type.GetType(messageType);
            return JsonConvert.DeserializeObject(json, type);
        }

        public static T ReadFromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}