using LondonStockAPI.Controllers;
using LondonStockAPI.Hubs;
using LondonStockAPI.Model;
using LondonStockAPI.Repository;
using LondonStockAPI.Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LondonStockApi.Tests
{
    public class StocksControllerTests
    {
        private readonly DbContextOptions<StockExchangeContext> _options;
        private readonly Mock<ITradeRepository> _mockTradeRepository;
        private readonly Mock<IHubContext<TradeHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<StocksController>> _mockLogger;
        private readonly StocksController _controller;
        public StocksControllerTests()
        {
            _options = new DbContextOptionsBuilder<StockExchangeContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _mockTradeRepository = new Mock<ITradeRepository>();
            _mockHubContext = new Mock<IHubContext<TradeHub>>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockLogger = new Mock<ILogger<StocksController>>();

            // Mock Clients.All to return a ClientProxy
            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            _controller = new StocksController(_mockTradeRepository.Object, _mockLogger.Object, _mockHubContext.Object);
        }

        // Helper method to create a populated context
        private StockExchangeContext GetContextWithData()
        {
            var context = new StockExchangeContext(_options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Add some test data
            context.Trades.AddRange(
                new Trade { TradeId = 1, TickerSymbol = "TCS", Price = 2500, Quantity = 100, BrokerId = "BROKER1" },
                new Trade { TradeId = 2, TickerSymbol = "SBI", Price = 560, Quantity = 50, BrokerId = "BROKER2" },
                new Trade { TradeId = 3, TickerSymbol = "IRB", Price = 120.00m, Quantity = 200, BrokerId = "BROKER1" },
                new Trade { TradeId = 4, TickerSymbol = "VOD", Price = 121.00m, Quantity = 100, BrokerId = "BROKER3" },
                new Trade { TradeId = 5, TickerSymbol = "BARC", Price = 150.00m, Quantity = 300, BrokerId = "BROKER2" }
            );
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task CreateTrade_ValidTrade_ReturnsCreated()
        {
            // Arrange
            using (var context = new StockExchangeContext(_options))
            {              
                var newTrade = new Trade
                {
                    TickerSymbol = "AAPL",
                    Price = 150.25m,
                    Quantity = 10,
                    BrokerId = "BROKER4"
                };
                _mockTradeRepository.Setup(repo => repo.AddTradeAsync(newTrade)).Returns(Task.CompletedTask);
                _mockClientProxy.Setup(c => c.SendCoreAsync("ReceiveTrade", It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);

                // Act
                var result = await _controller.CreateTrade(newTrade) as CreatedAtActionResult;


                // Assert
                var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
                Assert.Equal(nameof(StocksController.GetStock), createdAtResult.ActionName);
                _mockTradeRepository.Verify(repo => repo.AddTradeAsync(newTrade), Times.Once);
                _mockClientProxy.Verify(c => c.SendCoreAsync("ReceiveTrade", It.Is<object[]>(o => o[0] == newTrade), default), Times.Once);
            }
        }

        [Fact]
        public async Task CreateTrade_InvalidTrade_ReturnsBadRequest()
        {
            using (var context = new StockExchangeContext(_options))
            {
                var invalidTrade = new Trade
                {
                    TickerSymbol = "AAPL",
                    Price = -1, // Invalid price
                    Quantity = 10,
                    BrokerId = "BROKER4"
                };

                _controller.ModelState.AddModelError("Price", "Price must be a non-negative value."); //manually add model error

                // Act
                var result = await _controller.CreateTrade(invalidTrade) as BadRequestObjectResult;

                // Assert
                Assert.NotNull(result);
                Assert.Equal(400, result.StatusCode);
            }
        }

        [Fact]
        public async Task GetStock_ExistingTicker_ReturnsStockPrice()
        {
            // Arrange
            using (var context = GetContextWithData())
            {
                var repository = new TradeRepository(context, new Mock<ILogger<TradeRepository>>().Object);
                var controller = new StocksController(repository, new Mock<ILogger<StocksController>>().Object, _mockHubContext.Object);

                // Act
                var result = await controller.GetStock("TCS") as ActionResult<StockPrice>;

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Value);
                Assert.Equal("TCS", result.Value.TickerSymbol);
                Assert.Equal(2500, result.Value.AveragePrice); 
            }
        }

        [Fact]
        public async Task GetStocks_ReturnsAllStockPrices()
        {
            // Arrange
            using (var context = GetContextWithData())
            {
                var repository = new TradeRepository(context, new Mock<ILogger<TradeRepository>>().Object);
                var controller = new StocksController(repository, new Mock<ILogger<StocksController>>().Object, _mockHubContext.Object);
                // Act
                var result = await controller.GetStocks();

               
                var objectResult = result.Result as ObjectResult;
                // Assert
                Assert.NotNull(objectResult);
                Assert.NotNull(objectResult.Value);
                var stockPrices = objectResult.Value as IEnumerable<StockPrice>;
                var stockPricesList = stockPrices.ToList();
                Assert.Equal(5, stockPricesList.Count); 
                Assert.Contains(stockPricesList, s => s.TickerSymbol == "TCS");
                Assert.Contains(stockPricesList, s => s.TickerSymbol == "IRB");
                Assert.Contains(stockPricesList, s => s.TickerSymbol == "SBI");
            }
        }

        [Fact]
        public async Task GetStocks_WithTickers_ReturnsFilteredStockPrices()
        {
            // Arrange
            using (var context = GetContextWithData())
            {
                var repository = new TradeRepository(context, new Mock<ILogger<TradeRepository>>().Object);
                var controller = new StocksController(repository, new Mock<ILogger<StocksController>>().Object, new Mock<IHubContext<TradeHub>>().Object);

                // Act
                var result = await controller.GetStocks(tickers: "TCS,SBI") as ActionResult<IEnumerable<StockPrice>>;
                var objectResult = result.Result as ObjectResult;
                // Assert
                Assert.NotNull(objectResult);
                Assert.NotNull(objectResult.Value);
                var stockPrices = objectResult.Value as IEnumerable<StockPrice>;
                var stockPricesList = stockPrices.ToList();
                Assert.Equal(2, stockPricesList.Count);
                Assert.Contains(stockPricesList, s => s.TickerSymbol == "TCS");
                Assert.Contains(stockPricesList, s => s.TickerSymbol == "SBI");
                Assert.DoesNotContain(stockPricesList, s => s.TickerSymbol == "BARC");
            }
        }

        [Fact]
        public async Task CreateTrade_DatabaseError_ReturnsInternalServerError()
        {
            // Arrange
            using (var context = new StockExchangeContext(_options))
            {
                // Mock the repository to throw an exception
                var mockRepository = new Mock<ITradeRepository>();
                mockRepository.Setup(repo => repo.AddTradeAsync(It.IsAny<Trade>()))
                                .ThrowsAsync(new Exception("Simulated database error"));

                var mockLogger = new Mock<ILogger<StocksController>>().Object;
                var controller = new StocksController(mockRepository.Object, mockLogger, new Mock<IHubContext<TradeHub>>().Object);
                var newTrade = new Trade
                {
                    TickerSymbol = "AAPL",
                    Price = 150.25m,
                    Quantity = 10,
                    BrokerId = "BROKER4"
                };

                // Act
                var result = await controller.CreateTrade(newTrade) as ObjectResult;

                // Assert
                Assert.NotNull(result);
                Assert.Equal(500, result.StatusCode);
                Assert.Equal("Internal Server Error", result.Value);
            }
        }

        [Fact]
        public async Task GetStock_RepositoryThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            using (var context = new StockExchangeContext(_options))
            {
                // Mock the repository to throw an exception
                var mockRepository = new Mock<ITradeRepository>();
                mockRepository.Setup(repo => repo.GetAveragePriceAsync(It.IsAny<string>()))
                                .ThrowsAsync(new Exception("Simulated database error"));

                var mockLogger = new Mock<ILogger<StocksController>>().Object;
                var controller = new StocksController(mockRepository.Object, mockLogger, new Mock<IHubContext<TradeHub>>().Object);

                // Act
                var result = await controller.GetStock("LSEG");
                var objectResult = result.Result as ObjectResult;

                // Assert
                Assert.NotNull(objectResult);
                Assert.Equal(500, objectResult.StatusCode);
                Assert.Equal("Internal Server Error", objectResult.Value);
            }
        }

        [Fact]
        public async Task GetStocks_RepositoryThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            using (var context = new StockExchangeContext(_options))
            {
                // Mock the repository to throw an exception
                var mockRepository = new Mock<ITradeRepository>();
                mockRepository.Setup(repo => repo.GetAveragePricesAsync())
                                .ThrowsAsync(new Exception("Simulated database error"));

                var mockLogger = new Mock<ILogger<StocksController>>().Object;
                var controller = new StocksController(mockRepository.Object, mockLogger, new Mock<IHubContext<TradeHub>>().Object);

                // Act
                var result = await controller.GetStocks();
                var objectResult = result.Result as ObjectResult;

                // Assert
                Assert.NotNull(objectResult);
                Assert.Equal(500, objectResult.StatusCode);
                Assert.Equal("Internal Server Error", objectResult.Value);
            }
        }
    }
}