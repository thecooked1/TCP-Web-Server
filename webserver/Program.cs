using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;

public class SimpleWebServer
{
    private static readonly int Port = 8080; 
    private static readonly string WebRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "webroot"));

    public static void Main(string[] args)
    {
        Console.WriteLine($"WebRoot Directory: {WebRoot}");
        if (!Directory.Exists(WebRoot))
        {
            Console.WriteLine($"Error: WebRoot directory not found at {WebRoot}");
            return;
        }

        TcpListener? server = null;
        try
        {
            server = new TcpListener(IPAddress.Any, Port);
            server.Start();
            Console.WriteLine($"Server started on port {Port}. Listening for connections...");

            while (true) // Keep listening 
            {
                Console.WriteLine("Waiting for a connection...");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine($"Client connected from {((IPEndPoint?)client.Client.RemoteEndPoint)?.Address}");

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"SocketExpection: {e.Message}");
        }

        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occured: {e.Message}");
        }
        finally
        {
            server?.Stop();
            Console.WriteLine("Server stopped");
        }
    }


        private static void HandleClient (object? clientObj)
    {
        if (clientObj is not TcpClient client)
        {
            Console.WriteLine("Error: Invalid client object passed to thread.");
            return;
        }

        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Handling client...");
        try {
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))

            {
                string? requestLine = reader.ReadLine();

                if (string.IsNullOrEmpty(requestLine))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: " +
                        $"Empty request line received. Closing connection.");
                    SendErrorResponse(stream, "400", "Bad Request", "400 Bad Request", 
                        "The server could not understand the request due to malformed syntax.");
                    return;
                }

                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Request Line: {requestLine}");

                string? headerLine;
                while (!string.IsNullOrEmpty(headerLine = reader.ReadLine()))
                {
                    // Console.WriteLine($"Thread {threadId}: Header: {headerLine}"); // Uncomment to see all headers
                }

                string[] requestParts = requestLine.Split(' ');
                if (requestParts.Length < 3) 
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Malformed request line: {requestLine}");
                    SendErrorResponse(stream, "400", "Bad Request", "400 Bad Request", "Malformed request line.");
                    return;
                }

                string method = requestParts[0];
                string url = requestParts[1];
                //string httpVersion = requestParts[2];

                if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Method not allowed: {method}");
                    SendErrorResponse(stream, "405", "Method Not Allowed", "405 Method Not Allowed", 
                        "This server only supports GET requests.");
                    return;
                }

                string body = $"Method: {method}\r\nURL: {url}\r\n";
                string header = $"HTTP/1.1 200 OK\r\n" +
                                $"Content-Type: text/plain; charset=UTF-8\r\n" +
                                $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
                                $"Connection: close\r\n\r\n";
                byte[] responseBytes = Encoding.UTF8.GetBytes(header + body);
                stream.Write(responseBytes, 0, responseBytes.Length);
                stream.Flush();

                //Thread.Sleep(100);
                /*string body = "Hello from Thread"  + " " + $"{Thread.CurrentThread.ManagedThreadId}\r\nReceived: {requestLine}\r\n";
                string header = $"HTTP/1.1 200 OK\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
                                $"Connection: close\r\n\r\n";

                byte[] response = Encoding.UTF8.GetBytes(header + body);
                stream.Write(response, 0, response.Length);
                stream.Flush();*/

            }
            
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Response sent.");        
        }

        catch (IOException ex)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: IOException while handling client: {ex.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Error handling client: {e.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Client connection closed.");
        }
    }

    private static void SendGenericHttpResponse(NetworkStream stream, string httpStatusCodeAndReason, string contentType, byte[] content)
    {
        try
        {
            if (!stream.CanWrite)
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Cannot write to stream for response {httpStatusCodeAndReason}.");
                return;
            }
            string headers = $"HTTP/1.1 {httpStatusCodeAndReason}\r\n" +
                             $"Content-Type: {contentType}\r\n" +
                             $"Content-Length: {content.Length}\r\n" +
                             "Connection: close\r\n\r\n"; 

            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (content.Length > 0)
            {
                stream.Write(content, 0, content.Length);
            }
            stream.Flush();
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Sent response: {httpStatusCodeAndReason}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: IOException during SendGenericHttpResponse for {httpStatusCodeAndReason}: {ex.Message}");
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: ObjectDisposedException during SendGenericHttpResponse for {httpStatusCodeAndReason}: {ex.Message}");
        }
    }


    private static void SendErrorResponse(NetworkStream stream, string statusCode, string statusMessage, string title, string bodyContentHtml)
    {
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Sending error: {statusCode} {statusMessage}");
        string htmlResponse = $"<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>{title}</title></head><body><h1>{statusCode} {statusMessage}</h1><p>{bodyContentHtml}</p></body></html>";
        byte[] contentBytes = Encoding.UTF8.GetBytes(htmlResponse);
        SendGenericHttpResponse(stream, $"{statusCode} {statusMessage}", "text/html; charset=UTF-8", contentBytes);
    }


}