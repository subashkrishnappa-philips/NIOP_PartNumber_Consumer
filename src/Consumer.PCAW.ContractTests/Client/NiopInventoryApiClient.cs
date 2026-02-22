using System.Net.Http.Json;
using System.Text.Json;
using Consumer.PCAW.ContractTests.Models;

namespace Consumer.PCAW.ContractTests.Client;

/// <summary>
/// HTTP client wrapper for consuming the NIOP Beat Inventory API.
/// Used in consumer contract tests to define the expected interaction.
/// </summary>
public class NiopInventoryApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Explicit JSON serializer options to ensure consistent PascalCase property naming
    /// across all .NET versions. This matches the provider's expected contract format.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null, // Preserve PascalCase property names
        PropertyNameCaseInsensitive = true
    };

    public NiopInventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Calls the UpdateDeviceInformation endpoint on the NIOP provider API.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateDeviceInformationAsync(UpdateDeviceInformationRequest request)
    {
        return await _httpClient.PostAsJsonAsync("/api/UpdateDeviceInformation", request, JsonOptions);
    }
}
