using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Library.Interfaces
{
    public interface IMockSocket
    {
        void Bind(IPEndPoint localEndPoint);
        void Listen(int backlog);
        //void Close();
        //void Shutdown();
    }
}
