using System;
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
    public async void DoItAsync(Socket remote)
    {
        // TODO: parse http header params
        // TODO: parse route
        // TODO: deal the request and return response data
        // Get the network stream
        NetworkStream stream = new(remote, true);

        // Check the HTTP protocol flag: GET / HTTP/1.1
        string requestLine = ReadHeaderLine(stream);
        if (!requestLine.Contains("HTTP/"))
            throw new Exception("Invalid HTTP Request.");
        string[] headflags = requestLine.Split(' ', StringSplitOptions.TrimEntries);
        if (headflags.Length != 3)
            throw new Exception("Invalid HTTP Request.");

        Console.WriteLine(requestLine);
    }

    string ReadHeaderLine(Stream stream)
    {
        StringBuilder headerLine = new();
        int data;
        while (true)
        {
            data = stream.ReadByte();
            switch (data)
            {
                case '\r': continue;
                case '\n': return headerLine.ToString();
                case -1: continue;
                default: headerLine.Append(Convert.ToChar(data)); break;
            }
        }
    }

    #endregion
}