using System.Net;
using eppo_sdk.constants;
using NLog;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace eppo_sdk.http;


/// <summary>
/// Wraps the structured response from the API server with version information. 
/// </summary>
/// <typeparam name="TResource"></typeparam>
public class VersionedResourceResponse<TResource>
{
    public readonly TResource? Resource;
    public readonly bool IsModified;
    public readonly string? VersionIdentifier;

    public VersionedResourceResponse(TResource? resource,
                                     string? versionIdentifier = null,
                                     bool isModified = true)
    {
        Resource = resource;
        IsModified = isModified;
        VersionIdentifier = versionIdentifier;
    }
}

/// <summary>
/// The `EppoHttpClient` wraps the network call and response parsing, returning structured data to the caller.
/// 
/// Resources returned are _versioned_, using mechanisms in the underlying transport layer to identify distinct
/// versions of the resource and identify when it has not changed on the server. Specifically, the client makes
/// use of the `ETAG` response header and the `IF-NONE-MATCH` request header to version the resource and
/// determine if it has changed. When the resource has not changed, the network layer drops the response body
/// from transport, saving bandwidth.
/// 
/// Outwardly, the resource is wrapped in a `VersionedResourceResponse` instance carrying the version identifer
/// and whether it changed. If callers do not pass their "current" version identifier, the resource is always
/// loaded and `VersionedResourceResponse.IsModified` is `true`. 
/// </summary>
public class EppoHttpClient
{
    private readonly Dictionary<string, string> _defaultParams = new();
    private readonly string _baseUrl;
    private readonly int _requestTimeoutMillis;

    public EppoHttpClient(string apikey,
                          string sdkName,
                          string sdkVersion,
                          string baseUrl,
                          int requestTimeOutMillis = Constants.REQUEST_TIMEOUT_MILLIS)
    {
        _defaultParams.Add("apiKey", apikey);
        _defaultParams.Add("sdkName", sdkName);
        _defaultParams.Add("sdkVersion", sdkVersion);

        _baseUrl = baseUrl;
        _requestTimeoutMillis = requestTimeOutMillis;
    }

    // If any additional query params are needed.
    public void AddDefaultParam(string key, string value)
    {
        _defaultParams.Add(key, value);
    }

    /// <summary>
    /// Gets the resource at the given `url`
    /// </summary>
    /// <typeparam name="TResource"></typeparam>
    /// <param name="url"></param>
    /// <param name="lastVersion"></param> If provided, attempts to optimize network usage and response processing.
    /// <returns></returns>
    public virtual VersionedResourceResponse<TResource> Get<TResource>(string url,
                                                                       string? lastVersion = null)
    {
        return Get<TResource>(url, new Dictionary<string, string>(), new Dictionary<string, string>(), lastVersion);
    }

    /// <summary>
    /// Gets the resource at the given `url`
    /// </summary>
    /// <typeparam name="TResource"></typeparam>
    /// <param name="url"></param>
    /// <param name="parameters"></param>
    /// <param name="headers"></param>
    /// <param name="lastVersion"></param> If provided, attempts to optimize network usage and response processing.
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public VersionedResourceResponse<TResource> Get<TResource>(string url,
                                                               Dictionary<string, string> parameters,
                                                               Dictionary<string, string> headers,
                                                               string? lastVersion = null)
    {
        // Prepare request.
        var request = new RestRequest
        {
            Timeout = _requestTimeoutMillis
        };

        // Add query parameters.        
        _defaultParams.ToList().ForEach(x => parameters.Add(x.Key, x.Value));
        parameters.ToList().ForEach(x => request.AddParameter(new QueryParameter(x.Key, x.Value)));

        // `lastVersion` is the version identifier from the last time the caller requested this resource.
        // Use the IF-NONE-MATCH header to tell the API server it only needs to return a response body
        // if none of the provided versions match its latest.
        // If the `lastVersion` matches, the server will respond with `304 Not Modified` instead of `200 OK`
        if (lastVersion != null)
        {
            headers.Add("IF-NONE-MATCH", lastVersion);
        }
        request.AddHeaders(headers);

        var client = new RestClient(_baseUrl + url, configureSerialization: s => s.UseNewtonsoftJson());
        var restResponse = client.Execute<TResource>(request);

        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Invalid Eppo API Key");
        }

        // HTTP uses the `ETag` header to identify the version of the resource (or entity) returned in the response.
        string? eTag;
        try
        {
            eTag = restResponse.Headers?.ToList()?.Find(x => x.Name == "ETag").Value?.ToString() ?? null;
        }
        catch (Exception)
        {
            eTag = null;
        }

        return new VersionedResourceResponse<TResource>(restResponse.Data, eTag, isModified: restResponse.StatusCode != HttpStatusCode.NotModified);
    }
}
