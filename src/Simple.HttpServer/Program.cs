// See https://aka.ms/new-console-template for more information

using System;
using SimpleHttpServer;

Console.WriteLine("Simple HTTP Server base on the Sockets.");
HttpServer httpServer = new();
await httpServer.StartAsnyc();