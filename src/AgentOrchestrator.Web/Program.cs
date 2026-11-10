using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Orchestration;
using AgentOrchestrator.Core.Providers;
using AgentOrchestrator.Core.Simulation;
using AgentOrchestrator.Web.Components;
using AgentOrchestrator.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var config = builder.Configuration;

// Configuração do orquestrador: valida na inicialização para falhar cedo com mensagem clara.
var orchestratorOptions = config.GetSection("AI:Orchestrator").Get<OrchestratorOptions>() ?? new OrchestratorOptions();
orchestratorOptions.Validate();
builder.Services.AddSingleton(orchestratorOptions);

// Tabela de preços lida do appsettings (ModelPrice é um record posicional, então passa por PriceEntry).
var priceEntries = config.GetSection("AI:Prices").Get<List<PriceEntry>>() ?? new List<PriceEntry>();
var priceTable = new PriceTable(priceEntries.Select(p => new ModelPrice(
    p.Model, p.InputPerMillion, p.OutputPerMillion, p.CacheReadPerMillion, p.CacheWritePerMillion)));
builder.Services.AddSingleton(priceTable);
builder.Services.AddSingleton(new PriceNote(config["AI:PricesNote"] ?? ""));

// Escolha do provedor de IA. A chave da Anthropic vem de user-secrets (AI:Anthropic:ApiKey), nunca do appsettings.
var provider = config["AI:Provider"] ?? "Simulated";
switch (provider.Trim().ToLowerInvariant())
{
    case "simulated":
        var latencyMs = config.GetValue("AI:SimulatedLatencyMs", 300);
        builder.Services.AddSingleton<IModelClient>(
            new FakeModelClient(TimeSpan.FromMilliseconds(latencyMs), SimulatedResponder.Respond));
        builder.Services.AddSingleton(new ProviderInfo("Simulado", true));
        break;

    case "anthropic":
        builder.Services.AddSingleton(config.GetSection("AI:Anthropic").Get<AnthropicOptions>() ?? new AnthropicOptions());
        builder.Services.AddHttpClient<IModelClient, AnthropicModelClient>(c => c.Timeout = TimeSpan.FromMinutes(2));
        builder.Services.AddSingleton(new ProviderInfo("Anthropic", false));
        break;

    case "ollama":
        builder.Services.AddSingleton(config.GetSection("AI:Ollama").Get<OllamaOptions>() ?? new OllamaOptions());
        builder.Services.AddHttpClient<IModelClient, OllamaModelClient>(c => c.Timeout = TimeSpan.FromMinutes(2));
        builder.Services.AddSingleton(new ProviderInfo("Ollama", false));
        break;

    default:
        throw new InvalidOperationException(
            $"Provedor de IA desconhecido: '{provider}'. Use Simulated, Anthropic ou Ollama em AI:Provider.");
}

builder.Services.AddSingleton<IPlanner, Planner>();
builder.Services.AddSingleton<IModelRouter, ModelRouter>();
builder.Services.AddSingleton<Orchestrator>();
builder.Services.AddSingleton<RunHistory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
