using System.Text.Json.Serialization;

namespace Consumer.PCAW.ContractTests.Models;

/// <summary>
/// Request model for UpdateDeviceInformation API endpoint.
/// Consumer-side model that mirrors the provider's expected request format.
/// </summary>
public class UpdateDeviceInformationRequest
{
    /// <summary>
    /// The serial number of the device to update.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// The new part number to assign to the device.
    /// </summary>
   // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   // public string? NewPartNumber { get; set; }

    /// <summary>
    /// The username of the person/system performing the update.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The organization associated with the device update.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Org { get; set; }
}
