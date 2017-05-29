using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessageQueue.Messaging.Configuration;
using FluentAssertions;

namespace MessageQueue.Messaging.Test
{
    [TestClass]
    public class MessagingConfigurationTest
    {   

        [TestMethod]
        public void ParseConfig()
        {
            MessagingConfig.Current.Should().NotBeNull();
        }


    }
}
