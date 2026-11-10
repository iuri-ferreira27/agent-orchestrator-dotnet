using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Tests.Simulation;

public class FakeModelClientCacheTests
{
    private const string Sistema = "Instruções fixas bem compridas para valer a pena guardar em cache.";

    [Fact]
    public async Task SendAsync_PrimeiraChamadaComCache_GravaNoCache()
    {
        var client = new FakeModelClient();

        var r = await client.SendAsync(new ModelRequest("m", "tarefa", Sistema, CacheSystemPrompt: true));

        Assert.True(r.Usage.CacheWriteTokens > 0);
        Assert.Equal(0, r.Usage.CacheReadTokens);
    }

    [Fact]
    public async Task SendAsync_SegundaChamadaMesmoSystem_LeDoCache()
    {
        var client = new FakeModelClient();
        var pedido = new ModelRequest("m", "tarefa", Sistema, CacheSystemPrompt: true);

        var primeira = await client.SendAsync(pedido);
        var segunda = await client.SendAsync(pedido);

        Assert.True(segunda.Usage.CacheReadTokens > 0);
        Assert.Equal(0, segunda.Usage.CacheWriteTokens);
        Assert.Equal(primeira.Usage.CacheWriteTokens, segunda.Usage.CacheReadTokens);
        // O prompt do usuário nunca vai para o cache.
        Assert.Equal(TokenEstimator.Estimate("tarefa"), segunda.Usage.InputTokens);
    }

    [Fact]
    public async Task SendAsync_OutroModeloComMesmoSystem_GravaDeNovo()
    {
        var client = new FakeModelClient();

        await client.SendAsync(new ModelRequest("a", "x", Sistema, CacheSystemPrompt: true));
        var r = await client.SendAsync(new ModelRequest("b", "x", Sistema, CacheSystemPrompt: true));

        Assert.True(r.Usage.CacheWriteTokens > 0);
    }

    [Fact]
    public async Task SendAsync_SemCache_TudoViraInputTokens()
    {
        var client = new FakeModelClient();
        var pedido = new ModelRequest("m", "tarefa", Sistema, CacheSystemPrompt: false);

        await client.SendAsync(pedido);
        var r = await client.SendAsync(pedido);

        Assert.Equal(0, r.Usage.CacheReadTokens);
        Assert.Equal(0, r.Usage.CacheWriteTokens);
        Assert.Equal(TokenEstimator.Estimate(Sistema) + TokenEstimator.Estimate("tarefa"), r.Usage.InputTokens);
    }

    [Fact]
    public async Task SendAsync_ComResponder_UsaOTextoDele()
    {
        var client = new FakeModelClient(responder: req => $"eco:{req.Prompt}");

        var r = await client.SendAsync(new ModelRequest("m", "oi"));

        Assert.Equal("eco:oi", r.Text);
    }
}
