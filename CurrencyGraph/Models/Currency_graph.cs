using System;
using System.Text.Json.Serialization;

namespace CurrencyGraph.Models
{
    public class Currency_graph
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public double ExchangeRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ExchangeRatesResponse
    {
        public ExchangeRatesResponse(List<Currency_graph> exchangeRates)
        {
            this.exchangeRates = exchangeRates;
        }

        public List<Currency_graph> exchangeRates { get; set; }
    }
}
