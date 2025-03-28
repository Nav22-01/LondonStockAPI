using System.ComponentModel.DataAnnotations;

namespace LondonStockAPI.Model
{
    public class Trade
    {
        [Key]
        public int TradeId { get; set; }

        [Required]
        [StringLength(10)]
        public required string TickerSymbol { get; set; } 

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or a positive value.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be zero or a positive value.")]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public required string BrokerId { get; set; } 

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
