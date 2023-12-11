using Microsoft.AspNetCore.Mvc;
using ClimateMonitor.Services;
using ClimateMonitor.Services.Models;
using System.Security.Permissions;

namespace ClimateMonitor.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly DeviceSecretValidatorService _secretValidator;
    private readonly AlertService _alertService;

    public ReadingsController(
        DeviceSecretValidatorService secretValidator, 
        AlertService alertService)
    {
        _secretValidator = secretValidator;
        _alertService = alertService;
    }

    /// <summary>
    /// Evaluate a sensor readings from a device and return possible alerts.
    /// </summary>
    /// <remarks>
    /// The endpoint receives sensor readings (temperature, humidity) values
    /// as well as some extra metadata (firmwareVersion), evaluates the values
    /// and generate the possible alerts the values can raise.
    /// 
    /// There are old device out there, and if they get a firmwareVersion 
    /// format error they will request a firmware update to another service.
    /// </remarks>
    /// <param name="deviceSecret">A unique identifier on the device included in the header(x-device-shared-secret).</param>
    /// <param name="deviceReadingRequest">Sensor information and extra metadata from device.</param>
    /// /// 
    [HttpGet("hello")]
    public ActionResult<String> hello()
    {

        Console.WriteLine( "Hello");
        return "everything good";
    }

    [HttpPost("evaluate")]
    public ActionResult<IEnumerable<Alert>> EvaluateReading(
        [FromBody] DeviceReadingRequest deviceReadingRequest)
    {

          string deviceSecret = Request.Headers["x-device-shared-secret"];

        if (!_secretValidator.ValidateDeviceSecret(deviceSecret))
        {
            return Problem(
                detail: "Device secret is not within the valid range.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!_alertService.FirmwareValidation(deviceReadingRequest.FirmwareVersion))
        {
            // i will emulate a modelstate error called FirmwareVersion
            ModelState.AddModelError("FirmwareVersion", "The firmware value does not match semantic versioning format.");
            // as the unittest is waiting for a ValidationProblemDetails, we will use it
            var details = new ValidationProblemDetails(ModelState);
            return new ObjectResult(details)
            {
                ContentTypes = {"application/problem+json"},
                StatusCode = 400,
            };

        }    



        return Ok(_alertService.GetAlerts(deviceReadingRequest));
    }

}
