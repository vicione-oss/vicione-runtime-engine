using Amqp;
//using Amqp.Sasl;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AMQPDemoClientOnly;

internal sealed class Program
{
    // getestet mit RabbitMQ im Docker Container: 
    // > docker run -d --hostname my-rabbit --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:management
    // Management ist dann zu finden unter: http://localhost:15672, Anmeldung guest:guest
    // Damit dieser Client sich verbinden kann, muss noch das AMQP 1.0 Plugin aktiviert werden:
    // > docker exec some-rabbit rabbitmq-plugins enable rabbitmq_amqp1_0

    // alternativ mit ActiveMQ Artemis im Docker Container
    // > docker run -it --rm -p 5672:5672 -p 8161:8161 -p 61616:61616 vromero/activemq-artemis
    // Management ist dann zu finden unter: http://localhost:8161, Anmeldung artemis:simetraehcapa

    // alternativ mit Apache Qpid im docker Container
    // > docker run --name some-qpid -p 5672:5672 -p 8080:8080 abh1sh3k/apache-qpid
    // Management ist dann zu finden unter: http://localhost:8080, Anmeldung admin:admin
    // im Web-Interface muss noch ein Anonymous-Authentication Provider angelegt werden und AMQP auf diesen umgestellt werden
    // mit Apache Qpid kommt allerdings beim Senden oder Empfangen eine Meldung "amqp:not-found" (vermutlich weil die Queue nicht existiert?)

    private static bool s_exit;
    private static bool s_statisticsMode;
    private static int s_rxCount;
    private static System.Diagnostics.Stopwatch? s_rxWatch;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Keine unsichere Zufälligkeitsstufe verwenden", Justification = "Wird nicht auf sicherheitsrelevante Weise verwendet.")]
    public static void Main()
    {
        Console.CancelKeyPress += (s, e) => Environment.Exit(0);

        //Trace.TraceLevel = TraceLevel.Frame;
        Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]", CultureInfo.InvariantCulture) + " " + string.Format(CultureInfo.InvariantCulture, f, a));

        // RabbitMQ und Apache Qpid:
        var address = new Address("amqp://guest:guest@localhost:5672");
        // ActiveMQ Artemis:
        //var address = new Address("amqp://artemis:simetraehcapa@localhost:5672");
        // Apache Qpid:
        //var connection = new Connection(address, SaslProfile.Anonymous, null, null);
        // alle anderen:
        var connection = new Connection(address);
        var session = new Session(connection);
        var sender = new SenderLink(session, "sender-link", "q1");
        var randomizer = new Random();

        do
        {
            ShowMenu();
            ConsoleKeyInfo info = Console.ReadKey();

            switch (info.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    // Receiver Link erstellen
                    Console.WriteLine();
                    Console.Write("Queue name (q1): ");
                    string? queueName = Console.ReadLine();
                    queueName = string.IsNullOrWhiteSpace(queueName) ? "q1" : queueName;
                    Console.WriteLine("Verbinde...");
                    Task.Run(() => CreateReceiverLink(session, "receiver-link", queueName));
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    // Wert publishen
                    Console.WriteLine();
                    Console.Write($"Subject (abc/A/2): ");
                    string? subject = Console.ReadLine();
                    subject = string.IsNullOrWhiteSpace(subject) ? "abc/A/2" : subject;
                    string defaultValue = $"Die Zufallszahl beträgt {randomizer.Next()}";
                    Console.Write($"Wert ({defaultValue}): ");
                    string? value = Console.ReadLine();
                    value = string.IsNullOrWhiteSpace(value) ? defaultValue : value;
                    //var message = new Message(Encoding.UTF8.GetBytes(value));
                    using (var message = new Message(value))
                    {
                        //message.Properties = new Amqp.Framing.Properties();
                        //message.Properties.Subject = subject;
                        sender.Send(message);
                    }
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
                    var txWatch = System.Diagnostics.Stopwatch.StartNew();
                    while (true)
                    {
                        valueContinuous = $"Die Zufallszahl beträgt {randomizer.Next()}";
                        //valueContinuous = s_randomizer.Next();
                        var message_continous = new Message(valueContinuous);
                        sender.Send(message_continous);
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
                        s_rxWatch = System.Diagnostics.Stopwatch.StartNew();
                        s_statisticsMode = true;
                    }
                    else
                    {
                        s_rxWatch!.Stop();
                        s_statisticsMode = false;
                    }

                    break;

                case ConsoleKey.X:
                    s_exit = true;
                    break;
            }
        }
        while (s_exit == false);

        sender.Close();
        session.Close();
        connection.Close();
    }

    private static void ShowMenu()//string publisherPort)
    {
        Console.Clear();
        Console.WriteLine($"Client läuft"); //auf Port {publisherPort}.");
        Console.WriteLine();
        //Console.WriteLine($"Die Anwendung ist aktuell mit {s_subscribers.Count} Publishern verbunden{(s_subscribers.Any() ? ":" : ".")}");
        //if (s_subscribers.Any())
        //    foreach (SubscriberSocket subscriber in s_subscribers)
        //        Console.WriteLine($"- {subscriber.Options.LastEndpoint}");
        //Console.WriteLine();
        Console.WriteLine($"Modus: {(s_statisticsMode ? "Statistik" : "Anzeige")}");
        Console.WriteLine();
        Console.WriteLine("1: Receiver-Link erstellen");
        Console.WriteLine("2: Wert veröffentlichen");
        Console.WriteLine("3: kontinuierlich senden");
        Console.WriteLine("M: Modus umschalten");
        Console.WriteLine("X: beenden");
        Console.WriteLine();
        Console.Write("Auswahl: ");
    }

    private static void CreateReceiverLink(Session session, string name, string address)
    {
        var receiver = new ReceiverLink(session, name, address);
        receiver.SetCredit(100000);
        while (receiver.IsClosed == false)
        {
            try
            {
                Message message = receiver.Receive(Timeout.InfiniteTimeSpan);
                string? messageSubjectReceived = message.Properties?.Subject;
                string messageReceived = message.GetBody<string>();

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
                    Console.WriteLine($"Rx: {messageSubjectReceived}: {messageReceived}");

                receiver.Accept(message);
            }
            catch (Exception ex)
            {
                if (s_exit)
                    return;
                else
                    Console.WriteLine("Exception occured: " + ex.ToString());
            }
        }
    }
}
