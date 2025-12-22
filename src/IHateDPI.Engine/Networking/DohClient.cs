using System.Net;
using System.Net.Http.Headers;
using IHateDPI.Engine.Abstractions;

namespace IHateDPI.Engine.Networking;

/// <summary>
/// A client implementation that performs secure and encrypted DNS queries using the DNS over HTTPS (DoH) protocol.
/// </summary>
public sealed class DohClient : IDnsResolver, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _requestPath;

    private static readonly MediaTypeHeaderValue DnsContentType = new("application/dns-message");

    /// <summary>
    /// Initializes a new instance of the <see cref="DohClient"/> class using the specified provider URL.
    /// </summary>
    /// <param name="baseAddress">The full address of the DoH provider (e.g., <c>https://1.1.1.1/dns-query</c>).</param>
    public DohClient(string baseAddress)
    {
        var uri = new Uri(baseAddress);

        // Optimize HTTP/2 and connection pooling settings for high performance.
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            EnableMultipleHttp2Connections = true,
            MaxConnectionsPerServer = 20
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority)),
            Timeout = TimeSpan.FromSeconds(1.5), // Short timeout for real-time DNS queries
            DefaultRequestVersion = HttpVersion.Version20
        };

        _requestPath = uri.PathAndQuery;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("IHateDPI/1.0");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-message"));
    }

    /// <summary>
    /// Sends a raw DNS query (byte array) to the DoH server and writes the response directly to the destination buffer.
    /// </summary>
    /// <param name="dnsQuery">The read-only memory containing the raw DNS query data.</param>
    /// <param name="destBuffer">The target memory buffer to receive the server's response.</param>
    /// <param name="token">A token to cancel the operation.</param>
    /// <returns>
    /// The total number of bytes written to the buffer, or 0 if the request failed.
    /// </returns>
    public async Task<int> ResolveToBufferAsync(ReadOnlyMemory<byte> dnsQuery, Memory<byte> destBuffer, CancellationToken token = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _requestPath);
            using var content = new ReadOnlyMemoryContent(dnsQuery);
            content.Headers.ContentType = DnsContentType;
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode) return 0;

            using var stream = await response.Content.ReadAsStreamAsync(token);

            int totalBytesRead = 0;
            int bytesRead;
            // Read from the stream until the destination buffer is full or the stream ends.
            while ((bytesRead = await stream.ReadAsync(destBuffer[totalBytesRead..], token)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead >= destBuffer.Length)
                    break;
            }

            return totalBytesRead;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Disposes the <see cref="HttpClient"/> and releases underlying socket resources.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}