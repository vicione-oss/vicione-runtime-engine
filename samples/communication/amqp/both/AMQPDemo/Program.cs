using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using System;
using System.Threading.Tasks;

namespace AMQPDemo;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Environment.Exit(0);
        };

        var dataProducer = new DataProducer();

        // server
        string serverPort = "5672";
        if (args.Length > 0)
            serverPort = args[0];

        var address = new Address($"amqp://guest:guest@127.0.0.1:{serverPort}");

        var host = new ContainerHost(address);
        host.Open();
        host.RegisterLinkProcessor(new LinkProcessor(dataProducer));
        Console.WriteLine("Container host is listening on {0}:{1}", address.Host, address.Port);

        // client
        Console.WriteLine("Start the client");
        var client = new Client(address);
        var task = Task.Run(client.Run);

        Task.Delay(1000).Wait();
        dataProducer.Produce(1, 50000);

        Console.WriteLine("Press enter key to exit...");
        Console.ReadLine();

        host.Close();
    }

    private sealed class LinkProcessor(DataProducer dataProducer) : ILinkProcessor
    {
        private readonly DataProducer _dataProducer = dataProducer;

        public void Process(AttachContext attachContext)
        {
            if (!attachContext.Attach.Role)
            {
                attachContext.Complete(new Error(ErrorCode.NotAllowed) { Description = "Only receiver link is allowed." });
                return;
            }

            string address = ((Source)attachContext.Attach.Source).Address;
            if (!string.Equals(Client.DataPipeAddress, address, StringComparison.OrdinalIgnoreCase))
            {
                attachContext.Complete(new Error(ErrorCode.NotFound) { Description = "Cannot find address " + address });
                return;
            }

            Console.WriteLine("A client has connected. LinkName " + attachContext.Attach.LinkName);
            attachContext.Complete(new DataPipeLinkEndpoint(attachContext.Link, _dataProducer), 0);
        }
    }

    private sealed class DataPipeLinkEndpoint : LinkEndpoint
    {
        private readonly ListenerLink _link;
        private readonly DataProducer _dataProducer;

        public DataPipeLinkEndpoint(ListenerLink link, DataProducer dataProducer)
        {
            _link = link;
            _dataProducer = dataProducer;
            _dataProducer.DataProduced += OnDataProducerDataProduced;
        }

        private void OnDataProducerDataProduced(object? sender, object e)
        {
            using var message = new Message(e);
            try
            {
                _link.SendMessage(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: " + exception.Message);
            }
        }

        public override void OnDisposition(DispositionContext dispositionContext) { }

        public override void OnFlow(FlowContext flowContext) { }

        public override void OnLinkClosed(ListenerLink link, Error error)
        {
            Console.WriteLine("A client has disconnected.");
            _dataProducer.DataProduced -= OnDataProducerDataProduced;
            base.OnLinkClosed(link, error);
        }
    }

    private sealed class DataProducer
    {
        public event EventHandler<object>? DataProduced;

        public void Produce(int start, int count)
        {
            for (int i = 0; i < count; i++)
                DataProduced?.Invoke(this, start + i);
        }
    }

    private sealed class Client(Address address)
    {
        public const string DataPipeAddress = "DataPipe";
        private const string DataPipeReceiverAddress = "data-pipe-receiver";

        private readonly Address _address = address;
        private Connection? _connection;

        public void Run()
        {
            _connection = new Connection(_address);
            var session = new Session(_connection);
            var receiver = new ReceiverLink(session, DataPipeReceiverAddress, DataPipeAddress);
            receiver.SetCredit(int.MaxValue);
            while (true)
            {
                Message message = receiver.Receive();
                if (message == null)
                {
                    Console.WriteLine("Client exiting.");
                    break;
                }

                receiver.Accept(message);
                Console.WriteLine("Received " + message.Body);
            }
        }

        public void Close()
            => _connection?.Close();
    }
}
