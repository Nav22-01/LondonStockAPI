namespace LondonStockAPI.Model
{
    public class StockPrice
    {
        public string TickerSymbol { get; set; } = string.Empty;
        public decimal AveragePrice { get; set; }
    }

}
