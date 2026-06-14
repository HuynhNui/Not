using System.Threading.Tasks;

namespace _Project.Scripts.Systems.SaveSystem.Cloud
{
    public interface ICloudSaveProvider
    {
        bool IsAvailable { get; }

        Task<CloudSaveLoadResult> TryLoadAsync();
        Task<bool> TrySaveAsync(SaveData saveData);
    }
}
