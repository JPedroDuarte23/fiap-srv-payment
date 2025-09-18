using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiapSrvPayment.Application.Exceptions;
using FiapSrvPayment.Application.Interface;
using FiapSrvPayment.Application.Services;
using FiapSrvPayment.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class CartServiceTest
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IGameRepository> _gameRepoMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _service;

    public CartServiceTest()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _gameRepoMock = new Mock<IGameRepository>();
        _loggerMock = new Mock<ILogger<CartService>>();
        _service = new CartService(_userRepoMock.Object, _gameRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddToCart_AddsGame_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        await _service.AddToCart(gameId, userId);

        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<Player>(p => p.Cart.Contains(gameId))), Times.Once);
    }

    [Fact]
    public async Task AddToCart_ThrowsNotFoundException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.AddToCart(gameId, userId));
    }

    [Fact]
    public async Task DeleteFromCart_RemovesGame_WhenGameExistsInCart()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid> { gameId } };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        await _service.DeleteFromCart(gameId, userId);

        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<Player>(p => !p.Cart.Contains(gameId))), Times.Once);
    }

    [Fact]
    public async Task DeleteFromCart_ThrowsNotFoundException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteFromCart(gameId, userId));
    }

    [Fact]
    public async Task DeleteFromCart_ThrowsNotFoundException_WhenGameNotInCart()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var player = new Player { Id = userId, Cart = new List<Guid>() };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteFromCart(gameId, userId));
    }

    [Fact]
    public async Task GetCart_ReturnsGames_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var cartIds = new List<Guid> { Guid.NewGuid() };
        var player = new Player { Id = userId, Cart = cartIds };
        var games = new List<Game> { new Game { Id = cartIds[0], Title = "Game" } };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(player);
        _gameRepoMock.Setup(r => r.GetByIdsAsync(cartIds)).ReturnsAsync(games);

        var result = await _service.GetCart(userId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetCart_ThrowsNotFoundException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Player)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetCart(userId));
    }
}
