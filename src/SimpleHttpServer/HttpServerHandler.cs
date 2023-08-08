using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SimpleHttpServer;

/// <summary>
/// The handler of HTTP Server.
/// </summary>
public class HttpServerHandler
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerHandler"/> class.
    /// </summary>
    public HttpServerHandler()
    {

    }

    #endregion

    #region Implementations

    /// <summary>
    /// Handle the remote client connection.
    /// </summary>
    public async void HandleRequestAsync(Socket remote)
    {
        Stopwatch sw = Stopwatch.StartNew();
        //Console.WriteLine("Calling HandleRequestAsync()...");
        // TODO: parse http header params
        // TODO: parse route
        // TODO: deal the request and return response data
        // Get the network stream
        NetworkStream stream = new(remote, true);

        // Check the HTTP protocol flag: GET / HTTP/1.1
        string requestLine = ReadHeaderLine(stream);
        if (requestLine == "-1")
            return;

        if (!requestLine.Contains("HTTP/"))
            throw new Exception("Invalid HTTP Request.");
        string[] headflags = requestLine.Split(' ', StringSplitOptions.TrimEntries);
        if (headflags.Length != 3)
            throw new Exception("Invalid HTTP Request.");
        //Console.WriteLine($"the header  -->  {requestLine}\r\n");

        // Cheek the request header
        string headerLine;
        Dictionary<string, string> headers = new();
        do
        {
            headerLine = ReadHeaderLine(stream);
            if (string.IsNullOrWhiteSpace(headerLine) || headerLine == "-1") break;
            int separator = headerLine.IndexOf(':');
            if (separator == -1)
                throw new Exception($"Invalid HTTP Request Header Line: {headerLine}");
            headers.TryAdd(headerLine[..separator], headerLine[(separator + 1)..].Trim());
            //Console.WriteLine($"the header  -->  {headerLine}\r\n");
        } while (!string.IsNullOrWhiteSpace(headerLine));

        // Check the request content
        string content = null;
        byte[] contentBytes = null;
        if (headers.TryGetValue("Content-Length", out string length))
        {
            //Console.WriteLine(length);
            int total = Convert.ToInt32(length);
            int canRead = total;
            contentBytes = new byte[total];
            while (canRead > 0)
            {
                byte[] buffer = new byte[canRead > 1024 ? 1024 : canRead];
                int count = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (count <= 0)
                    continue;
                buffer.CopyTo(contentBytes, total - canRead);
                canRead -= count;
            }
            content = Encoding.UTF8.GetString(contentBytes);
        }
        //Console.WriteLine($"the http request content  -->  {content}");

        // Response
        byte[] headerToken1 = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nServer: .NET 6 Sockets\r\n");
        await stream.WriteAsync(headerToken1, 0, headerToken1.Length);

        byte[] respBytes;
        if (contentBytes is { Length: > 0 })
        {
            respBytes = Encoding.UTF8.GetBytes($"{content}\r\n{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}");
            byte[] headerToken2 = Encoding.UTF8.GetBytes($"Content-Type: text/html; charset=utf-8\r\nContent-Length: {respBytes.Length}\r\n\r\n");
            await stream.WriteAsync(headerToken2, 0, headerToken2.Length);
        }
        else
        {
            respBytes = Encoding.UTF8.GetBytes($"This is a default response content by Simple-HTTP-Server...\r\n{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}");
            byte[] headerToken2 = Encoding.UTF8.GetBytes($"Content-Type: text/html; charset=utf-8\r\nContent-Length: {respBytes.Length}\r\n\r\n");
            await stream.WriteAsync(headerToken2, 0, headerToken2.Length);
        }

        await stream.WriteAsync(respBytes, 0, respBytes.Length);
        await stream.FlushAsync();
        stream.Close();
        await stream.DisposeAsync();

        sw.Stop();
        Console.WriteLine($"Cost time: {sw.Elapsed.TotalMilliseconds} ms");
    }

    string ReadHeaderLine(Stream stream)
    {
        /* see the all http request document:

        byte[] buffer = new byte[1000];
        int count = await stream.ReadAsync(buffer, 0, buffer.Length);
        string content = Encoding.UTF8.GetString(buffer);

        */

        //Console.WriteLine("Calling ReadHeaderLine()...");
        StringBuilder headerLine = new();
        while (true)
        {
            int data = stream.ReadByte();
            switch (data)
            {
                case '\r':
                    //Console.WriteLine(@"get ASCII code: {0, -5}  -->  \r", data);
                    continue;
                case '\n':
                    //Console.WriteLine(@"get ASCII code: {0, -5}  -->  \n", data);
                    return headerLine.ToString();
                case -1:
                    //Console.WriteLine("-1: stream.DisposeAsync()...");
                    return "-1";
                default:
                    char c = Convert.ToChar(data);
                    //Console.WriteLine("get ASCII code: {0, -5}  -->  {1}", data, c);
                    headerLine.Append(c);
                    break;
            }
        }
    }

    #endregion
}