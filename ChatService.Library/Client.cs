using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatService.Library
{
    public class Client
    {
        // Data buffer to hold incoming data
        private byte[] _buffer = new byte[1024];

        private const int port = 11000;
        
        public static int backlog = 10;

        // Set the local endpoint for the socket.
        public static IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
        public static IPAddress IpAddress = ipHost.AddressList[1];
        public static IPEndPoint RemoteEndPoint = new IPEndPoint(IpAddress, port);

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone = 
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone = 
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = 
            new ManualResetEvent(false);

        // The response from the server
        private static string response = string.Empty;

        // TCP socket for listening to connections
        public static Socket client = new Socket(IpAddress.AddressFamily, 
            SocketType.Stream, ProtocolType.Tcp);

        public static void StartClient()
        {
            try
            {
                // Connect to the remote device
                client.BeginConnect(RemoteEndPoint, new AsyncCallback(ClientConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the remote device.
                Send(client," >> This is a test<EOF>");
                sendDone.WaitOne();

                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.
                Console.WriteLine($" >> Response received : '{response}'");

                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, string data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try 
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine($" >> Sent {bytesSent} bytes to server.");

                // Signal that all bytes have been sent.
                sendDone.Set();
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ClientConnectCallback(IAsyncResult ar)
        {
            try 
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket) ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine($" >> Socket connected to {client.RemoteEndPoint}"
                    );

                // Signal that the connection has been made.
                connectDone.Set();
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client) 
        {
            try 
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.ComSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive( state.Buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback( IAsyncResult ar ) {
            try {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject) ar.AsyncState;
                Socket client = state.ComSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0) {
                    // There might be more data, so store the data received so far.
                    state.Message += (Encoding.ASCII.GetString(state.Buffer,0,bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.Buffer,0,StateObject.BufferSize,0,
                        new AsyncCallback(ReceiveCallback), state);
                } else {
                    // All the data has arrived; put it in response.
                    if (state.Message.Length > 1) 
                    {
                        response = state.Message;
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
