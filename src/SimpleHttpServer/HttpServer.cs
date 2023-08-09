using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SimpleHttpServer;

/// <summary>
/// Represents a simple HTTP Service base on the <see cref="Socket"/> implementations.
/// </summary>
public class HttpServer : IAsyncDisposable
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    public HttpServer()
    {
        ip = IPAddress.Parse("127.0.0.1");
        port = 8888;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class with specific IpAddress and port.
    /// </summary>
    /// <param name="ip">The Server host IP.</param>
    /// <param name="port">The Server host Port.</param>
    public HttpServer(IPAddress ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }

    #endregion

    #region Implementations

    void Initial()
    {
        isAlive = true;
        if (socketServer is { Connected: false })
        {
            socketServer.Close();
            socketServer.Dispose();
            socketServer = null;
        }

        socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketServer.Bind(new IPEndPoint(ip, port));
        socketServer.Listen(1000);
    }

    /// <summary>
    /// Starts the HTTP Server.
    /// </summary>
    public async Task StartAsnyc()
    {
        Initial();
        httpHandler = new HttpServerHandler();
        while (isAlive)
        {
            try
            {
                Socket remote = await socketServer.AcceptAsync();
                Console.WriteLine($"Accepted the remote: {remote.RemoteEndPoint}, {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}");
                await Task.Factory.StartNew(() => httpHandler.HandleRequestAsync(remote));
                remote.Close();
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Initial();
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Utility

    /// <summary>
    /// The Socket.
    /// </summary>
    Socket socketServer;

    /// <summary>
    /// The Server host IP.
    /// </summary>
    readonly IPAddress ip;

    /// <summary>
    /// The Server host Port.
    /// </summary>
    readonly int port;

    /// <summary>
    /// Whether the HTTP Server is alive.
    /// </summary>
    bool isAlive;

    /// <summary>
    /// The <see cref="HttpServerHandler"/>
    /// </summary>
    HttpServerHandler httpHandler;

    #endregion
}