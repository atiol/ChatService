using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ChatService.Library.Interfaces;
using Moq;
using NUnit.Framework;

namespace ChatService.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void Server_SetupServerToListenToClients_ServerIsSetToListeningMode()
        {
            var mock = new Mock<IMockSocket>();
            mock.Setup(m => m.Bind(new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], 11000)));
            mock.Setup(m => m.Listen(10));
        }
    }
}
