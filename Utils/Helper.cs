﻿using ProjectM;
using ProjectM.Network;
using System;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RPGMods.Hooks;
using RPGMods.Systems;
using System.Text.RegularExpressions;
using ProjectM.Scripting;
using System.Collections.Generic;
using VampireCommandFramework;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Text.Json;
using Epic.OnlineServices.Stats;
using RPGMods.Commands;
using Unity.Entities.UniversalDelegates;

namespace RPGMods.Utils
{
    public class LazyDictionary<TKey,TValue> : Dictionary<TKey,TValue> where TValue : new()
    {
        public new TValue this[TKey key]
        {
            get 
            {
                if (!base.ContainsKey(key)) base.Add(key, new TValue());
                return base[key];
            }
            set 
            {
                if (!base.ContainsKey(key)) base.Add(key, value);
                else base[key] = value;
            }
        }
    }
    public static class Helper
    {
        private static Entity empty_entity = new Entity();
        private static System.Random rand = new System.Random();

        public static ServerGameSettings SGS = default;
        public static ServerGameManager SGM = default;
        public static UserActivityGridSystem UAGS = default;
        public static int groupRange = 50;

        public static int buffGUID = 1444835872;
        public static int forbiddenBuffGUID = -161632603;
        public static bool buffLogging = false;
        public static bool deathLogging = true;
        public static PrefabGUID appliedBuff = Database.Buff.Buff_VBlood_Perk_Moose;

        public static Regex rxName = new Regex(@"(?<=\])[^\[].*");
        
        public static bool GetUserActivityGridSystem(out UserActivityGridSystem uags)
        {
            uags = Plugin.Server.GetExistingSystem<AiPrioritizationSystem>()?._UserActivityGridSystem;
            return true;
        }

        public static bool GetServerGameManager(out ServerGameManager sgm)
        {
            sgm = (ServerGameManager)Plugin.Server.GetExistingSystem<ServerScriptMapper>()?._ServerGameManager;
            return true;
        }

        public static ModifyUnitStatBuff_DOTS makeBuff(int statID, double strength) {
            ModifyUnitStatBuff_DOTS buff;

            var modType = ModificationType.Add;
            if (Helper.inverseMultiplierStats.Contains(statID)) {
                if (statID == (int)UnitStatType.CooldownModifier && !WeaponMasterSystem.CDRStacks) {
                    modType = ModificationType.Set;
                } else if (Helper.multiplierStats.Contains(statID)) {
                    modType = ModificationType.Multiply;
                }
            }
            buff = (new ModifyUnitStatBuff_DOTS() {
                StatType = (UnitStatType)statID,
                Value = (float)strength,
                ModificationType = modType,
                Id = ModificationId.NewId(0)
            });
            return buff;
        }
        public static bool humanReadablePercentageStats = false;
        public static bool inverseMultipersDisplayReduction = true;
        public static double calcBuffValue(double strength, double effectiveness, double rate, int statID) {

            if (Helper.percentageStats.Contains(statID) && humanReadablePercentageStats) {
                rate /= 100;
            }
                double value = strength * rate * effectiveness;
            if (Helper.inverseMultiplierStats.Contains(statID)) {
                if (WeaponMasterSystem.linearCDR) {
                    value = strength * effectiveness;
                    value = value / (value + rate);
                } else {
                    value = (strength * effectiveness) / (rate * 2);
                }
                value = 1 - value;
            }
            return value;
        }

        public static bool GetServerGameSettings(out ServerGameSettings settings)
        {
            settings = Plugin.Server.GetExistingSystem<ServerGameSettingsSystem>()?._Settings;
            return true;
        }

        // Get allies for PlayerCharacter (ie, every vampire in the clan), cached for 5 minutes
        // The list of allies includes PlayerCharacter.
        public static int GetAllies(Entity playerCharacter, out PlayerGroup playerGroup) {
            if (!Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(playerCharacter)) {
                if (ExperienceSystem.xpLogging) {
                    Plugin.Logger.LogInfo($"{DateTime.Now}: Entity is not user: {playerCharacter}");
                    Plugin.Logger.LogInfo($"{DateTime.Now}: Components for Player Character are: {Plugin.Server.EntityManager.Debug.GetEntityInfo(playerCharacter)}");
                }

                playerGroup = new PlayerGroup {
                    Allies = new Dictionary<Entity, Entity>()
                };
                return 0;
            }

            if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo($"{DateTime.Now}: Beginning To Parse Player Group");
            if (Cache.PlayerAllies.TryGetValue(playerCharacter, out playerGroup)) {
                if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo($"{DateTime.Now}: Allies Found in Cache, timestamp is {playerGroup.TimeStamp}");
                TimeSpan CacheAge = DateTime.Now - playerGroup.TimeStamp;
                if (CacheAge.TotalSeconds < 300) return playerGroup.AllyCount;
                if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo($"{DateTime.Now}: Refreshing cached allies");
            }
            
            // Check if the player has a team
            if (!Plugin.Server.EntityManager.TryGetComponentData(playerCharacter, out Team playerTeam)) {
                if (ExperienceSystem.xpLogging) {
                    Plugin.Logger.LogInfo($"{DateTime.Now}: Could not get team for Player Character: {playerCharacter}");
                    Plugin.Logger.LogInfo($"{DateTime.Now}: Components for Player Character are: {Plugin.Server.EntityManager.Debug.GetEntityInfo(playerCharacter)}");
                }

                playerGroup = new PlayerGroup {
                    Allies = new Dictionary<Entity, Entity>()
                };
                return 0;
            } else if (ExperienceSystem.xpLogging) {
                Plugin.Logger.LogInfo($"{DateTime.Now}: Player Character Found Value: {playerTeam.Value} - Faction Index: {playerTeam.FactionIndex}");
            }
            
            playerGroup.TimeStamp = DateTime.Now;

            Dictionary<Entity, Entity> group = new();

            var query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlayerCharacter>(),
                        ComponentType.ReadOnly<IsConnected>()
                    },
                Options = EntityQueryOptions.IncludeDisabled
            });
            var allyBuffer = query.ToEntityArray(Allocator.Temp);
            if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo($"{DateTime.Now}: got connected PC entities buffer of length {allyBuffer.Length}");
            
            foreach (var entity in allyBuffer) {
                if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo(DateTime.Now + ": got Entity " + entity);
                if (Plugin.Server.EntityManager.HasComponent<PlayerCharacter>(entity)) {
                    if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo(DateTime.Now + ": Entity is User " + entity);
                    if (entity.Equals(playerCharacter)) {
                        if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo(DateTime.Now + ": Entity is self");
                        // We are an ally of ourself.
                        group[entity] = entity;
                        continue;
                    }

                    bool allies = false;
                    try {
                        if (ExperienceSystem.xpLogging)
                            Plugin.Logger.LogInfo(DateTime.Now + ": Trying to get teams ");
                        bool teamFound = Plugin.Server.EntityManager.TryGetComponentData(entity, out Team entityTeam);
                        if (ExperienceSystem.xpLogging) {
                            if (teamFound)
                                Plugin.Logger.LogInfo(DateTime.Now + ": Team Value:" + entityTeam.Value +
                                                      " - Faction Index: " + entityTeam.FactionIndex);
                            else {
                                Plugin.Logger.LogInfo(DateTime.Now + ": Could not get team for entity: " + entity);
                                Plugin.Logger.LogInfo(DateTime.Now + ": Components for entity are: " +
                                                      Plugin.Server.EntityManager.Debug.GetEntityInfo(entity));
                            }
                        }

                        allies = teamFound && entityTeam.Value == playerTeam.Value;
                    }
                    catch (Exception e) {
                        if (ExperienceSystem.xpLogging)
                            Plugin.Logger.LogInfo(DateTime.Now + ": IsAllies Failed " + e.Message);
                    }

                    if (allies) {
                        if (ExperienceSystem.xpLogging)
                            Plugin.Logger.LogInfo($"{DateTime.Now}: Allies: {playerCharacter} - {entity}");
                        group[entity] = entity;
                    }
                    else {
                        if (ExperienceSystem.xpLogging)
                            Plugin.Logger.LogInfo($"{DateTime.Now}: Not allies: {playerCharacter} - {entity}");

                    }
                }
                else {
                    if (ExperienceSystem.xpLogging) Plugin.Logger.LogInfo(DateTime.Now + ": No Associated User!");
                }
            }


            playerGroup.Allies = group;
            playerGroup.AllyCount = group.Count;
            Cache.PlayerAllies[playerCharacter] = playerGroup;

            return playerGroup.AllyCount;
        }

        public static FixedString64 GetTrueName(string name)
        {
            MatchCollection match = rxName.Matches(name);
            if (match.Count > 0)
            {
                name = match[match.Count - 1].ToString();
            }
            return name;
        }

        public static void CreatePlayerCache() {

            Cache.NamePlayerCache.Clear();
            Cache.SteamPlayerCache.Clear();
            EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<User>()
                    },
                Options = EntityQueryOptions.IncludeDisabled
            });
            var userEntities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in userEntities) {
                var userData = Plugin.Server.EntityManager.GetComponentData<User>(entity);
                PlayerData playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entity, userData.LocalCharacter._Entity);

                Cache.NamePlayerCache.TryAdd(GetTrueName(userData.CharacterName.ToString().ToLower()), playerData);
                Cache.SteamPlayerCache.TryAdd(userData.PlatformId, playerData);

            }

            Plugin.Logger.LogWarning("Player Cache Created.");
        }

        public static Dictionary<TKey, TVal> LoadDB<TKey,TVal>(string specificFile) {
            Dictionary<TKey, TVal> dict;
            Helper.confirmFile(AutoSaveSystem.mainSaveFolder, specificFile);
            Helper.confirmFile(AutoSaveSystem.backupSaveFolder, specificFile);
            string json = File.ReadAllText(AutoSaveSystem.mainSaveFolder + specificFile);
            try {
                dict = JsonSerializer.Deserialize<Dictionary<TKey, TVal>>(json);
                if (dict == null) {
                    json = File.ReadAllText(AutoSaveSystem.backupSaveFolder + specificFile);
                    dict = JsonSerializer.Deserialize<Dictionary<TKey, TVal>>(json);
                }
            } catch {
                dict = new Dictionary<TKey, TVal>();
                Plugin.Logger.LogWarning(DateTime.Now+ ": DB Created for " + specificFile);
            }
            return dict;
        }
        public static void TeleportTo(ChatCommandContext ctx, Tuple<float,float,float> position) {

            var entity = Plugin.Server.EntityManager.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
                );

            Plugin.Server.EntityManager.SetComponentData<FromCharacter>(entity, new() {
                User = ctx.Event.SenderUserEntity,
                Character = ctx.Event.SenderCharacterEntity
            });

            Plugin.Server.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new() {
                Position = new float3(position.Item1, position.Item2, position.Item3),
                Target = PlayerTeleportDebugEvent.TeleportTarget.Self
            });
        }
        public static void UpdatePlayerCache(Entity userEntity, string oldName, string newName, bool forceOffline = false)
        {
            var userData = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
            Cache.NamePlayerCache.Remove(GetTrueName(oldName.ToLower()));

            if (forceOffline) userData.IsConnected = false;
            PlayerData playerData = new PlayerData(newName, userData.PlatformId, userData.IsConnected, userEntity, userData.LocalCharacter._Entity);

            Cache.NamePlayerCache[GetTrueName(newName.ToLower())] = playerData;
            Cache.SteamPlayerCache[userData.PlatformId] = playerData;
        }

        public static bool RenamePlayer(Entity userEntity, Entity charEntity, FixedString64 newName)
        {
            //-- Max Char Length for FixedString64 is 61 bytes.
            //if (newName.utf8LengthInBytes > 61)
            //{
            //    return false;
            //}

            var userData = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
            UpdatePlayerCache(userEntity, userData.CharacterName.ToString(), newName.ToString());

            var des = Plugin.Server.GetExistingSystem<DebugEventsSystem>();
            var networkId = Plugin.Server.EntityManager.GetComponentData<NetworkId>(userEntity);
            var renameEvent = new RenameUserDebugEvent
            {
                NewName = newName,
                Target = networkId
            };
            var fromCharacter = new FromCharacter
            {
                User = userEntity,
                Character = charEntity
            };
            des.RenameUser(fromCharacter, renameEvent);
            return true;
        }

        public static bool ValidateName(string name, out CreateCharacterFailureReason invalidReason)
        {
            if (Regex.IsMatch(name, @"[^a-zA-Z0-9]"))
            {
                invalidReason = CreateCharacterFailureReason.InvalidName;
                return false;
            }

            //-- The game default max byte length is 20.
            //-- The max legth assignable is actually 61 bytes.
            FixedString64 charName = name;
            if (charName.utf8LengthInBytes > 20)
            {
                invalidReason = CreateCharacterFailureReason.InvalidName;
                return false;
            }

            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out _))
            {
                invalidReason = CreateCharacterFailureReason.NameTaken;
                return false;
            }

            invalidReason = CreateCharacterFailureReason.None;
            return true;
        }

        public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
        {
            var des = Plugin.Server.GetExistingSystem<DebugEventsSystem>();
            var fromCharacter = new FromCharacter()
            {
                User = User,
                Character = Char
            };
            var buffEvent = new ApplyBuffDebugEvent()
            {
                BuffPrefabGUID = GUID
            }
            ;
            Database.playerBuffs.Add(buffEvent);
            des.ApplyBuff(fromCharacter, buffEvent);
            
        }

        public static void RemoveBuff(Entity Char, PrefabGUID GUID)
        {
            if (BuffUtility.HasBuff(Plugin.Server.EntityManager, Char, GUID))
            {
                BuffUtility.TryGetBuff(Plugin.Server.EntityManager, Char, GUID, out var BuffEntity_);
                Plugin.Server.EntityManager.AddComponent<DestroyTag>(BuffEntity_);
                return;
            }
        }

        public static string GetNameFromSteamID(ulong SteamID)
        {
            //var UserEntities = Plugin.Server.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
            //foreach (var Entity in UserEntities)
            //{
            //    var EntityData = Plugin.Server.EntityManager.GetComponentData<User>(Entity);
            //    if (EntityData.PlatformId == SteamID) return EntityData.CharacterName.ToString();
            //}
            //return null;
            if (Cache.SteamPlayerCache.TryGetValue(SteamID, out var data))
            {
                return data.CharacterName.ToString();
            }
            else
            {
                return null;
            }
        }

        public static PrefabGUID GetGUIDFromName(string name)
        {
            var gameDataSystem = Plugin.Server.GetExistingSystem<GameDataSystem>();
            var managed = gameDataSystem.ManagedDataRegistry;

            foreach (var entry in gameDataSystem.ItemHashLookupMap)
            {
                try
                {
                    var item = managed.GetOrDefault<ManagedItemData>(entry.Key);
                    if (item.PrefabName.StartsWith("Item_VBloodSource") || item.PrefabName.StartsWith("GM_Unit_Creature_Base") || item.PrefabName == "Item_Cloak_ShadowPriest") continue;
                    if (item.Name.ToString().ToLower().Equals( name.ToLower())){
                        return entry.Key;
                    }
                    if (item.PrefabName.ToLower().Equals(name.ToLower())) { return entry.Key; }
                    //if (item.PrefabName.Substring(item.PrefabName.IndexOf("_"+1)).ToLower().Equals(name.ToLower())) { return entry.Key; }
                }
                catch { }
            }

            return new PrefabGUID(0);
        }

        public static void KickPlayer(Entity userEntity)
        {
            EntityManager em = Plugin.Server.EntityManager;
            var userData = em.GetComponentData<User>(userEntity);
            int index = userData.Index;
            NetworkId id = em.GetComponentData<NetworkId>(userEntity);

            var entity = em.CreateEntity(
                ComponentType.ReadOnly<NetworkEventType>(),
                ComponentType.ReadOnly<SendEventToUser>(),
                ComponentType.ReadOnly<KickEvent>()
            );

            var KickEvent = new KickEvent()
            {
                PlatformId = userData.PlatformId
            };

            em.SetComponentData<SendEventToUser>(entity, new()
            {
                UserIndex = index
            });
            em.SetComponentData<NetworkEventType>(entity, new()
            {
                EventId = NetworkEvents.EventId_KickEvent,
                IsAdminEvent = false,
                IsDebugEvent = false
            });

            em.SetComponentData(entity, KickEvent);
        }

        public static void AddItemToInventory(ChatCommandContext ctx, PrefabGUID guid, int amount)
        {
            /*
            unsafe
            {
                var gameData = Plugin.Server.GetExistingSystem<GameDataSystem>();
                var bytes = stackalloc byte[Marshal.SizeOf<FakeNull>()];
                var bytePtr = new IntPtr(bytes);
                Marshal.StructureToPtr<FakeNull>(new()
                {
                    value = 7,
                    has_value = true
                }, bytePtr, false);
                var boxedBytePtr = IntPtr.Subtract(bytePtr, 0x10);
                var hack = new Il2CppSystem.Nullable<int>(boxedBytePtr);
                var sets = new AddItemSettings();
                sets.DropRemainder = true;
                sets.EquipIfPossible = true;
                var hasAdded = InventoryUtilitiesServer.TryAddItem(sets,ctx.Event.SenderCharacterEntity, guid ,amount);
            }*/

            var gameData = Plugin.Server.GetExistingSystem<GameDataSystem>();
            var itemSettings = AddItemSettings.Create(Plugin.Server.EntityManager, gameData.ItemHashLookupMap);
            var inventoryResponse = InventoryUtilitiesServer.TryAddItem(itemSettings, ctx.Event.SenderCharacterEntity, guid, amount);
            //return inventoryResponse.NewEntity;
        }

        public static BloodType GetBloodTypeFromName(string name)
        {
            BloodType type = BloodType.Frailed;
            if (Enum.IsDefined(typeof(BloodType), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name)))
                Enum.TryParse(name, true, out type);
            return type;
        }

        public static PrefabGUID GetSourceTypeFromName(string name)
        {
            PrefabGUID type;
            name = name.ToLower();
            if (name.Equals("brute")) type = new PrefabGUID(-1464869978);
            else if (name.Equals("warrior")) type = new PrefabGUID(-1128238456);
            else if (name.Equals("rogue")) type = new PrefabGUID(-1030822544);
            else if (name.Equals("scholar")) type = new PrefabGUID(-700632469);
            else if (name.Equals("creature")) type = new PrefabGUID(1897056612);
            else if (name.Equals("worker")) type = new PrefabGUID(-1342764880);
            else if (name.Equals("mutant")) type = new PrefabGUID(-2017994753);
            else type = new PrefabGUID();
            return type;
        }

        public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }

            //-- Way of the Query
            //foreach (var UsersEntity in entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp))
            //{
            //    var target_component = entityManager.GetComponentData<User>(UsersEntity);
            //    if (mustOnline)
            //    {
            //        if (!target_component.IsConnected) continue;
            //    }


            //    string CharName = target_component.CharacterName.ToString();
            //    if (CharName.Equals(name))
            //    {
            //        userEntity = UsersEntity;
            //        playerEntity = target_component.LocalCharacter._Entity;
            //        return true;
            //    }
            //}
            //playerEntity = empty_entity;
            //userEntity = empty_entity;
            //return false;
        }
        public static bool FindPlayer(ulong steamid, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.SteamPlayerCache.TryGetValue(steamid, out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }
        }

        public static bool IsPlayerInCombat(Entity player)
        {
            return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, Database.Buff.InCombat) || BuffUtility.HasBuff(Plugin.Server.EntityManager, player, Database.Buff.InCombat_PvP);
        }

        public static bool HasBuff(Entity player, PrefabGUID BuffGUID)
        {
            return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, BuffGUID);
        }

        public static void SetPvPShield(Entity character, bool value)
        {
            var em = Plugin.Server.EntityManager;
            var cUnitStats = em.GetComponentData<UnitStats>(character);
            var cBuffer = em.GetBuffer<BoolModificationBuffer>(character);
            cUnitStats.PvPProtected.SetBaseValue(value, cBuffer);
            em.SetComponentData(character, cUnitStats);
        }

        public static bool SpawnNPCIdentify(out float identifier, string name, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
        {
            identifier = 0f;
            float default_duration = 5.0f;
            float duration_final;
            var isFound = Enum.TryParse(name, true, out Prefabs.Units unit);
            if (!isFound) return false;

            float UniqueID = (float)rand.NextDouble();
            if (UniqueID == 0.0) UniqueID += 0.00001f;
            else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
            duration_final = default_duration + UniqueID;
            
            while (Cache.spawnNPC_Listen.ContainsKey(duration))
            {
                UniqueID = (float)rand.NextDouble();
                if (UniqueID == 0.0) UniqueID += 0.00001f;
                else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
                duration_final = default_duration + UniqueID;
            }

            UnitSpawnerReactSystem_Patch.listen = true;
            identifier = duration_final;
            var Data = new SpawnNPCListen(duration, default, default, default, false);
            Cache.spawnNPC_Listen.Add(duration_final, Data);

            Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, new PrefabGUID((int)unit), position, 1, minRange, maxRange, duration_final);
            return true;
        }

        public static bool SpawnAtPosition(Entity user, Prefabs.Units unit, int count, float3 position, float minRange = 1, float maxRange = 2, float duration = -1) {
            var guid = new PrefabGUID((int)unit);

            //var translation = Plugin.Server.EntityManager.GetComponentData<Translation>(user);
            //var f3pos = new float3(position.x, translation.Value.y, position.y);
            try
            {
                Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, guid, position, count, minRange, maxRange, duration);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static PrefabGUID GetPrefabGUID(Entity entity)
        {
            var entityManager = Plugin.Server.EntityManager;
            PrefabGUID guid;
            try
            {
                guid = entityManager.GetComponentData<PrefabGUID>(entity);
            }
            catch
            {
                guid.GuidHash = 0;
            }
            return guid;
        }

        public static string GetPrefabName(PrefabGUID hashCode)
        {
            var s = Plugin.Server.GetExistingSystem<PrefabCollectionSystem>();
            string name = "Nonexistent";
            if (hashCode.GuidHash == 0)
            {
                return name;
            }
            try
            {
                name = s.PrefabGuidToNameDictionary[hashCode];
            }
            catch
            {
                name = "NoPrefabName";
            }
            return name;
        }
        
        public static Prefabs.Faction ConvertGuidToFaction(PrefabGUID guid) {
            if (Enum.IsDefined(typeof(Prefabs.Faction), guid.GetHashCode())) return (Prefabs.Faction)guid.GetHashCode();
            return Prefabs.Faction.Unknown;
        }

        /*
        public static void TeleportTo(Context ctx, float3 position)
        {
            var entity = ctx.EntityManager.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
                );

            ctx.EntityManager.SetComponentData<FromCharacter>(entity, new()
            {
                User = ctx.Event.SenderUserEntity,
                Character = ctx.Event.SenderCharacterEntity
            });

            ctx.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
            {
                Position = new float3(position.x, position.y, position.z),
                Target = PlayerTeleportDebugEvent.TeleportTarget.Self
            });
        }*/
        /*
        struct FakeNull
        {
            public int value;
            public bool has_value;
        }*/

        public enum BloodType {
            Frailed = -899826404,
            Creature = -77658840,
            Warrior = -1094467405,
            Rogue = 793735874,
            Brute = 581377887,
            Scholar = -586506765,
            Worker = -540707191,
            Mutant = -2017994753,
        }
        
        public static PrefabGUID vBloodType = new(1557174542);


        // For stats that reduce as a multiplier of 1 - their value, so that a value of 0.5 halves the stat, and 0.75 quarters it.
        // I do this so that we can compute linear increases to a formula of X/(X+Y) where Y is the amount for +100% effectivness and X is the stat value
        public static HashSet<int> inverseMultiplierStats = new HashSet<int> {
            {(int)UnitStatType.CooldownModifier },
            {(int)UnitStatType.PrimaryCooldownModifier },
            {(int)UnitStatType.PhysicalResistance },
            {(int)UnitStatType.SpellResistance },
            {(int)UnitStatType.ResistVsBeasts },
            {(int)UnitStatType.ResistVsCastleObjects },
            {(int)UnitStatType.ResistVsDemons },
            {(int)UnitStatType.ResistVsHumans },
            {(int)UnitStatType.ResistVsMechanical },
            {(int)UnitStatType.ResistVsPlayerVampires },
            {(int)UnitStatType.ResistVsUndeads },
            {(int)UnitStatType.BloodDrain },
            {(int)UnitStatType.ReducedResourceDurabilityLoss }
        };

        //
        public static HashSet<int> percentageStats = new HashSet<int> {
            {(int)UnitStatType.PhysicalCriticalStrikeChance },
            {(int)UnitStatType.SpellCriticalStrikeChance },
            {(int)UnitStatType.PhysicalCriticalStrikeDamage },
            {(int)UnitStatType.SpellCriticalStrikeDamage },
            {(int)UnitStatType.PhysicalLifeLeech },
            {(int)UnitStatType.PrimaryLifeLeech },
            {(int)UnitStatType.SpellLifeLeech },
            {(int)UnitStatType.AttackSpeed },
            {(int)UnitStatType.PrimaryAttackSpeed },
            {(int)UnitStatType.PassiveHealthRegen},
            {(int)UnitStatType.ResourceYield }

        };

        //This should be a dictionary lookup for the stats to what mod type they should use, and i should put the name strings in here, i might do it later.
        public static HashSet<int> multiplierStats = new HashSet<int> {
            {(int)UnitStatType.CooldownModifier },
            {(int)UnitStatType.PrimaryCooldownModifier },/*
            {(int)UnitStatType.PhysicalResistance },
            {(int)UnitStatType.SpellResistance },
            {(int)UnitStatType.ResistVsBeasts },
            {(int)UnitStatType.ResistVsCastleObjects },
            {(int)UnitStatType.ResistVsDemons },
            {(int)UnitStatType.ResistVsHumans },
            {(int)UnitStatType.ResistVsMechanical },
            {(int)UnitStatType.ResistVsPlayerVampires },
            {(int)UnitStatType.ResistVsUndeads },
            {(int)UnitStatType.ReducedResourceDurabilityLoss },
            {(int)UnitStatType.BloodDrain },*/
            {(int)UnitStatType.ResourceYield }

        };

        public static HashSet<int> baseStatsSet = new HashSet<int> {
            {(int)UnitStatType.PhysicalPower },
            {(int)UnitStatType.ResourcePower },
            {(int)UnitStatType.SiegePower },
            {(int)UnitStatType.AttackSpeed },
            {(int)UnitStatType.FireResistance },
            {(int)UnitStatType.GarlicResistance },
            {(int)UnitStatType.SilverResistance },
            {(int)UnitStatType.HolyResistance },
            {(int)UnitStatType.SunResistance },
            {(int)UnitStatType.SpellResistance },
            {(int)UnitStatType.PhysicalResistance },
            {(int)UnitStatType.SpellCriticalStrikeDamage },
            {(int)UnitStatType.SpellCriticalStrikeChance },
            {(int)UnitStatType.PhysicalCriticalStrikeDamage },
            {(int)UnitStatType.PhysicalCriticalStrikeChance },
            {(int)UnitStatType.PassiveHealthRegen },
            {(int)UnitStatType.ResourceYield },
            {(int)UnitStatType.PvPResilience },
            {(int)UnitStatType.ReducedResourceDurabilityLoss }

        };

        public static void confirmFile (string address, string file) {
            try {
                Directory.CreateDirectory(address);
            }
            catch (Exception e) {
                Plugin.Logger.LogWarning(DateTime.Now +": Error creating directory at " + address + "\n Error is: " + e.Message);
            }
            try {
                if (!File.Exists(address + file)) {
                    FileStream stream = File.Create(address + file);
                    stream.Dispose();
                }
            } catch (Exception e) {
                Plugin.Logger.LogWarning(DateTime.Now + ": Error creating file at " + address + "\n Error is: " + e.Message);
            }
        }
        public static string statTypeToString(UnitStatType type) {
            var name = Enum.GetName(type);
            // Split words by camel case
            // ie, PhysicalPower => "Physical Power"
            return Regex.Replace(name, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }
    }
}
