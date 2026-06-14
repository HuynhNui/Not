namespace _Project.Scripts.Systems.SaveSystem
{
    public sealed class SaveConflict
    {
        public SaveConflict(SaveData localData, SaveData cloudData)
        {
            LocalData = localData != null ? localData.Clone() : null;
            CloudData = cloudData != null ? cloudData.Clone() : null;
        }

        public SaveData LocalData { get; }
        public SaveData CloudData { get; }
    }

    public enum SaveConflictResolution
    {
        UseLocal,
        UseCloud
    }
}
