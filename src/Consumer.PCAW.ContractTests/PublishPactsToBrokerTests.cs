using Consumer.PCAW.ContractTests.Constants;
using Consumer.PCAW.ContractTests.Publisher;
using Xunit;
using Xunit.Abstractions;

namespace Consumer.PCAW.ContractTests;

/// <summary>
/// Publishes generated Pact files to the Pact Broker.
///
/// This test class is gated by the PACT_BROKER_BASE_URL environment variable.
/// When the variable is not set (e.g. local dev without a broker), the tests
/// are skipped automatically. In CI the pipeline sets the variable so that
/// publishing runs after the consumer tests pass.
///
/// Required environment variables:
///   PACT_BROKER_BASE_URL  – Pact Broker URL (required, gate)
///   PACT_BROKER_TOKEN     – Bearer token  (or use USERNAME + PASSWORD)
///   PACT_BROKER_USERNAME  – Basic-auth username
///   PACT_BROKER_PASSWORD  – Basic-auth password
///   CONSUMER_VERSION      – Defaults to GITHUB_SHA, then to a local timestamp
///   GITHUB_REF_NAME       – Git branch (auto-set in GitHub Actions)
/// </summary>
public class PublishPactsToBrokerTests
{
    private readonly ITestOutputHelper _output;

    public PublishPactsToBrokerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Publish all consumer pacts to the Pact Broker")]
    public async Task PublishPactsToBroker()
    {
        // --- Gate: skip when no broker is configured ---
        var brokerUrl = Environment.GetEnvironmentVariable(PactConstants.Broker.UrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(brokerUrl))
        {
            _output.WriteLine(
                $"SKIPPED: {PactConstants.Broker.UrlEnvironmentVariable} is not set. " +
                "Set the environment variable to enable pact publishing.");
            return;
        }

        // --- Create publisher from env vars ---
        var publisher = PactBrokerPublisher.CreateFromEnvironment();
        Assert.NotNull(publisher);

        // --- Resolve pact directory ---
        var pactDir = PactConstants.PactOutput.GetPactDirectory();
        _output.WriteLine($"Pact directory: {pactDir}");

        var pactFiles = Directory.GetFiles(pactDir, "*.json");
        Assert.True(pactFiles.Length > 0,
            $"No pact JSON files found in {pactDir}. Run consumer tests first to generate pact files.");

        foreach (var f in pactFiles)
            _output.WriteLine($"  Found pact: {Path.GetFileName(f)}");

        // --- Publish ---
        var results = await publisher.PublishAllPactsAsync(pactDir);

        foreach (var result in results)
        {
            _output.WriteLine($"[{(result.Success ? "OK" : "FAIL")}] {result.Message}");
        }

        // Assert all succeeded
        Assert.All(results, r => Assert.True(r.Success, r.Message));

        _output.WriteLine($"\nSuccessfully published {results.Count} pact(s) to {brokerUrl}.");
    }
}
