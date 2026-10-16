using System;
using UnityEngine;

namespace Rustbound.Progress
{
    [Serializable]
    public sealed class LevelRecord
    {
        public bool completed;
        public float bestSeconds;
        public int bestCollected;
        public int totalCollectibles;
    }

    [Serializable]
    public sealed class LevelProgressData
    {
        public int version = 1;
        public int unlockedCount = 1;
        public int lastPlayed = 1;
        public int newlyUnlocked;
        public LevelRecord[] records = {new LevelRecord(), new LevelRecord(), new LevelRecord()};
    }

    /// <summary>Progress rules independent of UI and scene loading. Level IDs are 1..3.</summary>
    public sealed class LevelProgressRepository
    {
        public const int LevelCount = 3;
        private const string SaveKey = "Rustbound.LevelProgress.v1";
        private readonly bool persistent;
        private LevelProgressData data;
        public int PreferredLevel => IsUnlocked(data.newlyUnlocked) ? data.newlyUnlocked : data.lastPlayed;
        public int UnlockedCount => data.unlockedCount;
        public int LastPlayed => data.lastPlayed;
        public int NewlyUnlocked => data.newlyUnlocked;

        public LevelProgressRepository(string json = null, bool persistent = false)
        {
            this.persistent = persistent;
            try { data = string.IsNullOrEmpty(json) ? new LevelProgressData() : JsonUtility.FromJson<LevelProgressData>(json); }
            catch (ArgumentException) { data = new LevelProgressData(); }
            Normalize();
        }

        public static LevelProgressRepository Load() => new LevelProgressRepository(PlayerPrefs.GetString(SaveKey, ""), true);
        public string ToJson() => JsonUtility.ToJson(data);
        public bool IsUnlocked(int level) => level >= 1 && level <= data.unlockedCount;
        public LevelRecord GetRecord(int level)
        {
            if (level < 1 || level > LevelCount) throw new ArgumentOutOfRangeException(nameof(level));
            // Return a snapshot so callers cannot mutate saved state without applying the rules.
            var record = data.records[level-1];
            return new LevelRecord {completed=record.completed, bestSeconds=record.bestSeconds,
                bestCollected=record.bestCollected, totalCollectibles=record.totalCollectibles};
        }

        public bool MarkEntered(int level)
        {
            if (!IsUnlocked(level)) return false;
            data.lastPlayed = level;
            data.newlyUnlocked = 0;
            Save();
            return true;
        }

        public bool CompleteLevel(int level, float seconds, int collected, int total)
        {
            if (!IsUnlocked(level) || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds) || total < 0)
                return false;
            var record = data.records[level-1];
            if (!record.completed || seconds < record.bestSeconds) record.bestSeconds = seconds;
            record.completed = true;
            record.bestCollected = Mathf.Max(record.bestCollected, Mathf.Clamp(collected, 0, total));
            record.totalCollectibles = total;
            data.lastPlayed = level;
            if (level == data.unlockedCount && level < LevelCount)
            {
                data.unlockedCount = level + 1;
                data.newlyUnlocked = level + 1;
            }
            Save();
            return true;
        }

        private void Save()
        {
            if (!persistent || !Application.isPlaying) return;
            PlayerPrefs.SetString(SaveKey, ToJson());
            PlayerPrefs.Save();
        }

        private void Normalize()
        {
            if (data == null || data.version != 1) data = new LevelProgressData();
            data.unlockedCount = Mathf.Clamp(data.unlockedCount, 1, LevelCount);
            if (!IsUnlocked(data.lastPlayed)) data.lastPlayed = 1;
            if (!IsUnlocked(data.newlyUnlocked)) data.newlyUnlocked = 0;
            if (data.records == null || data.records.Length != LevelCount) data.records = new LevelRecord[LevelCount];
            for (int i = 0; i < LevelCount; i++)
            {
                if (data.records[i] == null) data.records[i] = new LevelRecord();
                var record = data.records[i];
                if (record.bestSeconds <= 0 || float.IsNaN(record.bestSeconds) || float.IsInfinity(record.bestSeconds))
                { record.completed = false; record.bestSeconds = 0; }
                record.totalCollectibles = Mathf.Max(0, record.totalCollectibles);
                record.bestCollected = Mathf.Clamp(record.bestCollected, 0, record.totalCollectibles);
            }
        }
    }
}
