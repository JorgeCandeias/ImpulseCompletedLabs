// long lived orleans client
using Orleans;

var orleans = new ClientBuilder()
    .UseLocalhostClustering()
    .Build();

// connect to the cluster
await orleans.Connect(async error =>
{
    Console.WriteLine(error.ToString());
    await Task.Delay(TimeSpan.FromSeconds(1));
    return true;
});

// ideally keep the client alive and reuse it until app shutdown
Console.WriteLine("Connected!");

// at app shutdown gracefully disconnect from the cluster
await orleans.Close();