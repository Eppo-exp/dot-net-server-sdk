using System.Net;
using dot_net_eppo.dto;
using RestSharp;

namespace dot_net_sdk.http;

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
    public void addDefaultParam(string key, string value)
    {
        this._defaultParams.Add(key, value);
    }

    public ExperimentConfigurationResponse? Get(String url) {
        return this.Get(url, new Dictionary<string, string>(), new Dictionary<string, string>());
    }

    public ExperimentConfigurationResponse? Get(
        string url,
        Dictionary<string, string> parameters,
        Dictionary<string, string> headers
    )
    {
        _defaultParams.ToList().ForEach(x => parameters.Add(x.Key, x.Value));

        var request = new RestRequest
        {
            Timeout = _requestTimeOutMillis
        };
        parameters.ToList().ForEach(x => request.AddParameter(new QueryParameter(x.Key, x.Value)));
        request.AddHeaders(headers);
        var client = new RestClient(_baseUrl + url);
        var restResponse = client.Execute<ExperimentConfigurationResponse>(request);
        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Invalid Eppo API Key");
        }
        return restResponse.Data;
    }
}