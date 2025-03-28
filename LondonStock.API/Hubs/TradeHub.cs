using LondonStockAPI.Model;
using Microsoft.AspNetCore.SignalR;

namespace LondonStockAPI.Hubs
{
    public class TradeHub : Hub
    {
        private readonly ILogger<TradeHub> _logger;
        public TradeHub(ILogger<TradeHub> logger)
        {
            _logger = logger;
        }
        public async Task NotifyTrade(Trade trade)
        {
            _logger.LogInformation("Sending trade notification for ticker: {TickerSymbol}", trade.TickerSymbol);
            await Clients.All.SendAsync("ReceiveTrade", trade);
        }
    }
}
