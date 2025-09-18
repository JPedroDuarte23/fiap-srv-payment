using FiapSrvPayment.Domain.Entities;

namespace FiapSrvPayment.Application.Interface
{
    public interface ICartService
    {
        Task<IEnumerable<Game>> GetCart(Guid userId);
        Task AddToCart(Guid gameId, Guid userId);
        Task DeleteFromCart(Guid gameId, Guid userId);

        Task Checkout(Guid userId);
    }
}
