using ReportJobRunner;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

var host = builder.Build();
host.Run();
