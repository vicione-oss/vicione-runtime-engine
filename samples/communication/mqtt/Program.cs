using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTDemo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Keine unsichere Zufälligkeitsstufe verwenden", Justification = "Wird nicht auf sicherheitsrelevante Weise verwendet.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1502:Übermäßige Komplexität vermeiden", Justification = "Umgestaltung für Beispielprojekt nicht notwendig")]
internal sealed class Program
{
    private static readonly List<IManagedMqttClient> s_clients = [];
    private static readonly Random s_randomizer = new();
    private static bool s_statisticsMode;
    private static bool s_lossCheckMode;
    private static int s_rxCount;
    private static ConcurrentQueue<int>? s_rxValues;
    private static Stopwatch? s_rxWatch;
    private static bool s_breakContinousLoop;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5398:Hartcodierte SslProtocols-Werte vermeiden", Justification = "Ausreichend für diese Beispiel-Anwendung.")]
    private static void Main(string[] args)
    {
        if (args.Length != 1)
            return;

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            s_breakContinousLoop = true;
        };

        var factory = new MqttFactory();

        // Server erstellen
        string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        using X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(currentPath, "certificate.pfx"), "test", X509KeyStorageFlags.Exportable);
        int serverPort = int.Parse(args[0], CultureInfo.InvariantCulture);
        MqttServerOptionsBuilder optionsBuilder = new MqttServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(serverPort)
            .WithMaxPendingMessagesPerClient(int.MaxValue)
            .WithEncryptionCertificate(certificate.Export(X509ContentType.Pfx))
            .WithEncryptionSslProtocol(System.Security.Authentication.SslProtocols.Tls12);
        MqttServer server = factory.CreateMqttServer(optionsBuilder.Build());
        server.StartAsync().Wait();

        bool exit = false;
        do
        {
            ShowMenu(serverPort);
            ConsoleKeyInfo info = Console.ReadKey();

            switch (info.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    // Client erstellen
                    Console.WriteLine();
                    Console.Write("Host (localhost): ");
                    string? host = Console.ReadLine();
                    host = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
                    Console.Write("Port (1883): ");
                    string? port_text = Console.ReadLine();
                    int port = string.IsNullOrWhiteSpace(port_text) ? 1883 : int.Parse(port_text, CultureInfo.InvariantCulture);
                    Console.WriteLine("Verbinde...");
                    Connect(factory, host, port).Wait();
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    // Wert publishen
                    Console.WriteLine();
                    string defaultTopic = (serverPort % 2 == 1) ? "abc/A/2" : "abc/B/2";
                    Console.Write($"Topic ({defaultTopic}): ");
                    string? topic = Console.ReadLine();
                    topic = string.IsNullOrWhiteSpace(topic) ? defaultTopic : topic;
                    int defaultValue = s_randomizer.Next();
                    Console.Write($"Zahl ({defaultValue}): ");
                    string? value_text = Console.ReadLine();
                    int value = string.IsNullOrWhiteSpace(value_text) ? defaultValue : int.Parse(value_text, CultureInfo.InvariantCulture);
                    MqttApplicationMessage newMessage = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(BitConverter.GetBytes(value))
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                            .WithRetainFlag()
                            .Build();
                    server.InjectApplicationMessage(new InjectedMqttApplicationMessage(newMessage)).Wait();
                    break;

                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    // kontinuierlich senden
                    Console.WriteLine();
                    string defaultTopicContinuous = (serverPort % 2 == 1) ? "abc/A/2" : "abc/B/2";
                    Console.Write($"Topic ({defaultTopicContinuous}): ");
                    string? topicContinuous = Console.ReadLine();
                    topicContinuous = string.IsNullOrWhiteSpace(topicContinuous) ? defaultTopicContinuous : topicContinuous;
                    Console.WriteLine();
                    Console.WriteLine("Ab jetzt wird in Dauerschleife gesendet. Beenden mit Strg+C.");
                    //string valueContinuous;
                    int valueContinuous = 0;
                    MqttApplicationMessage newMessageContinous;
                    int txCount = 0;
                    var txWatch = Stopwatch.StartNew();
                    s_breakContinousLoop = false;
#pragma warning disable CA1508 // Toten bedingten Code vermeiden: s_breakContinousLoop wird im Handler von Console.CancelKeyPress auf true gesetzt
                    while (s_breakContinousLoop == false)
#pragma warning restore CA1508 // Toten bedingten Code vermeiden
                    {
                        valueContinuous++;
                        newMessageContinous = new MqttApplicationMessageBuilder()
                            .WithTopic(topicContinuous)
                            .WithPayload(BitConverter.GetBytes(valueContinuous))
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                            .WithRetainFlag()
                            .Build();
                        server.InjectApplicationMessage(new InjectedMqttApplicationMessage(newMessageContinous)).Wait();
                        txCount++;
                        if (txWatch.ElapsedMilliseconds > 1000)
                        {
                            Console.WriteLine($"Gesendete Nachrichten: {txCount}/{txWatch.ElapsedMilliseconds / 1000.0}s");
                            txCount = 0;
                            txWatch.Restart();
                        }

                        if (txCount % 1000 == 0)
                            Thread.Sleep(20);

                        if (valueContinuous >= 500000)
                            break;
                    }
                    break;

                case ConsoleKey.M:
                    if (s_statisticsMode == false && s_lossCheckMode == false)
                    {
                        s_rxCount = 0;
                        s_rxWatch = Stopwatch.StartNew();
                        s_statisticsMode = true;
                    }
                    else if (s_statisticsMode == true)
                    {
                        s_rxWatch!.Stop();
                        s_rxValues = new ConcurrentQueue<int>();
                        s_lossCheckMode = true;
                        s_statisticsMode = false;
                    }
                    else
                        s_lossCheckMode = false;

                    break;

                case ConsoleKey.X:
                    exit = true;
                    break;
            }
        }
        while (exit == false);

        server.StopAsync().Wait();
        foreach (IManagedMqttClient client in s_clients)
            client.StopAsync().Wait();
    }

    private static void ShowMenu(int serverPort)
    {
        Console.Clear();
        Console.WriteLine($"Server hört auf Port {serverPort}.");
        Console.WriteLine();
        Console.WriteLine($"Der Server ist aktuell mit {s_clients.Count} Servern verbunden{(s_clients.Count > 0 ? ":" : ".")}");
        foreach (IManagedMqttClient client in s_clients)
            Console.WriteLine($"- {client.Options.ClientOptions.ChannelOptions}: Verbunden: {client.IsConnected}");
        Console.WriteLine();
        Console.WriteLine($"Modus: {GetModeText()}");
        Console.WriteLine();
        Console.WriteLine("1: mit neuem Server verbinden");
        Console.WriteLine("2: Wert veröffentlichen");
        Console.WriteLine("3: kontinuierlich senden");
        Console.WriteLine("M: Modus umschalten");
        Console.WriteLine("X: beenden");
        Console.WriteLine();
        Console.Write("Auswahl: ");
    }

    private static string GetModeText()
    {
        if (s_statisticsMode)
            return "Statistik";
        else if (s_lossCheckMode)
            return "auf verlorene Pakete prüfen";
        return "Anzeige";
    }

    private static async Task Connect(MqttFactory factory, string host, int port)
    {
        ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithTlsOptions(o => { })
                //.WithCleanSession(false) // wird vom ManagedClient nicht untersützt
                .Build())
            .Build();

        options.ClientOptions.ChannelOptions.TlsOptions.CertificateValidationHandler = (_) => true;

        IManagedMqttClient newClient = factory.CreateManagedMqttClient();

        bool stopHandling = false;
        newClient.ApplicationMessageReceivedAsync += e =>
        {
            if (s_statisticsMode)
            {
                Interlocked.Increment(ref s_rxCount);
                if (s_rxWatch!.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"Empfangene Nachrichten: {s_rxCount}/{s_rxWatch.ElapsedMilliseconds / 1000.0}s");
                    Interlocked.Exchange(ref s_rxCount, 0);
                    s_rxWatch.Restart();
                }
            }
            else if (s_lossCheckMode)
            {
                if (stopHandling)
                    return Task.CompletedTask;
                Interlocked.Increment(ref s_rxCount);
                s_rxValues!.Enqueue(BitConverter.ToInt32(e.ApplicationMessage.PayloadSegment));
                if (s_rxCount >= 500000)
                {
                    stopHandling = true;
                    int startValue = 0;
                    int? lastValue = null;
                    int errorCount = 0;
                    while (s_rxValues.TryDequeue(out int rxValue))
                    {
                        if (lastValue.HasValue == false)
                            lastValue = startValue = rxValue;
                        else if (rxValue != ++lastValue)
                        {
                            errorCount++;
                            lastValue = rxValue;
                        }
                    }

                    Console.WriteLine($"Start: {startValue}, Ende: {lastValue}; Es sind {errorCount} Pakete verloren gegangen.");
                    Console.ReadKey();
                }
            }
            else
                Console.WriteLine($"{host}:{port} Rx: {e.ApplicationMessage.Topic}: {BitConverter.ToInt32(e.ApplicationMessage.PayloadSegment)}");

            return Task.CompletedTask;
        };
        await newClient.SubscribeAsync([new MqttTopicFilterBuilder().WithTopic("abc/#").Build()]);
        await newClient.StartAsync(options);

        s_clients.Add(newClient);
    }
}
