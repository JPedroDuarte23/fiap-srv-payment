using System.Text.Json;
using FiapSrvPayment.Application.Exceptions;
using FiapSrvPayment.Application.Interface;
using FiapSrvPayment.Domain.Entities;
using Microsoft.Extensions.Logging;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;

namespace FiapSrvPayment.Application.Services;

public class CartService : ICartService
{
    private readonly IUserRepository _userRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<CartService> _logger;
    private readonly IAmazonSimpleNotificationService _snsClient; 
    private readonly IConfiguration _configuration;
    public CartService(IUserRepository userRepository, IGameRepository gameRepository, ILogger<CartService> logger)
    {
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task AddToCart(Guid gameId, Guid userId)
    {
        _logger.LogInformation("Iniciando Adição do jogo {GameId} ao carrinho do usuário {UserId}", gameId, userId);
        var user = (Player) await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado ao tentar adicionar jogo {GameId} ao carrinho", userId, gameId);
            throw new Exceptions.NotFoundException("Usuário não encontrado");

        }

        user.Cart.Add(gameId);

        try
        {
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Jogo {GameId} adicionado com sucesso ao carrinho do usuário {UserId}", gameId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar jogo {GameId} ao carrinho do usuário {UserId}", gameId, userId);
            throw new ModifyDatabaseException(ex.Message);
        }

    }

    public async Task DeleteFromCart(Guid gameId, Guid userId)
    {
        _logger.LogInformation("Iniciando remoção do jogo {GameId} do carrinho do usuário {UserId}", gameId, userId);

        var user = await _userRepository.GetByIdAsync(userId) as Player;
        
        if (user == null)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado ao tentar remover jogo {GameId} do carrinho", userId, gameId);
            throw new Exceptions.NotFoundException("Usuário não encontrado");
        }
            

        if (!user.Cart.Contains(gameId))
        {
            _logger.LogWarning("Jogo {GameId} não está no carrinho do usuário {UserId}", gameId, userId);
            throw new Exceptions.NotFoundException("Jogo não está na biblioteca.");

        }

        user.Cart.Remove(gameId);

        try
        {
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Jogo {GameId} removido com sucesso do carrinho do usuário {UserId}", gameId, userId);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Erro ao remover jogo {GameId} do carrinho do usuário {UserId}", gameId, userId);
            throw new ModifyDatabaseException(ex.Message);
        }
    }
    public async Task<IEnumerable<Game>> GetCart(Guid userId)
    {
        _logger.LogInformation("Buscando carrinho do usuário {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId) as Player;
        if (user == null)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado ao buscar carrinho", userId);
            throw new Exceptions.NotFoundException("Usuário não encontrado");
        }
            
        var cart = await _gameRepository.GetByIdsAsync(user.Cart);

        _logger.LogInformation("Retornando {Count} jogos do carrinho do usuário {UserId}", cart.Count(), userId); 
        return cart;
    }

    public async Task Checkout(Guid userId)
    {
        _logger.LogInformation("Iniciando checkout do carrinho do usuário {UserId}", userId);
        var user = await _userRepository.GetByIdAsync(userId) as Player;
        if (user == null)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado ao tentar fazer checkout", userId);
            throw new Exceptions.NotFoundException("Usuário não encontrado");
        }

        if (user.Cart.Count == 0)
        {
            _logger.LogWarning("Carrinho vazio para o usuário {UserId} ao tentar fazer checkout", userId);
            throw new InvalidOperationException("Carrinho vazio");
        }
        var games = await _gameRepository.GetByIdsAsync(user.Cart);
        var totalPrice = games.Sum(g => g.Price);
        var gamesPurchased = user.Cart;
        user.Library.AddRange(user.Cart);
        user.Cart.Clear();
        try
        {
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Checkout realizado com sucesso para o usuário {UserId}.", userId);

            var message = new
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.Name,
                GameIds = gamesPurchased,
                TotalPrice = totalPrice,
                PurchaseDate = DateTime.UtcNow
            };

            var topicArn = _configuration["SnsTopics:SuccessCheckoutTopicArn"];
            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message),
                MessageGroupId = "checkout" 
            };

            await _snsClient.PublishAsync(publishRequest);
            _logger.LogInformation("Mensagem de checkout para o usuário {UserId} publicada no tópico SNS.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar checkout para o usuário {UserId}", userId);
            throw new ModifyDatabaseException(ex.Message);
        }
    }
}
