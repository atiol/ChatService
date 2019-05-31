using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatService.Library
{
    public class Server
    {
        // Data buffer to hold incoming data
        private byte[] _buffer = new byte[1024];

        private const int port = 11000;
        
        public static int backlog = 10;

        // Set the local endpoint for the socket.
        public static IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
        public static IPAddress IpAddress = ipHost.AddressList[1];
        public static IPEndPoint LocalEndPoint = new IPEndPoint(IpAddress, port);

        // Manually control thread signals
        public static ManualResetEvent AllDone = new ManualResetEvent(false);

        // TCP socket for listening to connections
        public static Socket listener = new Socket(AddressFamily.InterNetwork, 
            SocketType.Stream, ProtocolType.Tcp);

        public static void Run()
        {
            try
            {
                listener.Bind(LocalEndPoint);
                listener.Listen(backlog);

                while (true)
                {
                    // Set the event to non signaled state
                    AllDone.Reset();

                    // Listen for connections
                    Console.WriteLine("\n >> Waiting for connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // For until a client connects before continuing.
                    AllDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Press Enter to continue...");
            Console.Read();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue
            AllDone.Set();

            // Extract client socket
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            // Set state object parameters
            StateObject state = new StateObject();
            state.ComSocket = handler;

            // Begin Receiving bytes from client socket
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), state);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            // Retrieve the state object that was passed earlier on
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.ComSocket;

            // Read data from the client socket
            int bytesReceived = handler.EndReceive(ar);

            if (bytesReceived > 0)
            {
                state.Message += Encoding.ASCII.GetString(state.Buffer, 0, bytesReceived);

                content = state.Message;

                if(content.IndexOf("<EOF>") > -1)
                    Console.WriteLine($" >> Read {bytesReceived} bytes from socket.\n >> Data '{content}'");

                Send(handler, "Hello From Server");
            }
            else
            {
                // There is still more data to read
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
        }

        private static void Send(Socket handler, string content)
        {
            // Convert content to bytes for sending
            var byteData = Encoding.ASCII.GetBytes(content);

            // Begin sending data
            handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try 
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine($" >> Sent {bytesSent} bytes to client.");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
