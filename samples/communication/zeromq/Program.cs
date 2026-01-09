using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQDemo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Keine unsichere Zufälligkeitsstufe verwenden", Justification = "Wird nicht auf sicherheitsrelevante Weise verwendet.")]
internal sealed class Program
{
    private static readonly List<SubscriberSocket> s_subscribers = [];
    private static readonly Random s_randomizer = new();
    private static bool s_statisticsMode;
    private static int s_rxCount;
    private static Stopwatch? s_rxWatch;

    private static void Main(string[] args)
    {
        if (args.Length != 1)
            return;

        Console.CancelKeyPress += (s, e) => Environment.Exit(0);

        // Publisher erstellen
        using var pubSocket = new PublisherSocket();
        Console.WriteLine("Publisher socket binding...");
        pubSocket.Options.SendHighWatermark = 1000;
        pubSocket.Bind($"tcp://*:{args[0]}");

        bool exit = false;
        do
        {
            ShowMenu(args[0]);
            ConsoleKeyInfo info = Console.ReadKey();

            switch (info.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    // Subscriber erstellen
                    Console.WriteLine();
                    Console.Write("Host (localhost): ");
                    string? host = Console.ReadLine();
                    host = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
                    Console.Write("Port (12345): ");
                    string? port = Console.ReadLine();
                    port = string.IsNullOrWhiteSpace(port) ? "12345" : port;
                    Console.WriteLine("Verbinde...");
                    Task.Run(() => CreateSubscriber(host, port));
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    // Wert publishen
                    Console.WriteLine();
                    Console.Write($"Topic (abc/A/2): ");
                    string? topic = Console.ReadLine();
                    topic = string.IsNullOrWhiteSpace(topic) ? "abc/A/2" : topic;
                    string defaultValue = $"Die Zufallszahl beträgt {s_randomizer.Next()}";
                    Console.Write($"Wert ({defaultValue}): ");
                    string? value = Console.ReadLine();
                    value = string.IsNullOrWhiteSpace(value) ? defaultValue : value;
                    pubSocket.SendMoreFrame(topic).SendFrame(value);
                    break;

                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    // kontinuierlich senden
                    Console.WriteLine();
                    Console.Write($"Topic (abc/A/2): ");
                    string? topicContinuous = Console.ReadLine();
                    topicContinuous = string.IsNullOrWhiteSpace(topicContinuous) ? "abc/A/2" : topicContinuous;
                    Console.WriteLine();
                    Console.WriteLine("Ab jetzt wird in Dauerschleife gesendet. Beenden mit Strg+C.");
                    string valueContinuous;
                    //int valueContinuous;
                    int txCount = 0;
                    var txWatch = Stopwatch.StartNew();
                    while (true)
                    {
                        valueContinuous = $"Die Zufallszahl beträgt {s_randomizer.Next()}";
                        //valueContinuous = s_randomizer.Next();
                        pubSocket.SendMoreFrame(topicContinuous).SendFrame(valueContinuous);
                        txCount++;
                        if (txWatch.ElapsedMilliseconds > 1000)
                        {
                            Console.WriteLine($"Gesendete Nachrichten: {txCount}/{txWatch.ElapsedMilliseconds / 1000.0}s");
                            txCount = 0;
                            txWatch.Restart();
                        }

                        if (txCount % 1000 == 0)
                            Thread.Sleep(10);
                    }

                case ConsoleKey.M:
                    if (s_statisticsMode == false)
                    {
                        s_rxCount = 0;
                        s_rxWatch = Stopwatch.StartNew();
                        s_statisticsMode = true;
                    }
                    else
                    {
                        s_rxWatch!.Stop();
                        s_statisticsMode = false;
                    }

                    break;

                case ConsoleKey.X:
                    exit = true;
                    break;
            }
        }
        while (exit == false);
    }

    private static void ShowMenu(string publisherPort)
    {
        Console.Clear();
        Console.WriteLine($"Publisher läuft auf Port {publisherPort}.");
        Console.WriteLine();
        Console.WriteLine($"Die Anwendung ist aktuell mit {s_subscribers.Count} Publishern verbunden{(s_subscribers.Count > 0 ? ":" : ".")}");
        foreach (SubscriberSocket subscriber in s_subscribers)
            Console.WriteLine($"- {subscriber.Options.LastEndpoint}");
        Console.WriteLine();
        Console.WriteLine($"Modus: {(s_statisticsMode ? "Statistik" : "Anzeige")}");
        Console.WriteLine();
        Console.WriteLine("1: mit neuem Publisher verbinden");
        Console.WriteLine("2: Wert veröffentlichen");
        Console.WriteLine("3: kontinuierlich senden");
        Console.WriteLine("M: Modus umschalten");
        Console.WriteLine("X: beenden");
        Console.WriteLine();
        Console.Write("Auswahl: ");
    }

    private static void CreateSubscriber(string host, string port)
    {
        var subSocket = new SubscriberSocket();
        subSocket.Options.ReceiveHighWatermark = 1000;
        subSocket.Connect($"tcp://{host}:{port}");
        subSocket.Subscribe("abc/");
        s_subscribers.Add(subSocket);
        while (subSocket.IsDisposed == false)
        {
            string messageTopicReceived = subSocket.ReceiveFrameString();
            string messageReceived = subSocket.ReceiveFrameString();

            if (s_statisticsMode)
            {
                s_rxCount++;
                if (s_rxWatch!.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"Empfangene Nachrichten: {s_rxCount}/{s_rxWatch.ElapsedMilliseconds / 1000.0}s");
                    s_rxCount = 0;
                    s_rxWatch.Restart();
                }
            }
            else
                Console.WriteLine($"{host}:{port} Rx: {messageTopicReceived}: {messageReceived}");
        }
    }
}
