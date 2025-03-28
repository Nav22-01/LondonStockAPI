using LondonStockAPI.Model;
using System.Diagnostics;

namespace LondonStockAPI.Repository.Interface
{
    public interface ITradeRepository
    {
        Task AddTradeAsync(Trade trade);
        Task<decimal?> GetAveragePriceAsync(string tickerSymbol);
        Task<List<StockPrice>> GetAveragePricesAsync();
        Task<List<StockPrice>> GetAveragePricesAsync(List<string> tickerSymbols);
    }
}
