using ContractApi.Data;
using ContractApi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace ContractApi.Services;

public class ExchangeRateService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _http;
    private readonly string _apiId;

    public ExchangeRateService(ApplicationDbContext context, IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _context = context;
        _http = clientFactory.CreateClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ExchangeRateApp/1.0");
        _apiId = configuration["ExchangeRateApi:ApiKey"] ?? throw new ArgumentException("API key not found in configuration.");
    }

    public async Task<string> LoadRates(DateTime startDate, DateTime? endDate = null)
    {
        var today = DateTime.Today;
        var minDate = new DateTime(2002, 5, 15);

        if (startDate < minDate || (endDate.HasValue && endDate.Value < minDate))
            return JsonMessage("error", $"You cannot search for a course list before {minDate:dd.MM.yyyy}.");

        if (startDate > today || (endDate.HasValue && endDate.Value > today))
            return JsonMessage("error", $"You cannot search the exchange rate list for dates after {today:dd.MM.yyyy}.");

        var currencies = await _context.Currencies
            .Where(c => !c.Inactive)
            .Select(c => c.CurrencyCode)
            .ToListAsync();

        var dates = endDate.HasValue
            ? Enumerable.Range(0, (endDate.Value - startDate).Days + 1)
                        .Select(offset => startDate.AddDays(offset))
            : new List<DateTime> { startDate };

        int successCount = 0;
        var inserted = new List<string>();
        var updated = new List<string>();
        var debugLines = new List<string>();

        foreach (var date in dates)
        {
            var dateStr = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            var url = $"https://api.kursna-lista.info/{_apiId}/kl_na_dan/{dateStr}/json";

            var response = await _http.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return JsonMessage("error", $"Error {response.StatusCode} for {dateStr}", content);

            JObject json;
            try
            {
                json = JObject.Parse(content);
            }
            catch (Exception ex)
            {
                return JsonMessage("error", $"Invalid JSON response for {dateStr}", ex.Message);
            }

            var status = json["status"]?.ToString();
            var code = json["code"]?.ToString();
            var message = json["message"]?.ToString();

            if (status != "ok")
                return JsonMessage("error", $"API error for {dateStr}: {message ?? "Unknown error (code: " + code + ")"}");

            var rates = json["result"];
            if (rates == null)
                return JsonMessage("error", $"Answer for {dateStr} does not contain the expected format.", json.ToString());

            foreach (var codeValue in currencies)
            {
                var rateEntry = rates.Children<JProperty>()
                    .FirstOrDefault(p => string.Equals(p.Name, codeValue, StringComparison.OrdinalIgnoreCase))?
                    .Value;

                var rateToken = rateEntry?["sre"];
                if (rateToken == null) continue;

                if (decimal.TryParse(rateToken.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                {
                    var existing = await _context.ExchangeRates.FindAsync(codeValue, "RSD", date);

                    if (existing == null)
                    {
                        _context.ExchangeRates.Add(new ExchangeRate
                        {
                            CurrencyFrom = codeValue,
                            CurrencyTo = "RSD",
                            ExchangeRateDate = date,
                            Rate = rate,
                            Ts = DateTime.Now
                        });
                        inserted.Add(codeValue);
                        debugLines.Add($"{codeValue}: Inserted | API={rate}");
                        successCount++;
                    }
                    else
                    {
                        decimal dbRateRounded = decimal.Round(existing.Rate, 4);
                        decimal apiRateRounded = decimal.Round(rate, 4);

                        if (dbRateRounded != apiRateRounded)
                        {
                            existing.Rate = rate;
                            existing.Ts = DateTime.Now;
                            _context.ExchangeRates.Update(existing);
                            updated.Add(codeValue);
                            debugLines.Add($"{codeValue}: Updated | DB={dbRateRounded}, API={apiRateRounded}");
                            successCount++;
                        }
                        else
                        {
                            debugLines.Add($"{codeValue}: Skipped (no change) | DB={dbRateRounded}, API={apiRateRounded}");
                        }
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        if (successCount == 0)
        {
            return JsonMessage("info", "The exchange rate list has already been entered for the requested date. There is no new data to add..",
                null, inserted, updated, debugLines);
        }

        return JsonMessage("success", $"Courses successfully entered for {successCount} currency.",
            null, inserted, updated, debugLines);
    }

    public async Task<string> CheckStatusForToday()
    {
        var today = DateTime.Today;

        bool anyExists = await _context.ExchangeRates
            .AnyAsync(x => x.ExchangeRateDate == today);

        if (anyExists)
        {
            return JsonMessage("complete", "Courses for today's date are already registered.", today.ToString("yyyy-MM-dd"));
        }
        else
        {
            return JsonMessage("missing", "Courses for today's date have not yet been registered.", today.ToString("yyyy-MM-dd"));
        }
    }


    private string JsonMessage(string status, string message, string? detail = null,
           List<string>? inserted = null, List<string>? updated = null, List<string>? debug = null)
    {
        var json = new JObject
        {
            ["status"] = status,
            ["message"] = message
        };

        if (!string.IsNullOrWhiteSpace(detail))
            json["detail"] = detail;

        if (inserted != null && inserted.Any())
            json["inserted"] = JArray.FromObject(inserted);

        if (updated != null && updated.Any())
            json["updated"] = JArray.FromObject(updated);

        if (debug != null && debug.Any())
            json["debug"] = JArray.FromObject(debug);

        return json.ToString();
    }

}
