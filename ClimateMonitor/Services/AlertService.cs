using ClimateMonitor.Services.Models;

namespace ClimateMonitor.Services;

public class AlertService
{
    private static readonly HashSet<Func<DeviceReadingRequest, Alert?>> SensorValidators = new()
    {
        deviceReading =>
            deviceReading.Humidity is < 0 or > 100 
            ? new Alert(AlertType.HumiditySensorOutOfRange, "Humidity sensor is out of range.")
            : default,

        deviceReading => 
            deviceReading.Humidity is < -10 or > 50 
            ? new Alert(AlertType.TemperatureSensorOutOfRange, "Temperature sensor is out of range.")
            : default,
    };

    public IEnumerable<Alert> GetAlerts(DeviceReadingRequest deviceReadingRequest)
    {
        return SensorValidators
            .Select(validator => validator(deviceReadingRequest))
            .OfType<Alert>();
    }
}
