using System.Net;
using System.Text.Json;
using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Providers;

namespace AgentOrchestrator.Tests.Providers;

public class OllamaModelClientTests
{
    private const string RespostaOk = """
        {
          "model": "llama-teste",
          "message": { "role": "assistant", "content": "resposta local" },
          "prompt_eval_count": 12,
          "eval_count": 34
        }
        """;

    private static (OllamaModelClient Client, StubHttpMessageHandler Stub) Criar(
        HttpStatusCode status = HttpStatusCode.OK, string corpo = RespostaOk)
    {
        var stub = new StubHttpMessageHandler(status, corpo);
        var client = new OllamaModelClient(new HttpClient(stub), new OllamaOptions());
        return (client, stub);
    }

    [Fact]
    public async Task SendAsync_ComSystem_EnviaDuasMensagens()
    {
        var (client, stub) = Criar();

        await client.SendAsync(new ModelRequest("llama-teste", "pergunta", SystemPrompt: "regras", MaxOutputTokens: 128));

        Assert.Equal(HttpMethod.Post, stub.Method);
        Assert.Equal("http://localhost:11434/api/chat", stub.Url!.ToString());

        using var doc = JsonDocument.Parse(stub.Body!);
        var raiz = doc.RootElement;
        Assert.Equal("llama-teste", raiz.GetProperty("model").GetString());
        Assert.False(raiz.GetProperty("stream").GetBoolean());
        Assert.Equal(128, raiz.GetProperty("options").GetProperty("num_predict").GetInt32());

        var mensagens = raiz.GetProperty("messages");
        Assert.Equal(2, mensagens.GetArrayLength());
        Assert.Equal("system", mensagens[0].GetProperty("role").GetString());
        Assert.Equal("regras", mensagens[0].GetProperty("content").GetString());
        Assert.Equal("user", mensagens[1].GetProperty("role").GetString());
        Assert.Equal("pergunta", mensagens[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_SemSystem_EnviaApenasMensagemDoUsuario()
    {
        var (client, stub) = Criar();

        await client.SendAsync(new ModelRequest("m", "pergunta", CacheSystemPrompt: true));

        using var doc = JsonDocument.Parse(stub.Body!);
        var mensagens = doc.RootElement.GetProperty("messages");
        Assert.Equal(1, mensagens.GetArrayLength());
        Assert.Equal("user", mensagens[0].GetProperty("role").GetString());
    }

    [Fact]
    public async Task SendAsync_LeTextoETokens()
    {
        var (client, _) = Criar();

        var resposta = await client.SendAsync(new ModelRequest("m", "p"));

        Assert.Equal("llama-teste", resposta.Model);
        Assert.Equal("resposta local", resposta.Text);
        Assert.Equal(12, resposta.Usage.InputTokens);
        Assert.Equal(34, resposta.Usage.OutputTokens);
        Assert.Equal(0, resposta.Usage.CacheReadTokens);
        Assert.Equal(0, resposta.Usage.CacheWriteTokens);
    }

    [Fact]
    public async Task SendAsync_ContagemAusente_ViraZero()
    {
        var (client, _) = Criar(corpo: """{ "message": { "role": "assistant", "content": "ok" } }""");

        var resposta = await client.SendAsync(new ModelRequest("m", "p"));

        Assert.Equal(0, resposta.Usage.InputTokens);
        Assert.Equal(0, resposta.Usage.OutputTokens);
    }

    [Fact]
    public async Task SendAsync_Erro500_LancaModelClientException()
    {
        var (client, _) = Criar(HttpStatusCode.InternalServerError, "falha interna");

        var ex = await Assert.ThrowsAsync<ModelClientException>(
            () => client.SendAsync(new ModelRequest("m", "p")));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal("Ollama", ex.Provider);
        Assert.Contains("falha interna", ex.Message);
    }
}
