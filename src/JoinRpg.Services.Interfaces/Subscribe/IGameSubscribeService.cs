using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Subscribe
{
    public interface IGameSubscribeService
    {
        Task UpdateSubscribeForGroup(SubscribeForGroupRequest request);
    }
}
