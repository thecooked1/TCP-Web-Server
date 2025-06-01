using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;

public class SimpleWebServer
{
    private static readonly int Port = 8080; 
    private static readonly string WebRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "webroot"));
    private static readonly string[] AllowedExtensions = { ".html", ".css", ".js" };

    public static void Main(string[] args)
    {
        Console.WriteLine($"WebRoot Directory: {WebRoot}");
        if (!Directory.Exists(WebRoot))
        {
            Console.WriteLine($"Attempting to create WebRoot directory at {WebRoot}");

            try
            {
                Directory.CreateDirectory(WebRoot);
                Console.WriteLine("WebRoot directory created. Please add your static files (index.html, etc.) there.");
                File.WriteAllText(Path.Combine(WebRoot, "index.html"),
                    "<!DOCTYPE html><html><head><title>Welcome</title></head><body><h1>Server is Running!</h1>" +
                    "<p>This is the default index.html in your webroot.</p></body></html>");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: Could not create WebRoot directory at {WebRoot}. {ex.Message}");
                return;
            }
        }

        TcpListener? server = null;
        try
        {
            server = new TcpListener(IPAddress.Any, Port);
            server.Start();
            Console.WriteLine($"Server started on port {Port}. Listening for connections...");

            while (true) // Keep listening 
            {
                //Console.WriteLine("Waiting for a connection...");
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

        NetworkStream? stream = null;
        string clientInfoForLog = "Unknown client";
        try
        {
            
            var remoteEndPoint = (IPEndPoint?)client.Client?.RemoteEndPoint; 
            if (remoteEndPoint != null)
            {
                clientInfoForLog = remoteEndPoint.Address?.ToString() ?? "Unknown IP";
            }
            else if (client.Client == null)
            {
                clientInfoForLog = "Client socket already null";
            }
            else
            {
                clientInfoForLog = "Client RemoteEndPoint null";
            }
        }
        catch (Exception ex) 
        {
            clientInfoForLog = $"Error getting client IP: {ex.Message}";
        }

        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Handling client from {clientInfoForLog}...");

        // Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}:" +
        //$" Handling client from {((IPEndPoint?)client.Client.RemoteEndPoint)?.Address}...");

        try
        {
            stream = client.GetStream();
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

                //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Request Line: {requestLine}");

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
                string requestedPath = url.Contains('?') ? url.Substring(0, url.IndexOf('?')) : url;
                string decodedPath = WebUtility.UrlDecode(requestedPath);

                

                if (decodedPath == "/") { decodedPath = "/index.html"; }

                string safeFileName = decodedPath.TrimStart('/');

                if (safeFileName.Contains(".."))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Forbidden path (contains ..): {safeFileName}");
                    SendErrorResponse(stream, "403", "Forbidden", "403 Forbidden", "Path traversal attempt detected.");
                    return;
                }

                string fullPath = Path.GetFullPath(Path.Combine(WebRoot, safeFileName));

                if (!fullPath.StartsWith(WebRoot, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Forbidden access attempt. Requested: '{safeFileName}', Resolved: '{fullPath}' is outside WebRoot.");
                    SendErrorResponse(stream, "403", "Forbidden", "403 Forbidden", "Access to the requested resource is denied.");
                    return;
                }


                string extension = Path.GetExtension(fullPath).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Forbidden file type requested for {clientInfoForLog}: {extension} for {fullPath} (URL: {url})");
                    SendErrorResponse(stream, "403", "Forbidden (File Type)", "403 Forbidden", $"File type '{WebUtility.HtmlEncode(extension)}' is not supported.");
                    return;
                }

                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Serving file for {clientInfoForLog}: {fullPath} (URL: {url})");
                    byte[] fileBytes = File.ReadAllBytes(fullPath);
                    string contentType = GetContentType(extension);
                    SendGenericHttpResponse(stream, "200 OK", contentType, fileBytes);
                }
                else
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: File not found for {clientInfoForLog}: {fullPath} (URL: {url})");
                    SendErrorResponse(stream, "404", "Not Found", "404 Not Found", $"The requested resource '{WebUtility.HtmlEncode(safeFileName)}' was not found on this server.");
                    return;
                   
                }
            }   
        }

        catch (IOException ex)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: IOException while handling client {clientInfoForLog}: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: ArgumentException (likely invalid path chars): {ex.Message}");
            if (stream != null && stream.CanWrite)
            {
                SendErrorResponse(stream, "400", "Bad Request", "400 Bad Request", "The requested path contains invalid characters.");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Error handling client: {e.Message}\n{e.StackTrace}");
            if (stream != null && stream.CanWrite)
            {
                try { SendErrorResponse(stream, "500", "Internal Server Error", "500 Internal Server Error", "An unexpected error occurred."); return; }
                catch (Exception iseEx) { Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Also failed to send 500 error for {clientInfoForLog}: {iseEx.Message}"); }
            }
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Exception during client.Close() for {clientInfoForLog}: {ex.Message}");
            }
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Client connection closed for {clientInfoForLog}.");
            Console.Out.Flush();
        }
    }


    private static string GetContentType(string extension) 
    {
        return extension.ToLowerInvariant() switch 
        {
            ".html" => "text/html; charset=UTF-8",
            ".css" => "text/css; charset=UTF-8",
            ".js" => "application/javascript; charset=UTF-8",
            _ => "application/octet-stream", 
        };
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