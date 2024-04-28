﻿using ProjectM;
using ProjectM.Network;
using RPGMods.Systems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Faction = RPGMods.Utils.Prefabs.Faction;

namespace RPGMods.Utils
{
    public struct LevelData
    {
        public float Level;
        public DateTime TimeStamp;

        public LevelData(float level = 0, DateTime timeStamp = default)
        {
            Level = level;
            TimeStamp = timeStamp;
        }
    }

    public struct BuffData
    {
        public string source;
        public int targetStat;
        public int modificationType;
        public double value;
        public int ID;
        public bool isApplied;

    }

    public struct PowerUpData
    {
        public string Name { get; set; }
        public float MaxHP { get; set; }
        public float PATK { get; set; }
        public float PDEF { get; set; }
        public float SATK { get; set; }
        public float SDEF { get; set; }
    }
    public struct StatsBonus()
    {
        public int Level_Int { get; set; } = 0;
        public float HP_Float { get; set; } = 0;
        public float PhysicalPower_Float { get; set; } = 0;
        public float PhysicalResistance_Float { get; set; } = 0;
        public float PhysicalCriticalStrikeChance_Float { get; set; } = 0;
        public float PhysicalCriticalStrikeDamage_Float { get; set; } = 0;
        public float SpellPower_Float { get; set; } = 0;
        public float SpellResistance_Float { get; set; } = 0;
        public float SpellCriticalStrikeChance_Float { get; set; } = 0;
        public float SpellCriticalStrikeDamage_Float { get; set; } = 0;
        public float DamageVsPlayerVampires_Float { get; set; } = 0;
        public float ResistVsPlayerVampires_Float { get; set; } = 0;
        public int FireResistance_Int { get; set; } = 0;
    }

    public struct FactionData(Prefabs.Faction faction)
    {
        public string Name { get; set; } = $"Faction_{Enum.GetName(faction)}";
        public bool Active { get; set; } = false;
        public int Level { get; set; } = 0;
        public int MaxLevel { get; set; } = 0;
        public int MinLevel { get; set; } = 0;
        public int ActivePower { get; set; } = 0;
        public int StoredPower { get; set; } = 0;
        public int DailyPower { get; set; } = 0;
        public int RequiredPower { get; set; } = 0;
        public StatsBonus FactionBonus { get; set; } = new();
    }

    public struct PlayerHeatData {
        public struct Heat {
            public int level { get; set; }
            public DateTime lastAmbushed { get; set; }
        }
        
        public Dictionary<Faction, Heat> heat { get; } = new();
        public DateTime lastCooldown { get; set; }
        public bool isLogging { get; set; }

        public PlayerHeatData() {
            foreach (Faction faction in FactionHeat.ActiveFactions) {
                heat[faction] = new();
            }
        }
    }

    public struct PlayerGroup()
    {
        public HashSet<Entity> Allies { get; } = new();
        public HashSet<Entity> Enemies { get; } = new();
        public DateTime TimeStamp { get; } = DateTime.Now;
    }

    public struct SiegeData
    {
        public bool IsSiegeOn { get; set; }
        public DateTime SiegeEndTime { get; set; }
        public DateTime SiegeStartTime { get; set; }

        public SiegeData(bool isSiegeOn = false, DateTime siegeEndTime = default, DateTime siegeStartTime = default)
        {
            IsSiegeOn = isSiegeOn;
            SiegeEndTime = siegeEndTime;
            SiegeStartTime = siegeStartTime;
        }
    }

    public struct StateData
    {
        public ulong SteamID { get; set; }
        public bool IsHostile { get; set; }

        public StateData (ulong steamID = 0, bool isHostile = false)
        {
            SteamID = steamID;
            IsHostile = isHostile;
        }
    }

    public struct PlayerData
    {
        public FixedString64 CharacterName { get; set; }
        public ulong SteamID { get; set; }
        public bool IsOnline { get; set; }
        public Entity UserEntity { get; set; }
        public Entity CharEntity { get; set; }
        public PlayerData( FixedString64 characterName = default, ulong steamID = 0, bool isOnline = false, Entity userEntity = default, Entity charEntity = default)
        {
            CharacterName = characterName;
            SteamID = steamID;
            IsOnline = isOnline;
            UserEntity = userEntity;
            CharEntity = charEntity;
        }
    }

    public struct ReputationLog
    {
        public int TotalGained { get; set; }
        public DateTime TimeStamp { get; set; }
        public ReputationLog(int totalGained = 0, DateTime timeStamp = default)
        {
            TotalGained = totalGained;
            TimeStamp = timeStamp;
        }
    }

    public struct PvPOffenseLog
    {
        public int Offense { get; set; }
        public DateTime LastOffense { get; set; }

        public PvPOffenseLog(int offense = 0, DateTime lastOffense = default)
        {
            Offense = offense;
            LastOffense = lastOffense;
        }
    }

    public struct HonorRankInfo
    {
        public string Title { get; set; }
        public int HonorRank { get; set; }
        public int Rewards { get; set; }
        public HonorRankInfo( string title = "default", int honorRank = 0, int rewards = 0)
        {
            Title = title;
            HonorRank = honorRank;
            Rewards = rewards;
        }
    }

    public struct PvPData
    {
        public string PlayerName { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public double KD { get; set; }
        public int Reputation { get; set; }
        public string Title { get; set; }
        public PvPData(string playerName = "default", int kills = 0, int deaths = 0, float kd = 0f, int reputation = 0, string title = "default")
        {
            PlayerName = playerName;
            Kills = kills;
            Deaths = deaths;
            KD = kd;
            Reputation = reputation;
            Title = title;
        }
    }

    public struct Float2
    {
        public float x { get; set; }
        public float y { get; set; }
        public Float2(float X, float Y)
        {
            x = X;
            y = Y;
        }
    }
    public struct newWaypointData {
        public float x;
        public float y;
        public float z;
        public newWaypointData(float X, float Y, float Z) {x = X; y = Y; z = Z; }
    }

        public struct WaypointData
    {
        public string Name { get; set; }
        public ulong Owner { get; set; }
        public float3 Location { get; set; }
        public WaypointData(string name, ulong owner, float3 location)
        {
            Name = name;
            Owner = owner;
            Location = location;
        }
    }

    public struct WeaponMasterDataOld
    {
        public int Spear { get; set; }
        public int Sword { get; set; }
        public int Scythe { get; set; }
        public int Crossbow { get; set; }
        public int Mace { get; set; }
        public int Slashers { get; set; }
        public int Axes { get; set; }
        public int None { get; set; }
        public int FishingPole { get; set; }
        public int Spell { get; set; }

        public WeaponMasterDataOld(int spear = 0, int sword = 0, int scythe = 0, int crossbow = 0, int mace = 0, int slashers = 0, int axes = 0, int none = 0, int fishingpole = 0, int spell = 0)
        {
            Spear = spear;
            Sword = sword;
            Scythe = scythe;
            Crossbow = crossbow;
            Mace = mace;
            Slashers = slashers;
            Axes = axes;
            None = none;
            FishingPole = fishingpole;
            Spell = spell;
        }
    }


    public struct BloodlineData{
        public double[] strength { get; set; }
        public double[] efficency { get; set; }
        public double[] growth { get; set; }
        public BloodlineData(double[] strengthIn, double[] efficencyIn, double[] growthIn){
            strength = strengthIn;
            efficency = efficencyIn;
            growth = growthIn;
        }
        public BloodlineData() {
            strength = new double[Bloodlines.rates.Length];
            efficency = new double[Bloodlines.rates.Length];
            growth = new double[Bloodlines.rates.Length];
            for (int i = 0; i < strength.Length; i++) {
                strength[i] = 0.0;
                efficency[i] = 1.0;
                growth[i] = 1.0;
            }
        }
    }

    public struct WeaponMasterData
    {
        public double[] mastery { get; set; }
        public double[] efficency { get; set; }
        public double[] growth { get; set; }

        public WeaponMasterData(double[] strengthIn, double[] efficencyIn, double[] growthIn) {
            mastery = strengthIn;
            efficency = efficencyIn;
            growth = growthIn;
            for (int i = 0; i < mastery.Length; i++) {
                mastery[i] = 0.0;
                efficency[i] = 1.0;
                growth[i] = 1.0;
            }
        }
        public WeaponMasterData() {
            mastery = new double[WeaponMasterSystem.masteryRates.Length];
            efficency = new double[WeaponMasterSystem.masteryRates.Length];
            growth = new double[WeaponMasterSystem.masteryRates.Length];
            for (int i = 0; i < mastery.Length; i++) {
                mastery[i] = 0.0;
                efficency[i] = 1.0;
                growth[i] = 1.0;
            }
        }
    }


    public struct BanData
    {
        public DateTime BanUntil { get; set; }
        public string Reason { get; set; }
        public string BannedBy { get; set; }
        public ulong SteamID { get; set; }

        public BanData(DateTime banUntil = default(DateTime), string reason = "Invalid", string bannedBy = "Default", ulong steamID = 0)
        {
            BanUntil = banUntil;
            Reason = reason;
            BannedBy = bannedBy;
            SteamID = steamID;
        }
    }

    public struct SpawnOptions
    {
        public bool ModifyBlood { get; set; }
        public PrefabGUID BloodType { get; set; }
        public float BloodQuality { get; set; }
        public bool BloodConsumeable { get; set; }
        public bool ModifyStats { get; set; }
        public UnitStats UnitStats { get; set; }
        public bool Process { get; set; }

        public SpawnOptions(bool modifyBlood = false, PrefabGUID bloodType = default, float bloodQuality = 0, bool bloodConsumeable = true, bool modifyStats = false, UnitStats unitStats = default, bool process = false)
        {
            ModifyBlood = modifyBlood;
            BloodType = bloodType;
            BloodQuality = bloodQuality;
            BloodConsumeable = bloodConsumeable;
            ModifyStats = modifyStats;
            UnitStats = unitStats;
            Process = process;
        }
    }

    public struct SpawnNPCListen
    {
        public float Duration { get; set; }
        public int EntityIndex { get; set; }
        public int EntityVersion { get; set; }
        public SpawnOptions Options { get; set; }
        public bool Process { get; set; }

        public SpawnNPCListen(float duration = 0.0f, int entityIndex = 0, int entityVersion = 0, SpawnOptions options = default, bool process = true)
        {
            Duration = duration;
            EntityIndex = entityIndex;
            EntityVersion = entityVersion;
            Options = options;
            Process = process;
        }

        public Entity getEntity()
        {
            Entity entity = new Entity()
            {
                Index = this.EntityIndex,
                Version = this.EntityVersion,
            };
            return entity;
        }
    }

    public struct VChatEvent
    {
        public Entity SenderUserEntity { get; set; }
        public Entity SenderCharacterEntity { get; set; }
        public string Message { get; set; }
        public ChatMessageType Type { get; set; }
        public User User { get; set; }

        public VChatEvent(Entity senderUserEntity, Entity senderCharacterEntity, string message, ChatMessageType type, User user)
        {
            SenderUserEntity = senderUserEntity;
            SenderCharacterEntity = senderCharacterEntity;
            Message = message;
            Type = type;
            User = user;
        }
    }

    public sealed class SizedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {

        private int maxSize;
        private Queue<TKey> keys;

        public SizedDictionary(int size)
        {
            maxSize = size;
            keys = new Queue<TKey>();
        }

        public new void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException();
            base.TryAdd(key, value);
            keys.Enqueue(key);
            if (keys.Count > maxSize) base.Remove(keys.Dequeue());
        }

        public new bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException();
            if (!keys.Contains(key)) return false;
            var newQueue = new Queue<TKey>();
            while (keys.Count > 0)
            {
                var thisKey = keys.Dequeue();
                if (!thisKey.Equals(key)) newQueue.Enqueue(thisKey);
            }
            keys = newQueue;
            return base.Remove(key);
        }
    }

    public sealed class SizedDictionaryAsync<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {

        private int maxSize;
        private Queue<TKey> keys;

        public SizedDictionaryAsync(int size)
        {
            maxSize = size;
            keys = new Queue<TKey>();
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException();
            base.TryAdd(key, value);
            keys.Enqueue(key);
            if (keys.Count > maxSize) base.TryRemove(keys.Dequeue(), out _);
        }

        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException();
            if (!keys.Contains(key)) return false;
            var newQueue = new Queue<TKey>();
            while (keys.Count > 0)
            {
                var thisKey = keys.Dequeue();
                if (!thisKey.Equals(key)) newQueue.Enqueue(thisKey);
            }
            keys = newQueue;
            return base.TryRemove(key, out _);
        }
    }
}
