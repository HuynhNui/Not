using System;
using System.IO;
using UnityEngine;

namespace _Project.Scripts.Systems.SaveSystem
{
    public sealed class LocalSaveRepository
    {
        private const string SaveFileName = "save.json";
        private const string TempFileName = "save.tmp";
        private const string BackupFileName = "save.bak";

        private readonly string _directoryPath;

        public LocalSaveRepository(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        public string SavePath => Path.Combine(_directoryPath, SaveFileName);
        public string BackupPath => Path.Combine(_directoryPath, BackupFileName);

        public bool HasSave()
        {
            return File.Exists(SavePath) || File.Exists(BackupPath);
        }

        public bool TryLoad(out SaveData saveData)
        {
            if (TryReadFile(SavePath, out saveData))
            {
                return true;
            }

            return TryReadFile(BackupPath, out saveData);
        }

        public void Save(SaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            Directory.CreateDirectory(_directoryPath);

            string tempPath = Path.Combine(_directoryPath, TempFileName);
            string json = JsonUtility.ToJson(saveData, prettyPrint: true);
            File.WriteAllText(tempPath, json);

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, overwrite: true);
            }

            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            File.Move(tempPath, SavePath);
        }

        private static bool TryReadFile(string path, out SaveData saveData)
        {
            saveData = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                saveData = JsonUtility.FromJson<SaveData>(json);
                return saveData != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read save file '{path}': {exception.Message}");
                return false;
            }
        }
    }
}
