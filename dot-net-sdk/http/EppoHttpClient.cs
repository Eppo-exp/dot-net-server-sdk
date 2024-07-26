using System.Net;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace eppo_sdk.http;


public class VersionedResource<RType>
{
    public readonly RType? Resource;
    public readonly bool IsModified;
    public readonly string? ETag;

    public VersionedResource(RType? resource, string? eTag = null, bool isModified = true)
    {
        Resource = resource;
        IsModified = isModified;
        ETag = eTag;
    }
}

public class EppoHttpClient
{
    private readonly Dictionary<string, string> _defaultParams = new();
    private readonly string _baseUrl;
    private readonly int _requestTimeOutMillis = 3000;

    public EppoHttpClient(string apikey, string sdkName, string sdkVersion, string baseUrl)
    {
        this._defaultParams.Add("apiKey", apikey);
        this._defaultParams.Add("sdkName", sdkName);
        this._defaultParams.Add("sdkVersion", sdkVersion);
        this._baseUrl = baseUrl;
    }

    public EppoHttpClient(
        string apikey,
        string sdkName,
        string sdkVersion,
        string baseUrl,
        int requestTimeOutMillis
    )
    {
        this._defaultParams.Add("apiKey", apikey);
        this._defaultParams.Add("sdkName", sdkName);
        this._defaultParams.Add("sdkVersion", sdkVersion);
        this._baseUrl = baseUrl;
        this._requestTimeOutMillis = requestTimeOutMillis;
    }

    // If any additional query params are needed.
    public void AddDefaultParam(string key, string value)
    {
        this._defaultParams.Add(key, value);
    }

    public VersionedResource<RType> Get<RType>(string url, string? lastVersion = null)
    {
        return this.Get<RType>(url, new Dictionary<string, string>(), new Dictionary<string, string>(), lastVersion);
    }

    public VersionedResource<RType> Get<RType>(
        string url,
        Dictionary<string, string> parameters,
        Dictionary<string, string> headers,
        string? lastVersion = null
    )
    {
        _defaultParams.ToList().ForEach(x => parameters.Add(x.Key, x.Value));

        var request = new RestRequest
        {
            Timeout = _requestTimeOutMillis
        };

        parameters.ToList().ForEach(x => request.AddParameter(new QueryParameter(x.Key, x.Value)));

        if (lastVersion != null)
        {
            headers.Add("IF-NONE-MATCH", lastVersion);
        }
        request.AddHeaders(headers);

        var client = new RestClient(_baseUrl + url, configureSerialization: s => s.UseNewtonsoftJson());
        var restResponse = client.Execute<RType>(request);

        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Invalid Eppo API Key");
        }

        string? eTag;
        try
        {
            eTag = restResponse.Headers?.ToList()?.Find(x => x.Name == "ETag").Value?.ToString() ?? null;
        }
        catch (Exception)
        {
            eTag = null;
        }

        return new VersionedResource<RType>(restResponse.Data, eTag, isModified: restResponse.StatusCode != HttpStatusCode.NotModified);
    }
}
