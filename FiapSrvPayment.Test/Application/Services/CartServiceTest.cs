using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FiapSrvPayment.Application.Exceptions;
using FiapSrvPayment.Application.Interface;
using FiapSrvPayment.Application.Services;
using FiapSrvPayment.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class CartServiceTest
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IGameRepository> _gameRepoMock;
    private readonly Mock<IAmazonSimpleNotificationService> _snsClientMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _service;

    public CartServiceTest()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _gameRepoMock = new Mock<IGameRepository>();
        _snsClientMock = new Mock<IAmazonSimpleNotificationService>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<CartService>>();

        // Setup da configuração para o Topic ARN do SNS
        _configMock.Setup(c => c["SnsTopics:SuccessCheckoutTopicArn"]).Returns("arn:aws:sns:us-east-1:123456789012:success-checkout");

        _service = new CartService(
            _userRepoMock.Object,
            _gameRepoMock.Object,
            _loggerMock.Object,
            _snsClientMock.Object,
            _configMock.Object
        );
    }

    #region AddToCart Tests

    [Fact]
    public async Task AddToCart_ShouldAddGameToCart_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        // Act
        await _service.AddToCart(gameId, userId);

        // Assert
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<Player>(p => p.Cart.Contains(gameId))), Times.Once);
    }

    [Fact]
    public async Task AddToCart_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        // Act & Assert
        await Assert.ThrowsAsync<FiapSrvPayment.Application.Exceptions.NotFoundException>(() => _service.AddToCart(gameId, userId));
    }

    [Fact]
    public async Task AddToCart_ShouldThrowModifyDatabaseException_WhenUpdateFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Player>())).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<ModifyDatabaseException>(() => _service.AddToCart(gameId, userId));
    }

    #endregion

    #region DeleteFromCart Tests

    [Fact]
    public async Task DeleteFromCart_ShouldRemoveGame_WhenUserAndGameInCartExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId } };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        // Act
        await _service.DeleteFromCart(gameId, userId);

        // Assert
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<Player>(p => !p.Cart.Contains(gameId))), Times.Once);
    }

    [Fact]
    public async Task DeleteFromCart_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        // Act & Assert
        await Assert.ThrowsAsync<FiapSrvPayment.Application.Exceptions.NotFoundException>(() => _service.DeleteFromCart(gameId, userId));
    }

    [Fact]
    public async Task DeleteFromCart_ShouldThrowNotFoundException_WhenGameIsNotInCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() }; // Carrinho vazio
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        // Act & Assert
        await Assert.ThrowsAsync<FiapSrvPayment.Application.Exceptions.NotFoundException>(() => _service.DeleteFromCart(gameId, userId));
    }

    [Fact]
    public async Task DeleteFromCart_ShouldThrowModifyDatabaseException_WhenUpdateFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId } };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Player>())).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<ModifyDatabaseException>(() => _service.DeleteFromCart(gameId, userId));
    }

    #endregion

    #region GetCart Tests

    [Fact]
    public async Task GetCart_ShouldReturnGames_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId } };
        var gamesInCart = new List<Game> { new Game { Id = gameId, Title = "Jogo no Carrinho" } };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _gameRepoMock.Setup(r => r.GetByIdsAsync(player.Cart)).ReturnsAsync(gamesInCart);

        // Act
        var result = await _service.GetCart(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Jogo no Carrinho", result.First().Title);
    }

    [Fact]
    public async Task GetCart_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        // Act & Assert
        await Assert.ThrowsAsync<FiapSrvPayment.Application.Exceptions.NotFoundException>(() => _service.GetCart(userId));
    }

    #endregion

    #region Checkout Tests

    [Fact]
    public async Task Checkout_ShouldSucceedAndPublishEvent_WhenCartIsNotEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId }, Library = new List<Guid>() };
        var games = new List<Game> { new Game { Id = gameId, Title = "Jogo Comprado", Price = 50.0 } };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _gameRepoMock.Setup(r => r.GetByIdsAsync(player.Cart)).ReturnsAsync(games);

        // Act
        await _service.Checkout(userId);

        // Assert
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<Player>(p => p.Cart.Count == 0)), Times.Once);
        _snsClientMock.Verify(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Checkout_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        // Act & Assert
        await Assert.ThrowsAsync<FiapSrvPayment.Application.Exceptions.NotFoundException>(() => _service.Checkout(userId));
    }

    [Fact]
    public async Task Checkout_ShouldThrowBadRequestException_WhenCartIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() }; // Carrinho vazio
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _service.Checkout(userId));
    }

    [Fact]
    public async Task Checkout_ShouldThrowModifyDatabaseException_WhenUpdateFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId } };
        var games = new List<Game> { new Game { Id = gameId, Title = "Jogo", Price = 50.0 } };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _gameRepoMock.Setup(r => r.GetByIdsAsync(player.Cart)).ReturnsAsync(games);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Player>())).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<ModifyDatabaseException>(() => _service.Checkout(userId));
    }

    #endregion
}