using System.ComponentModel.DataAnnotations;

namespace ClimateMonitor.Services.Models;

public class DeviceReadingRequest
{
    [Required]
    public string FirmwareVersion { get; set; } = string.Empty;

    [Required]
    public decimal Temperature { get; set; }

    [Required]
    public decimal Humidity { get; set; }
}
