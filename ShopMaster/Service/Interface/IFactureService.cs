using ShopMaster.Models;

namespace ShopMaster.Service.Interface
{
    public interface IFactureService
    {
        Task<FactureViewModel> GenererFactureAsync(int commandeId);
        Task EnvoyerFactureParEmailAsync(int commandeId, string emailClient);
    }
}
