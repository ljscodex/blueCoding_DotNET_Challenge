using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using ClimateMonitor.Api.Controllers;
using ClimateMonitor.Services.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Sdk;

namespace ClimateMonitor.Test;

public class EvaluateReadingTests
{
    private readonly WebApplicationFactory<ReadingsController> _application;

    public EvaluateReadingTests()
    {
        _application = new WebApplicationFactory<ReadingsController>();
    }

    [Theory]
    [InlineData("secret-ABC-123-XYZ-001", HttpStatusCode.OK)]
    [InlineData("secret-ABC-123-XYZ-002", HttpStatusCode.OK)]
    [InlineData("secret-ABC-123-XYZ-003", HttpStatusCode.OK)]
    [InlineData("secret-ABC-123-XYZ-001-invalid", HttpStatusCode.Unauthorized)]
    [InlineData("secret-ABC-123-XYZ-002-invalid", HttpStatusCode.Unauthorized)]
    [InlineData("secret-ABC-123-XYZ-003-invalid", HttpStatusCode.Unauthorized)]
    public async Task CorrectStatusCodeBasedOnSecretValidity(string secret, HttpStatusCode exceptedStatusCode)
    {
        var httpClient = _application.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await httpClient.PostAsJsonAsync(
            $"/readings/evaluate", 
            new DeviceReadingRequest()
            {
                FirmwareVersion = "1.0.0",
                Humidity = 50,
                Temperature = 20
            });

        Assert.Equal(exceptedStatusCode, response.StatusCode);
    }
    
    [Theory]
    [InlineData("secret-ABC-123-XYZ-001", "1.0.0")]
    [InlineData("secret-ABC-123-XYZ-001", "1.0.1")]
    [InlineData("secret-ABC-123-XYZ-001", "0.0.1-BETA")]
    [InlineData("secret-ABC-123-XYZ-001", "10.3.10")]
    public async Task SuccessWhenValidFirmware(string secret, string firmware)
    {
        var httpClient = _application.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await httpClient.PostAsJsonAsync(
            $"/readings/evaluate",
            new DeviceReadingRequest()
            {
                FirmwareVersion = firmware,
                Humidity = 50,
                Temperature = 20
            });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("secret-ABC-123-XYZ-001", "1.0.0.1")]
    [InlineData("secret-ABC-123-XYZ-001", "something")]
    [InlineData("secret-ABC-123-XYZ-001", "1")]
    [InlineData("secret-ABC-123-XYZ-001", "1.0")]
    [InlineData("secret-ABC-123-XYZ-001", "a.b.c")]
    [InlineData("secret-ABC-123-XYZ-001", "..")]
    public async Task ReturnErrorWhenInvalidFirmware(string secret, string firmware)
    {
        var httpClient = _application.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-device-shared-secret", secret);

        var response = await httpClient.PostAsJsonAsync(
            $"/readings/evaluate",
            new DeviceReadingRequest()
            {
                FirmwareVersion = firmware,
                Humidity = default,
                Temperature = default
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        try
        {
            var expectedError = "The firmware value does not match semantic versioning format.";

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            
            if (problemDetails?.Errors.ContainsKey("FirmwareVersion") == true)
            {
                Assert.Equal(expectedError, problemDetails?.Errors["FirmwareVersion"].FirstOrDefault());
            }
            else
            {
                throw new XunitException($"Expected a valid response with same structure as `ValidationProblemDetails` DTO for the FirmwareVersion field, but received something else.");
            }
        }
        catch (JsonException ex)
        {
            throw new XunitException($"Expected a valid JSON response with same structure as `ValidationProblemDetails` DTO, but received something else. {ex.Message}");
        }
    }
    
    [Theory]
    [InlineData(60, 15)]
    [InlineData(70, 25)]
    [InlineData(90, 35)]
    public async Task SuccessWhenAllValuesAreCorrect(int humidityValue, int temperatureValue)
    {
        var httpClient = _application.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-device-shared-secret", "secret-ABC-123-XYZ-001");

        var response = await httpClient.PostAsJsonAsync(
            $"/readings/evaluate",
            new DeviceReadingRequest()
            {
                FirmwareVersion = "1.0.0",
                Humidity = humidityValue,
                Temperature = temperatureValue
            });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        try {
            var alerts = await response.Content.ReadFromJsonAsync<IList<Alert>>();
            Assert.NotNull(alerts);
            Assert.Empty(alerts);
        }
        catch (JsonException ex)
        {
            throw new XunitException($"Expected a valid JSON response with same structure as array of `Alert` DTO, but received something else. {ex.Message}");
        }
    }

    [Theory]
    [InlineData(50, false, -50, true)]
    [InlineData(50, false, 200, true)]
    [InlineData(-1, true, 20, false)]
    [InlineData(200, true, 20, false)]
    [InlineData(-1, true, -50, true)]
    public async Task ReturnAlertWhenValuesAreOutOfRange(int humidityValue, bool expectHumidityAlert, int temperatureValue, bool expectTemperatureAlert)
    {
        var httpClient = _application.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-device-shared-secret", "secret-ABC-123-XYZ-001");

        var response = await httpClient.PostAsJsonAsync(
            $"/readings/evaluate",
            new DeviceReadingRequest()
            {
                FirmwareVersion = "1.0.0",
                Humidity = humidityValue,
                Temperature = temperatureValue
            });


        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        try
        {
            var alerts = await response.Content.ReadFromJsonAsync<IList<Alert>>();

            Assert.NotNull(alerts);

            if (expectHumidityAlert)
            {
                Assert.Contains(alerts, alert => alert.AlertType == AlertType.HumiditySensorOutOfRange);
            }

            if (expectTemperatureAlert)
            {
                Assert.Contains(alerts, alert => alert.AlertType == AlertType.TemperatureSensorOutOfRange); 
            }
        }
        catch (JsonException ex)
        {
            throw new XunitException($"Expected a valid JSON response with same structure as array of `Alert` DTO, but received something else. {ex.Message}");
        }
    }
    
}