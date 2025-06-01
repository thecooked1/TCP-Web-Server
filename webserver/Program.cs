using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class SimpleWebServer
{
    private static readonly int Port = 8080; 
    private static readonly string WebRoot = Path.Combine(AppContext.BaseDirectory, "webroot"); 

    public static void Main(string[] args)
    {
        Console.WriteLine($"WebRoot Directory: {Path.GetFullPath(WebRoot)}");
        if (!Directory.Exists(WebRoot))
        {
            Console.WriteLine($"Error: WebRoot directory not found at {Path.GetFullPath(WebRoot)}");
            Console.WriteLine("Please create it and add some files (e.g., index.html).");
            Console.WriteLine("Ensure 'Copy to Output Directory' is set for webroot files in your .csproj.");
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
                Console.WriteLine("Client connected!");

                
                NetworkStream stream = client.GetStream();
                string body = "Hello from server!\r\n";
                string header = $"HTTP/1.1 200 OK\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
                                $"Connection: close\r\n\r\n";

                byte[] response = Encoding.UTF8.GetBytes(header + body);
                stream.Write(response, 0, response.Length);
                stream.Flush();

                Console.WriteLine("Sent basic response and closing connection.");
                client.Close();
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"SocketException: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occurred: {e.Message}");
        }
        finally
        {
            server?.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}