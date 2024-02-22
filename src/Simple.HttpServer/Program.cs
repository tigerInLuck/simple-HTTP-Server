// See https://aka.ms/new-console-template for more information

using SimpleHttpServer;
using System;

Console.WriteLine("Simple HTTP Server base on the .NET 6 Sockets ServiceProvider.");
HttpServer httpServer = new();
await httpServer.StartAsnyc();