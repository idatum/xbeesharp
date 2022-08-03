using xbee2mqtt;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.UseUtcTimestamp = false;
            options.TimestampFormat = "HH:mm:ss ";
        });
    })
    .Build();

await host.RunAsync();
