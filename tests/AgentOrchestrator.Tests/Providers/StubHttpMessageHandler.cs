using System.Net;
using System.Text;

namespace AgentOrchestrator.Tests.Providers;

/// <summary>
/// Handler falso de HTTP: captura o que o cliente enviou e devolve uma resposta pronta.
/// Assim os testes nunca tocam a rede de verdade.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _responseBody;

    public StubHttpMessageHandler(HttpStatusCode status, string responseBody)
    {
        _status = status;
        _responseBody = responseBody;
    }

    public HttpMethod? Method { get; private set; }
    public Uri? Url { get; private set; }
    public HttpRequestMessage? Request { get; private set; }
    public string? Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        Method = request.Method;
        Url = request.RequestUri;
        // O corpo precisa ser lido aqui, antes de a requisição ser descartada pelo cliente.
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
        };
    }
}
