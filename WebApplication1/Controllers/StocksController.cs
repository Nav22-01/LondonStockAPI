using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LondonStockAPI.Model;
using LondonStockAPI.Repository.Interface;
using Microsoft.AspNetCore.SignalR;
using LondonStockAPI.Hubs;

namespace LondonStockAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly ITradeRepository _tradeRepository;
        private readonly ILogger<StocksController> _logger;
        private readonly IHubContext<TradeHub> _hubContext;
        public StocksController(ITradeRepository tradeRepository, ILogger<StocksController> logger, IHubContext<TradeHub> hubContext)
        {
            _tradeRepository = tradeRepository;
            _logger = logger;
            _hubContext = hubContext;
        }
        [HttpPost("trades")]
        [ProducesResponseType(201)] 
        [ProducesResponseType(400)] 
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateTrade([FromBody] Trade trade)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid trade data received.");
                return BadRequest(ModelState); 
            }

            try
            {
                await _tradeRepository.AddTradeAsync(trade);
                await _hubContext.Clients.All.SendAsync("ReceiveTrade", trade);
                return CreatedAtAction(nameof(GetStock), new { tickerSymbol = trade.TickerSymbol }, trade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade.");
                return StatusCode(500, "Internal Server Error"); 
            }
        }

        [HttpGet("price/{tickerSymbol}")]
        [ProducesResponseType(200, Type = typeof(StockPrice))] // OK
        [ProducesResponseType(404)] // Not Found
        [ProducesResponseType(500)]
        public async Task<ActionResult<StockPrice>> GetStock(string tickerSymbol)
        {
            try
            {
                var averagePrice = await _tradeRepository.GetAveragePriceAsync(tickerSymbol);
                if (averagePrice == null)
                {
                    _logger.LogWarning($"Stock {tickerSymbol} not found.");
                    return NotFound();
                }
                return new StockPrice { TickerSymbol = tickerSymbol, AveragePrice = averagePrice.Value };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving stock price for {tickerSymbol}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<StockPrice>))] // OK
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StockPrice>>> GetStocks([FromQuery] string tickers = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tickers))
                {
                    var stockPrices = await _tradeRepository.GetAveragePricesAsync();
                    return Ok(stockPrices);
                }

                var tickerList = tickers.Split(',').ToList();
                var filteredStockPrices = await _tradeRepository.GetAveragePricesAsync(tickerList);
                return Ok(filteredStockPrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock prices");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
