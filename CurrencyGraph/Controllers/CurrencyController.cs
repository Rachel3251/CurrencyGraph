namespace CurrencyGraph.Controllers
{
    using CurrencyGraph.Data;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using CurrencyGraph.Services;

    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrencyService _currencyService;

        public CurrencyController(ApplicationDbContext dbContext, CurrencyService currencyService)
        {
            _dbContext = dbContext;
            _currencyService = currencyService;
        }

        //Retrieving data from the API and inserting it into a database
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAndStoreData()
        {
            RemoveOldCurrencyData();
            await _currencyService.FetchAndStoreCurrencyGraphAsync();
            return Ok("Currency data fetched and stored successfully.");
        }

        // Retrieving data based on user input
        [HttpGet("currency-graph")]
        public IActionResult GetCurrencyGraphs([FromQuery] string period)
        {
            var validPeriods = new[] { "week", "month", "halfyear", "year" };

            DateTime fromDate = GetStartDate(period.ToLower());

            if (period.ToLower() == "week" || period.ToLower() == "month")
            {
                var graphs = _dbContext.CurrencyGraphs
                    .Where(r => r.LastUpdated >= fromDate)
                    .GroupBy(r => new { r.CurrencyCode, r.CurrencyName })
                    .Select(g => new
                    {
                        Currency = g.Key.CurrencyName,
                        CurrencyCode = g.Key.CurrencyCode,
                        Values = g.OrderBy(x => x.LastUpdated)
                            .Select(x => new
                            {
                                x.LastUpdated,
                                x.ExchangeRate
                            })
                            .ToList()
                    })
                    .ToList();
                return Ok(graphs);
            }
            else
            {
                var graphs = _dbContext.CurrencyGraphs
                    .Where(r => r.LastUpdated >= fromDate)
                    .GroupBy(r => new { r.CurrencyCode, r.CurrencyName })
                    .Select(g => new
                    {
                        Currency = g.Key.CurrencyName,
                        CurrencyCode = g.Key.CurrencyCode,
                        Values = g.OrderBy(x => x.LastUpdated)
                            .Select(x => new
                            {
                                x.LastUpdated,
                                x.ExchangeRate
                            })
                            .Where(v => v.LastUpdated.Day == DateTime.Now.Day)
                    })
                    .ToList();
                return Ok(graphs);
            }

        }

        // Helper method to remove old data beyond a year
        private void RemoveOldCurrencyData()
        {
            var oneYearAgo = DateTime.Now.AddYears(-1);
            var oldData = _dbContext.CurrencyGraphs
                .Where(r => r.LastUpdated < oneYearAgo)
                .ToList();

            if (oldData.Any())
            {
                _dbContext.CurrencyGraphs.RemoveRange(oldData);
                _dbContext.SaveChanges();
            }
        }

        private DateTime GetStartDate(string period) => period.ToLower()
        switch
        {
            "week" => DateTime.Now.AddDays(-7),
            "month" => DateTime.Now.AddMonths(-1),
            "halfyear" => DateTime.Now.AddMonths(-6),
            "year" => DateTime.Now.AddYears(-1),
            _ => DateTime.Now 
        };
    }
}
