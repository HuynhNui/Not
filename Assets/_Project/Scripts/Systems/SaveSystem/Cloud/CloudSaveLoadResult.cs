namespace _Project.Scripts.Systems.SaveSystem.Cloud
{
    public readonly struct CloudSaveLoadResult
    {
        public readonly bool HasData;
        public readonly SaveData Data;

        public CloudSaveLoadResult(bool hasData, SaveData data)
        {
            HasData = hasData;
            Data = data;
        }

        public static CloudSaveLoadResult Empty()
        {
            return new CloudSaveLoadResult(false, null);
        }

        public static CloudSaveLoadResult FromData(SaveData data)
        {
            return new CloudSaveLoadResult(data != null, data);
        }
    }
}
