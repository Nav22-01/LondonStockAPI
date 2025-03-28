using Microsoft.EntityFrameworkCore;
using LondonStockAPI.Model;
using LondonStockAPI.Repository.Interface;

namespace LondonStockAPI.Repository
{
    public class TradeRepository: ITradeRepository
    {
        private readonly StockExchangeContext _context;
        private readonly ILogger<TradeRepository> _logger;

        public TradeRepository(StockExchangeContext context, ILogger<TradeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddTradeAsync(Trade trade)
        {
            try
            {
                _context.Trades.Add(trade);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Trade added: {trade.TradeId}, Ticker: {trade.TickerSymbol}, Broker: {trade.BrokerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding trade to the database.");
                throw;
            }
        }

        public async Task<decimal?> GetAveragePriceAsync(string tickerSymbol)
        {
            try
            {
                return await _context.Trades
                    .Where(t => t.TickerSymbol == tickerSymbol)
                    .AverageAsync(t => t.Price);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting average price for {tickerSymbol}");
                throw;
            }
        }

        public async Task<List<StockPrice>> GetAveragePricesAsync()
        {
            try
            {
                return await _context.Trades
                    .GroupBy(t => t.TickerSymbol)
                    .Select(g => new StockPrice
                    {
                        TickerSymbol = g.Key,
                        AveragePrice = g.Average(t => t.Price)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average prices for all stocks");
                throw;
            }
        }

        public async Task<List<StockPrice>> GetAveragePricesAsync(List<string> tickerSymbols)
        {
            try
            {
                return await _context.Trades
                    .Where(t => tickerSymbols.Contains(t.TickerSymbol))
                    .GroupBy(t => t.TickerSymbol)
                    .Select(g => new StockPrice
                    {
                        TickerSymbol = g.Key,
                        AveragePrice = g.Average(t => t.Price)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting average prices for tickers: {string.Join(",", tickerSymbols)}");
                throw;
            }
        }
    }
}
