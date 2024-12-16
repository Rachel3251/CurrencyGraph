using System.Xml;
using System.Xml.Linq;
using CurrencyGraph.Data;
using CurrencyGraph.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CurrencyGraph.Services
{
    public class CurrencyService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string[] _supportedCurrencies = { "USD", "GBP", "SEK", "CHF" };
        private readonly Dictionary<string, string> _types = new() { { "USD", "דולר" }, { "GBP", "לירה" }, { "SEK", "כתר" }, { "CHF", "פרנק" } }; 
        private readonly string _currencyApiUrl;

        public CurrencyService(ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            string currentDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            string lastYearDate = DateTime.Now.Date.AddYears(-1).ToString("yyyy-MM-dd");
            _currencyApiUrl = $"https://edge.boi.gov.il/FusionEdgeServer/sdmx/v2/data/dataflow/BOI.STATISTICS/EXR/1.0?startperiod={lastYearDate}&endperiod={currentDate}";
        }

        /// <summary>
        /// Get Data From API and add to DB the data that not added yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task FetchAndStoreCurrencyGraphAsync()
        {
             var lastUpdateDate = await _dbContext.CurrencyGraphs
            .OrderByDescending(c => c.LastUpdated)
            .Select(c => c.LastUpdated.Date)
            .FirstOrDefaultAsync();
            var today = DateTime.Today;
            if (lastUpdateDate >= today)
            {
                return; 
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync(_currencyApiUrl);
                var exchangeRates = ParseCurrencyData(response);
                foreach (var exchangeRate in exchangeRates)
                {
                    foreach (var rate in exchangeRate)
                    {
                        var existingRecord = await _dbContext.CurrencyGraphs
                            .FirstOrDefaultAsync(c => c.CurrencyCode == rate.CurrencyCode && c.LastUpdated.Date == rate.LastUpdated.Date);
                        if (existingRecord == null)
                        {
                            _dbContext.CurrencyGraphs.Add(rate);
                        }
                        else
                        {
                            existingRecord.ExchangeRate = rate.ExchangeRate;
                            existingRecord.LastUpdated = rate.LastUpdated;
                        }
                    }
                }
                await _dbContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error fetching and storing currency data.", ex);
            }

        }

        /// <summary>
        /// parse xml data to currency graph objects
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private List<List<Currency_graph>> ParseCurrencyData(string data)
        {
            List<List<Currency_graph>> allData = new List<List<Currency_graph>>();
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Received empty or null JSON data.");
            try
            {
                var doc = XDocument.Parse(data);
                var seriesElements = doc.Descendants(XName.Get("Series")).ToList();
                foreach (var seriesElement in seriesElements)
                {
                    string baseCurrency = (string)seriesElement.Attribute("BASE_CURRENCY");
                    if (baseCurrency == "USD")
                    {
                        string seriesTitle = (string)seriesElement.Attribute("SERIES_CODE");
                        if (seriesTitle != "RER_USD_ILS")
                            continue;
                    }
                    var observations = seriesElement.Descendants(XName.Get("Obs"))
                        .Select(obs => new Currency_graph()
                        {
                            CurrencyCode = baseCurrency,
                            LastUpdated = (DateTime)obs.Attribute("TIME_PERIOD"),
                            ExchangeRate = (double)obs.Attribute("OBS_VALUE")
                        })
                        .ToList();
                    ExchangeRatesResponse response = new ExchangeRatesResponse(observations);
                    if (!response.exchangeRates.Any())
                    {
                        Console.WriteLine("No exchange rates found in the response.");
                        return new List<List<Currency_graph>>();
                    }
                    var result = response.exchangeRates
                        .Where(rate => _supportedCurrencies.Contains(rate.CurrencyCode))
                        .ToList();
                    result.ForEach(c => c.CurrencyName = _types[c.CurrencyCode]);
                    allData.Add(result);
                }
                return allData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing the JSON data: {data}");
                throw new InvalidOperationException("Error parsing JSON data.", ex);
            }
        }
    }
}
