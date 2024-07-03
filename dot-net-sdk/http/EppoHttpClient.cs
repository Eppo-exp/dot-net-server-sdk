using System.Net;
using eppo_sdk.dto;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace eppo_sdk.http;

public class EppoHttpClient
{
    private readonly Dictionary<string, string> _defaultParams = new();
    private readonly string _baseUrl;
    private readonly int _requestTimeOutMillis = 3000;

    public EppoHttpClient(string apikey,
                          IReadOnlyDictionary<string, string> appDetails,
                          string baseUrl,
                          int requestTimeOutMillis = 3000)
    {
        this._defaultParams.Add("apiKey", apikey);
        foreach (KeyValuePair<string, string> datum in appDetails)
        {
            this._defaultParams.Add(datum.Key, datum.Value);
        }
        this._baseUrl = baseUrl;
        this._requestTimeOutMillis = requestTimeOutMillis;
    }

    // If any additional query params are needed.
    public void AddDefaultParam(string key, string value)
    {
        this._defaultParams.Add(key, value);
    }

    public RType? Get<RType>(string url)
    {
        return this.Get<RType>(url, new Dictionary<string, string>(), new Dictionary<string, string>());
    }

    public RType? Get<RType>(
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

        var client = new RestClient(_baseUrl + url, configureSerialization: s => s.UseNewtonsoftJson());
        var restResponse = client.Execute<RType>(request);

        if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Invalid Eppo API Key");
        }

        return restResponse.Data;
    }
}