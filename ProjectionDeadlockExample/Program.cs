using System;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Topshelf;

namespace ProjectionDeadlockExample
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<DeadLockService>(s =>
                {
                    s.ConstructUsing(name => new DeadLockService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
            });
        }
    }

    public class DeadLockService
    {
        private readonly IEventStoreConnection _connection;

        private EventStoreStreamCatchUpSubscription _subscription;

        public DeadLockService()
        {
            var cs = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500"; // "GossipSeeds=10.0.1.20:3113,10.0.1.20:4112,10.0.1.20:5113; HeartBeatTimeout=500"
            _connection = EventStoreConnection.Create(cs);
        }

        public void Start()
        {
            _connection.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            Thread.Sleep(3000); // let's give it some time to ensure that connected handler will not be invoked at start

            Console.WriteLine("Starting subscription");

            _subscription = _connection.SubscribeToStreamFrom("$ce-abc", null,
                 new CatchUpSubscriptionSettings(1000, 100, true, true),
                 EventAppeared, null, null, new UserCredentials("admin", "changeit"));

            Console.WriteLine("Subscription started");

            Console.WriteLine("Binding to connected handler");

            _connection.Connected += ConnectionOnConnected;

            Console.WriteLine("Bound to connected handler");
        }

        private void ConnectionOnConnected(object sender, ClientConnectionEventArgs clientConnectionEventArgs)
        {
            // ThreadPool.QueueUserWorkItem(state =>
            //  {
            Console.WriteLine("Before reading stream in connected handler");

            clientConnectionEventArgs.Connection.ReadStreamEventsBackwardAsync("$ce-abc", -1, 1, true,
                new UserCredentials("admin", "changeit")).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine("After reading stream in connected handler");
            // });
        }

        private void EventAppeared(EventStoreCatchUpSubscription arg1, ResolvedEvent arg2)
        {
            _connection.AppendToStreamAsync("checkpointAbc", ExpectedVersion.Any, new UserCredentials("admin", "changeit"),
                new EventData(Guid.NewGuid(), "checkpointAbc", true, Encoding.ASCII.GetBytes(arg2.OriginalEventNumber.ToString()),
                    null)).Wait();
        }

        public void Stop()
        {
            _subscription.Stop();
            _connection.Dispose();
        }
    }
}
