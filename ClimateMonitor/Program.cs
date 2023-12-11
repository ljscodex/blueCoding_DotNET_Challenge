using ClimateMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTransient<AlertService>();
builder.Services.AddTransient<DeviceSecretValidatorService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
