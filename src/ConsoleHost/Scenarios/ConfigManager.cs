using Microsoft.Extensions.Configuration;

namespace SoftWell.RtFix.ConsoleHost.Scenarios;

public class ConfigManager
{
    private readonly IConfiguration _configuration;

    public ConfigManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public OperationOptions GetOperationSettings(string operationName)
    {
        var commonSettings = _configuration.GetSection("CommonSettings").Get<OperationOptions>()
            ?? throw new InvalidOperationException("Common settings are not configured.");

        var operationSection = _configuration.GetSection(operationName);

        var settings = new OperationOptions
        {
            PartyId = operationSection.GetValue<string>("PartyId") ?? commonSettings.PartyId,
            PartyIds = operationSection.GetSection("PartyIds").Get<string[]>() ?? commonSettings.PartyIds,
            QuotationSecurityId = operationSection.GetValue<string>("QuotationSecurityId") ?? commonSettings.QuotationSecurityId,
            CurveCode = operationSection.GetValue<string>("CurveCode") ?? commonSettings.CurveCode,
            SecurityId = operationSection.GetValue<string>("SecurityId") ?? commonSettings.SecurityId,
            SecurityIds = operationSection.GetSection("SecurityIds").Get<string[]>() ?? commonSettings.SecurityIds,
            Interval = operationSection.GetValue<TimeSpan?>("Interval") ?? commonSettings.Interval
        };

        return settings;
    }
}