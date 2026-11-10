using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Tests;

public class FakeModelClientTests
{
    [Fact]
    public async Task SendAsync_DevolveTextoETokens()
    {
        var client = new FakeModelClient();

        var response = await client.SendAsync(new ModelRequest("modelo-barato", "Revise este script Lua"));

        Assert.Equal("modelo-barato", response.Model);
        Assert.Contains("Revise este script Lua", response.Text);
        Assert.True(response.Usage.InputTokens > 0);
        Assert.True(response.Usage.OutputTokens > 0);
    }

    [Fact]
    public async Task SendAsync_RespeitaLimiteDeSaida()
    {
        var client = new FakeModelClient();
        var promptGrande = new string('a', 10_000); // cerca de 2.500 tokens

        var response = await client.SendAsync(new ModelRequest("m", promptGrande, MaxOutputTokens: 100));

        Assert.Equal(100, response.Usage.OutputTokens);
    }

    [Fact]
    public async Task SendAsync_ContaTokensDoSystemPrompt()
    {
        var client = new FakeModelClient();

        var semSystem = await client.SendAsync(new ModelRequest("m", "tarefa"));
        var comSystem = await client.SendAsync(new ModelRequest("m", "tarefa", SystemPrompt: "Você é um revisor de código."));

        Assert.True(comSystem.Usage.InputTokens > semSystem.Usage.InputTokens);
    }

    [Fact]
    public async Task SendAsync_RegistraChamadas()
    {
        var client = new FakeModelClient();

        await client.SendAsync(new ModelRequest("a", "1"));
        await client.SendAsync(new ModelRequest("b", "2"));

        Assert.Equal(["a", "b"], client.Calls.Select(c => c.Model));
    }

    [Fact]
    public async Task SendAsync_CanceladoAntes_LancaExcecao()
    {
        var client = new FakeModelClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.SendAsync(new ModelRequest("m", "x"), cts.Token));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("abcd", 1)]
    [InlineData("abcde", 2)]
    public void TokenEstimator_ArredondaParaCima(string? text, long expected)
    {
        Assert.Equal(expected, TokenEstimator.Estimate(text));
    }
}
