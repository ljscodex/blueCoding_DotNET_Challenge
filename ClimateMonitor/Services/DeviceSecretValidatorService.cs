namespace ClimateMonitor.Services;

public class DeviceSecretValidatorService
{
    private static readonly ISet<string> ValidSecrets = new HashSet<string>
    {
        "secret-ABC-123-XYZ-001",
        "secret-ABC-123-XYZ-002",
        "secret-ABC-123-XYZ-003"
    };

    public bool ValidateDeviceSecret(string deviceSecret) 
        => ValidSecrets.Contains(deviceSecret);
}
