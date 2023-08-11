using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    public HttpServerHandler(int worker)
    {
        RemoteSockets = new ConcurrentQueue<Socket>();
        for (int i = 0; i < worker; i++)
        {
            Thread threadWorker = new(FetchRequest)
            {
                Name = $"threadWorker-{i + 1}",
                IsBackground = true
            };
            threadWorker.Start();
            Console.WriteLine($"{threadWorker.Name} has been started...");
        }
    }

    #endregion

    #region Implementations

    /// <summary>
    /// Fetch the requests task.
    /// </summary>
    async void FetchRequest()
    {
        while (Thread.CurrentThread.IsAlive)
        {
            //Console.WriteLine($"RemoteSockets.Count = {RemoteSockets.Count}");
            if (RemoteSockets.TryDequeue(out Socket remote))
            {
                try
                {
                    await HandleRequestAsync(remote);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                remote.Close();
            }
            await Task.Delay(0);
        }
    }

    /// <summary>
    /// Handle the remote client connection.
    /// </summary>
    public async Task HandleRequestAsync(Socket remote)
    {
        //Stopwatch sw = Stopwatch.StartNew();
        //Console.WriteLine("Calling HandleRequestAsync()...");
        // TODO: parse http header params
        // TODO: parse route
        // TODO: deal the request and return response data
        // Get the network stream
        //Console.WriteLine($"{Thread.CurrentThread.Name}, remote.Connected={remote.Connected}");
        NetworkStream stream = new(remote);

        // Check the HTTP protocol Start-Line: GET / HTTP/1.1
        string startLine = ReadLine(stream);
        if (startLine == "-1")
            return;

        if (!startLine.Contains("HTTP/"))
            throw new Exception("Invalid HTTP Request.");
        string[] startLineFlags = startLine.Split(' ', StringSplitOptions.TrimEntries);
        if (startLineFlags.Length != 3)
            throw new Exception("Invalid HTTP Request.");
        //Console.WriteLine($"the header  -->  {requestLine}\r\n");

        // Cheek the request headers
        string headerLine;
        Dictionary<string, string> headers = new();
        do
        {
            headerLine = ReadLine(stream);
            if (headerLine is "") break;
            int separator = headerLine.IndexOf(':');
            if (separator == -1)
                throw new Exception($"Invalid HTTP Request Header Line: {headerLine}");
            headers.TryAdd(headerLine[..separator], headerLine[(separator + 1)..].Trim());
            //Console.WriteLine($"the header  -->  {headerLine}\r\n");
        } while (headerLine is not "");

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
        //await stream.DisposeAsync();

        //sw.Stop();
        //Console.WriteLine($"Cost time: {sw.Elapsed.TotalMilliseconds} ms");
    }

    string ReadLine(Stream stream)
    {
        /* see the all http request document:

        byte[] buffer = new byte[1000];
        int count = await stream.ReadAsync(buffer, 0, buffer.Length);
        string content = Encoding.UTF8.GetString(buffer);

        */

        //Console.WriteLine("Calling ReadLine()...");
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
                    //char c = Convert.ToChar(data);
                    //Console.WriteLine("get ASCII code: {0, -5}  -->  {1}", data, c);
                    headerLine.Append(Convert.ToChar(data));
                    break;
            }
        }
    }

    #endregion

    #region Utility

    public readonly ConcurrentQueue<Socket> RemoteSockets;

    #endregion
}

// =============== HTTP Request Headers like this: ===============

// GET / HTTP/1.1
// Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7
// Accept-Encoding: gzip, deflate, br
// Accept-Language: en-GB,en;q=0.9,zh-CN;q=0.8,zh;q=0.7
// Cache-Control: no-cache
// Connection: keep-alive
// Host: localhost:8888
// Pragma: no-cache
// Sec-Fetch-Dest: document
// Sec-Fetch-Mode: navigate
// Sec-Fetch-Site: none
// Sec-Fetch-User: ?1
// Upgrade-Insecure-Requests: 1
// User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188
// sec-ch-ua: "Not/A)Brand";v="99", "Microsoft Edge";v="115", "Chromium";v="115"
// sec-ch-ua-mobile: ?0
// sec-ch-ua-platform: "Windows"
//
// (the empty line "\r\n")
//
// content (body)...

// =============== HTTP Response Headers like this: ===============

// HTTP/1.1 200 OK
// Server: .NET 6 Sockets
// Content-Type: text/html; charset=utf-8
// Content-Length: 87
//
// (the empty line "\r\n")
//
// response content...

