using System.Net;
using System.Text.Json;
using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Providers;

namespace AgentOrchestrator.Tests.Providers;

public class AnthropicModelClientTests
{
    private const string ChaveFalsa = "chave-de-teste";

    private const string RespostaOk = """
        {
          "model": "modelo-resposta",
          "content": [
            { "type": "text", "text": "Olá, " },
            { "type": "tool_use", "id": "x" },
            { "type": "text", "text": "mundo" }
          ],
          "usage": {
            "input_tokens": 10,
            "output_tokens": 20,
            "cache_read_input_tokens": 30,
            "cache_creation_input_tokens": 40
          }
        }
        """;

    private static (AnthropicModelClient Client, StubHttpMessageHandler Stub) Criar(
        HttpStatusCode status = HttpStatusCode.OK, string corpo = RespostaOk)
    {
        var stub = new StubHttpMessageHandler(status, corpo);
        var client = new AnthropicModelClient(new HttpClient(stub), new AnthropicOptions { ApiKey = ChaveFalsa });
        return (client, stub);
    }

    [Fact]
    public async Task SendAsync_EnviaUrlHeadersECorpoCorretos()
    {
        var (client, stub) = Criar();

        await client.SendAsync(new ModelRequest("modelo-x", "faça isso", SystemPrompt: "seja breve", MaxOutputTokens: 256));

        Assert.Equal(HttpMethod.Post, stub.Method);
        Assert.Equal("https://api.anthropic.com/v1/messages", stub.Url!.ToString());
        Assert.Equal(ChaveFalsa, stub.Request!.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", stub.Request.Headers.GetValues("anthropic-version").Single());

        using var doc = JsonDocument.Parse(stub.Body!);
        var raiz = doc.RootElement;
        Assert.Equal("modelo-x", raiz.GetProperty("model").GetString());
        Assert.Equal(256, raiz.GetProperty("max_tokens").GetInt32());
        // Sem cache: system é string simples.
        Assert.Equal("seja breve", raiz.GetProperty("system").GetString());
        var msg = raiz.GetProperty("messages")[0];
        Assert.Equal("user", msg.GetProperty("role").GetString());
        Assert.Equal("faça isso", msg.GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_ComCache_EnviaSystemComCacheControl()
    {
        var (client, stub) = Criar();

        await client.SendAsync(new ModelRequest("m", "p", SystemPrompt: "regras fixas", CacheSystemPrompt: true));

        using var doc = JsonDocument.Parse(stub.Body!);
        var bloco = doc.RootElement.GetProperty("system")[0];
        Assert.Equal("text", bloco.GetProperty("type").GetString());
        Assert.Equal("regras fixas", bloco.GetProperty("text").GetString());
        Assert.Equal("ephemeral", bloco.GetProperty("cache_control").GetProperty("type").GetString());
    }

    [Fact]
    public async Task SendAsync_SemSystem_OmiteCampoSystem()
    {
        var (client, stub) = Criar();

        await client.SendAsync(new ModelRequest("m", "p", CacheSystemPrompt: true));

        using var doc = JsonDocument.Parse(stub.Body!);
        Assert.False(doc.RootElement.TryGetProperty("system", out _));
    }

    [Fact]
    public async Task SendAsync_LeTextoEOsQuatroTiposDeToken()
    {
        var (client, _) = Criar();

        var resposta = await client.SendAsync(new ModelRequest("modelo-x", "p"));

        Assert.Equal("modelo-resposta", resposta.Model);
        Assert.Equal("Olá, mundo", resposta.Text); // só os blocos de texto, concatenados
        Assert.Equal(10, resposta.Usage.InputTokens);
        Assert.Equal(20, resposta.Usage.OutputTokens);
        Assert.Equal(30, resposta.Usage.CacheReadTokens);
        Assert.Equal(40, resposta.Usage.CacheWriteTokens);
    }

    [Fact]
    public async Task SendAsync_CamposAusentesOuNulos_ViramZeroEUsamModeloDoPedido()
    {
        var (client, _) = Criar(corpo: """
            { "content": [ { "type": "text", "text": "ok" } ],
              "usage": { "input_tokens": 5, "cache_read_input_tokens": null } }
            """);

        var resposta = await client.SendAsync(new ModelRequest("modelo-pedido", "p"));

        Assert.Equal("modelo-pedido", resposta.Model);
        Assert.Equal(5, resposta.Usage.InputTokens);
        Assert.Equal(0, resposta.Usage.OutputTokens);
        Assert.Equal(0, resposta.Usage.CacheReadTokens);
        Assert.Equal(0, resposta.Usage.CacheWriteTokens);
    }

    [Fact]
    public async Task SendAsync_Erro401_LancaExcecaoSemVazarAChave()
    {
        var (client, _) = Criar(HttpStatusCode.Unauthorized, """{"error":{"message":"invalid x-api-key"}}""");

        var ex = await Assert.ThrowsAsync<ModelClientException>(
            () => client.SendAsync(new ModelRequest("m", "p")));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("Anthropic", ex.Provider);
        Assert.DoesNotContain(ChaveFalsa, ex.Message);
        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task SendAsync_CorpoDeErroGrande_ELimitadoA300Caracteres()
    {
        var (client, _) = Criar(HttpStatusCode.InternalServerError, new string('x', 5000));

        var ex = await Assert.ThrowsAsync<ModelClientException>(
            () => client.SendAsync(new ModelRequest("m", "p")));

        Assert.True(ex.Message.Length < 500);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ApiKeyVazia_LancaComOrientacao(string chave)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new AnthropicModelClient(new HttpClient(), new AnthropicOptions { ApiKey = chave }));

        Assert.Contains("user-secrets", ex.Message);
    }
}
