using System.Net.Http.Headers;
using System.Text;
using Consumer.PCAW.ContractTests.Constants;

namespace Consumer.PCAW.ContractTests.Publisher;

/// <summary>
/// Publishes generated Pact files to the Pact Broker using its REST API.
/// This removes the dependency on the external pact-broker Ruby CLI tool.
///
/// Pact Broker API reference:
///   PUT /pacts/provider/{provider}/consumer/{consumer}/version/{version}
///
/// Authentication is resolved from environment variables:
///   - Bearer token:  PACT_BROKER_TOKEN
///   - Basic auth:    PACT_BROKER_USERNAME + PACT_BROKER_PASSWORD
/// </summary>
public class PactBrokerPublisher
{
    private readonly HttpClient _httpClient;
    private readonly string _brokerBaseUrl;
    private readonly string _consumerVersion;
    private readonly string? _branch;
    private readonly string? _tag;

    /// <summary>
    /// Creates a new publisher configured from the supplied parameters.
    /// </summary>
    /// <param name="brokerBaseUrl">Pact Broker base URL (e.g. https://your-broker.pactflow.io)</param>
    /// <param name="consumerVersion">Unique version string – typically the git SHA.</param>
    /// <param name="branch">Optional git branch name to associate with the pact version.</param>
    /// <param name="tag">Optional tag to apply to the consumer version.</param>
    /// <param name="bearerToken">Bearer token for Pactflow / token-based auth.</param>
    /// <param name="username">Username for basic auth (ignored when bearerToken is set).</param>
    /// <param name="password">Password for basic auth.</param>
    public PactBrokerPublisher(
        string brokerBaseUrl,
        string consumerVersion,
        string? branch = null,
        string? tag = null,
        string? bearerToken = null,
        string? username = null,
        string? password = null)
    {
        _brokerBaseUrl = brokerBaseUrl.TrimEnd('/');
        _consumerVersion = consumerVersion;
        _branch = branch;
        _tag = tag;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Configure authentication
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        }
        else if (!string.IsNullOrWhiteSpace(username))
        {
            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", encoded);
        }
    }

    /// <summary>
    /// Creates a publisher using configuration from environment variables.
    /// Returns null if PACT_BROKER_BASE_URL is not set.
    /// </summary>
    public static PactBrokerPublisher? CreateFromEnvironment()
    {
        var brokerUrl = Environment.GetEnvironmentVariable(PactConstants.Broker.UrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(brokerUrl))
            return null;

        var version = Environment.GetEnvironmentVariable("CONSUMER_VERSION")
                      ?? Environment.GetEnvironmentVariable("GITHUB_SHA")
                      ?? $"local-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME")
                     ?? Environment.GetEnvironmentVariable("GIT_BRANCH");

        var tag = Environment.GetEnvironmentVariable("PACT_TAG");

        var token = Environment.GetEnvironmentVariable(PactConstants.Broker.TokenEnvironmentVariable);
        var username = Environment.GetEnvironmentVariable(PactConstants.Broker.UsernameEnvironmentVariable);
        var password = Environment.GetEnvironmentVariable(PactConstants.Broker.PasswordEnvironmentVariable);

        return new PactBrokerPublisher(brokerUrl, version, branch, tag, token, username, password);
    }

    /// <summary>
    /// Publishes a single pact JSON file to the Pact Broker.
    /// </summary>
    /// <param name="consumerName">Consumer participant name.</param>
    /// <param name="providerName">Provider participant name.</param>
    /// <param name="pactFilePath">Absolute path to the pact JSON file.</param>
    /// <returns>True if the publish was successful (HTTP 200/201).</returns>
    public async Task<PublishResult> PublishPactAsync(string consumerName, string providerName, string pactFilePath)
    {
        if (!File.Exists(pactFilePath))
            return new PublishResult(false, $"Pact file not found: {pactFilePath}");

        var pactContent = await File.ReadAllTextAsync(pactFilePath);

        // 1. Publish the pact contract
        var publishUrl = $"{_brokerBaseUrl}/pacts/provider/{Uri.EscapeDataString(providerName)}" +
                         $"/consumer/{Uri.EscapeDataString(consumerName)}" +
                         $"/version/{Uri.EscapeDataString(_consumerVersion)}";

        var content = new StringContent(pactContent, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(publishUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return new PublishResult(false,
                $"Failed to publish pact. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        // 2. Tag the consumer version with the branch name (if provided)
        if (!string.IsNullOrWhiteSpace(_branch))
        {
            await TagVersionAsync(consumerName, _branch);
        }

        if (!string.IsNullOrWhiteSpace(_tag))
        {
            await TagVersionAsync(consumerName, _tag);
        }

        return new PublishResult(true,
            $"Published pact to {publishUrl} (consumer version: {_consumerVersion}, branch: {_branch ?? "n/a"})");
    }

    /// <summary>
    /// Publishes all pact JSON files found in the given directory.
    /// </summary>
    public async Task<IReadOnlyList<PublishResult>> PublishAllPactsAsync(string pactDirectory)
    {
        var results = new List<PublishResult>();

        if (!Directory.Exists(pactDirectory))
        {
            results.Add(new PublishResult(false, $"Pact directory not found: {pactDirectory}"));
            return results;
        }

        var pactFiles = Directory.GetFiles(pactDirectory, "*.json");
        if (pactFiles.Length == 0)
        {
            results.Add(new PublishResult(false, $"No pact JSON files found in: {pactDirectory}"));
            return results;
        }

        foreach (var pactFile in pactFiles)
        {
            // Extract consumer and provider names from the pact JSON
            var (consumer, provider) = await ExtractParticipantsFromPactFileAsync(pactFile);
            if (consumer == null || provider == null)
            {
                results.Add(new PublishResult(false, $"Could not extract consumer/provider from: {pactFile}"));
                continue;
            }

            var result = await PublishPactAsync(consumer, provider, pactFile);
            results.Add(result);
        }

        return results;
    }

    private async Task TagVersionAsync(string consumerName, string tag)
    {
        var tagUrl = $"{_brokerBaseUrl}/pacticipants/{Uri.EscapeDataString(consumerName)}" +
                     $"/versions/{Uri.EscapeDataString(_consumerVersion)}" +
                     $"/tags/{Uri.EscapeDataString(tag)}";

        var tagContent = new StringContent("{}", Encoding.UTF8, "application/json");
        await _httpClient.PutAsync(tagUrl, tagContent);
    }

    private static async Task<(string? Consumer, string? Provider)> ExtractParticipantsFromPactFileAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var doc = System.Text.Json.JsonDocument.Parse(json);

            string? consumer = null;
            string? provider = null;

            if (doc.RootElement.TryGetProperty("consumer", out var c) &&
                c.TryGetProperty("name", out var cn))
                consumer = cn.GetString();

            if (doc.RootElement.TryGetProperty("provider", out var p) &&
                p.TryGetProperty("name", out var pn))
                provider = pn.GetString();

            return (consumer, provider);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Result of a pact publish operation.
    /// </summary>
    public record PublishResult(bool Success, string Message);
}
