using System.Threading.Tasks;

namespace _Project.Scripts.Systems.SaveSystem.Cloud
{
    public sealed class NoOpCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false;

        public Task<CloudSaveLoadResult> TryLoadAsync()
        {
            return Task.FromResult(CloudSaveLoadResult.Empty());
        }

        public Task<bool> TrySaveAsync(SaveData saveData)
        {
            return Task.FromResult(false);
        }
    }
}
