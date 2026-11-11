using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    // reuse one client across invocations
    private static readonly HttpClient client = new HttpClient();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogLine($"Received: {input}");

        var json = JObject.Parse(input.ToString());

        // GitHub sends a ping event with no issue, so skip it instead of crashing
        string? issueUrl = (string?)json["issue"]?["html_url"];
        if (issueUrl == null)
        {
            return "no issue in payload, ignored";
        }

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}