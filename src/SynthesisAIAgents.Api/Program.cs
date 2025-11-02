using Amazon.Comprehend;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Services;
using SynthesisAIAgents.Api.Tools;


var builder = WebApplication.CreateBuilder(args);

// config
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// DI registrations
builder.Services.AddSingleton<IRunRepository, InMemoryRunRepository>();
builder.Services.AddSingleton<IOrchestrator, Orchestrator>();

// Tools - register tool implementations (pluggable)
builder.Services.AddSingleton<ITool, HttpFetcherTool>();
builder.Services.AddSingleton<ITool, AwsSentimentAnalysisTool>();
builder.Services.AddSingleton<ITool, AwsKeyPhrasesTool>();
builder.Services.AddSingleton<IToolFactory, ToolFactory>();

// Agents - register via DI so new agent types can be added
builder.Services.AddTransient<IAgent, FetchHttpAgent>();
builder.Services.AddTransient<IAgent, AwsSentimentAnalysisAgent>();
builder.Services.AddTransient<IAgent, AwsKeyphrasesAgent>();

// configure concurrency & retry settings
builder.Services.Configure<OrchestratorOptions>(builder.Configuration.GetSection("Orchestrator"));

// configure integration clients
builder.Services.AddSingleton<IAmazonComprehend>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var region = cfg["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    return new AmazonComprehendClient(Amazon.RegionEndpoint.GetBySystemName(region));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseAuthorization();
app.Run();
