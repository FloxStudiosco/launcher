using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public sealed class HttpObjectSource : IObjectSource
{
    private readonly HttpClient _http;
    private readonly Uri _baseUri;

    public HttpObjectSource(HttpClient http, Uri baseUri)
    {
        _http = http;
        _baseUri = baseUri.AbsoluteUri.EndsWith("/") ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
    }

    public async Task<Stream> OpenAsync(string relativePath, CancellationToken cancellation)
    {
        var address = new Uri(_baseUri, relativePath);
        HttpResponseMessage response = await _http
            .GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellation)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            int status = (int)response.StatusCode;
            response.Dispose();
            throw new HttpRequestException($"GET {address} returned {status}.");
        }
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }
}
