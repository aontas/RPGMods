﻿using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Entities;
using XPRising.Models;
using XPRising.Utils;

namespace XPRising.Systems
{
    using WeaponMasteryData = LazyDictionary<WeaponMasterySystem.MasteryType,MasteryData>;
    public static class WeaponMasterySystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static int MasteryCombatTick = 5;
        public static int MaxCombatTick = 12;
        public static double MasteryGainMultiplier = 0.1;
        public static double InactiveMultiplier = 0.1;
        public static double VBloodMultiplier = 15;
        
        public static bool IsDecaySystemEnabled = true;
        public static int DecayInterval = 60;
        public static double OnlineDecayValue = 0;
        public static double OfflineDecayValue = 1;

        public static double MaxEffectiveness = 10;
        public static bool EffectivenessSubSystemEnabled = false;
        public static double GrowthPerEffectiveness = -1;

        private static readonly Random Rand = new Random();
        
        public enum MasteryType
        {
            Unarmed,
            Spear,
            Sword,
            Scythe,
            Crossbow,
            Mace,
            Slasher,
            Axe,
            FishingPole,
            Rapier,
            Pistol,
            GreatSword,
            LongBow,
            Whip,
            Spell
        }
        
        // This is a "potential" name to mastery map. Multiple keywords map to the same mastery
        public static Dictionary<string, MasteryType> KeywordToMasteryMap = new()
        {
            { "spell", MasteryType.Spell },
            { "magic", MasteryType.Spell },
            { "unarmed", MasteryType.Unarmed },
            { "none", MasteryType.Unarmed },
            { "spear", MasteryType.Spear },
            { "crossbow", MasteryType.Crossbow },
            { "slashers", MasteryType.Slasher },
            { "slasher", MasteryType.Slasher },
            { "scythe", MasteryType.Scythe },
            { "reaper", MasteryType.Scythe },
            { "sword", MasteryType.Sword },
            { "fishingpole", MasteryType.FishingPole },
            { "mace", MasteryType.Mace },
            { "axe", MasteryType.Axe },
            { "greatsword", MasteryType.GreatSword },
            { "rapier", MasteryType.Rapier },
            { "pistol", MasteryType.Pistol },
            { "dagger", MasteryType.Sword },
            { "longbow", MasteryType.LongBow },
            { "xbow", MasteryType.Crossbow },
            { "whip", MasteryType.Whip }
        };

        public static void UpdateMastery(ulong steamID, MasteryType masteryType, double victimPower, bool isVBlood)
        {
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Updating weapon mastery for {steamID}");
            double masteryValue = victimPower;
            
            var wd = Database.PlayerWeaponmastery[steamID];

            var weaponMastery = wd[masteryType];
            var spellMastery = wd[MasteryType.Spell];

            var vBloodMultiplier = isVBlood ? VBloodMultiplier : 1;
            var changeInMastery = weaponMastery.CalculateBaseMasteryGrowth(masteryValue, Rand) * vBloodMultiplier;
            
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery change {Enum.GetName(masteryType)}: [{masteryValue},{changeInMastery}]");
            wd = ModMastery(steamID, wd, masteryType, changeInMastery);

            Database.PlayerWeaponmastery[steamID] = wd;

            if (Database.PlayerLogConfig[steamID].LoggingMastery)
            {
                var currentMastery = wd[masteryType].Mastery;
                var message =
                    LocalisationSystem.Get(LocalisationSystem.TemplateKey.WeaponMasteryGain)
                        .AddField("{masteryChange}", $"{changeInMastery:F3}")
                        .AddField("{masteryType}", $"{Enum.GetName(masteryType)}")
                        .AddField("{currentMastery}", $"{currentMastery:F2}");
                Output.SendMessage(steamID, message);
            }
        }

        public static void UpdateMastery(Entity killer, Entity victim)
        {
            if (killer == victim) return;
            if (_em.HasComponent<Minion>(victim)) return;
            
            Entity userEntity = _em.GetComponentData<PlayerCharacter>(killer).UserEntity;
            ulong steamID = _em.GetComponentData<User>(userEntity).PlatformId;
            
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Updating weapon mastery for {steamID}");
            
            var masteryType = WeaponToMasteryType(GetWeaponType(killer));
            
            var victimStats = _em.GetComponentData<UnitStats>(victim);
            double masteryValue = victimStats.PhysicalPower.Value;
            double spellMasteryValue = victimStats.SpellPower.Value;
            
            var wd = Database.PlayerWeaponmastery[steamID];

            var weaponMastery = wd[masteryType];
            var spellMastery = wd[MasteryType.Spell];
            
            var isVBlood = _em.TryGetComponentData<BloodConsumeSource>(victim, out var victimBlood) && Helper.IsVBlood(victimBlood);
            
            if (_em.HasComponent<PlayerCharacter>(victim) && _em.TryGetComponentData<Equipment>(victim, out var victimGear))
            {
                var bonusMastery = victimGear.ArmorLevel + victimGear.WeaponLevel + victimGear.SpellLevel;
                masteryValue *= (1 + (bonusMastery * 0.01));
                spellMasteryValue *= (1 + (bonusMastery * 0.01));
                Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Bonus mastery {bonusMastery:F3}]");
            }

            var vBloodMultiplier = isVBlood ? VBloodMultiplier : 1;
            var changeInMastery = weaponMastery.CalculateBaseMasteryGrowth(masteryValue, Rand) * vBloodMultiplier;
            var changeInSpellMastery = spellMastery.CalculateBaseMasteryGrowth(spellMasteryValue, Rand) * vBloodMultiplier;
            
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery change {Enum.GetName(masteryType)}: [{masteryValue},{changeInMastery}]");
            wd = ModMastery(steamID, wd, masteryType, changeInMastery);

            Database.PlayerWeaponmastery[steamID] = wd;

            if (Database.PlayerLogConfig[steamID].LoggingMastery)
            {
                var currentMastery = wd[masteryType].Mastery;
                
                var message =
                    LocalisationSystem.Get(LocalisationSystem.TemplateKey.WeaponMasteryGain)
                        .AddField("{masteryChange}", $"{changeInMastery:F3}")
                        .AddField("{masteryType}", $"{Enum.GetName(masteryType)}")
                        .AddField("{currentMastery}", $"{currentMastery:F2}");
                Output.SendMessage(steamID, message);
            }
        }

        // TODO Currently not used.
        // Keeping (for now) to test whether the above UpdateMastery (by hit) is OK, or if we want to use the growth model like below.
        public static void LoopMastery(Entity user, Entity player)
        {
            var steamID = _em.GetComponentData<User>(user).PlatformId;

            var lastCombat = Cache.player_last_combat[steamID];
            var combatTicks = Cache.player_combat_ticks[steamID];
            var elapsedTime = DateTime.Now - lastCombat;
            if (elapsedTime.TotalSeconds >= 10) combatTicks = 0;
            if (elapsedTime.TotalSeconds * 0.2 < 1) return;

            Cache.player_last_combat[steamID] = DateTime.Now;

            if (combatTicks > MaxCombatTick) return;
            var masteryType = WeaponToMasteryType(GetWeaponType(player));
            
            var wd = Database.PlayerWeaponmastery[steamID];
            
            var weaponGrowth = wd[masteryType].Growth;
            var spellGrowth = wd[MasteryType.Spell].Growth;

            var changeInMastery = (MasteryCombatTick * weaponGrowth)/1000.0;
            var changeInSpellMastery = (MasteryCombatTick * spellGrowth)/1000.0;
            Cache.player_combat_ticks[steamID] = combatTicks + 1;
            
            wd = ModMastery(steamID, wd, masteryType, changeInMastery);

            Database.PlayerWeaponmastery[steamID] = wd;
        }

        public static void DecayMastery(Entity userEntity, DateTime lastDecay)
        {
            var steamID = _em.GetComponentData<User>(userEntity).PlatformId;
            var elapsedTime = DateTime.Now - lastDecay;
            if (elapsedTime.TotalSeconds < DecayInterval) return;

            var decayTicks = (int)Math.Floor(elapsedTime.TotalSeconds / DecayInterval);
            if (decayTicks > 0)
            {
                var decayValue = OfflineDecayValue * decayTicks * -1;

                var message =
                    LocalisationSystem.Get(LocalisationSystem.TemplateKey.WeaponMasteryDecay)
                        .AddField("{duration}", $"{elapsedTime.TotalMinutes}")
                        .AddField("{decay}", $"{decayValue * 0.001:F3)}");
                Output.SendMessage(steamID, message);
                
                var wd = Database.PlayerWeaponmastery[steamID];

                foreach (var type in Enum.GetValues<MasteryType>())
                {
                    wd = ModMastery(steamID, wd, type, decayValue);
                }

                Database.PlayerWeaponmastery[steamID] = wd;
            }
        }

        public static void BuffReceiver(ref LazyDictionary<UnitStatType, float> statBonus, Entity owner, ulong steamID)
        {
            var masteryType = WeaponToMasteryType(GetWeaponType(owner));
            var weaponMasterData = Database.PlayerWeaponmastery[steamID];

            foreach (var (type, mastery) in weaponMasterData)
            {
                // Currently equipped weapon and spell masteries are always applied.
                var multiplier = type == masteryType || type == MasteryType.Spell ? 1.0 : InactiveMultiplier;
                var effectiveness = (EffectivenessSubSystemEnabled ? mastery.Effectiveness : 1) * multiplier;
                if (effectiveness > 0)
                {
                    var masteryValue = Math.Max(mastery.Mastery, 0);
                    var config = Database.MasteryStatConfig[type];
                    foreach (var statConfig in config)
                    {
                        statBonus[statConfig.type] += (float)Helper.CalcBuffValue(masteryValue, effectiveness,
                            statConfig.rate, statConfig.type);
                    }
                }
            }
        }

        public static void ModMastery(ulong steamID, MasteryType type, double changeInMastery)
        {
            var wd = Database.PlayerWeaponmastery[steamID];
            wd = ModMastery(steamID, wd, type, changeInMastery);
            Database.PlayerWeaponmastery[steamID] = wd;
        }

        private static WeaponMasteryData ModMastery(ulong steamID, WeaponMasteryData wd, MasteryType type, double changeInMastery)
        {
            if (type == MasteryType.Unarmed){
                if (changeInMastery > 0) changeInMastery *= 2;
            }

            var mastery = wd[type];
            mastery.Mastery += changeInMastery * MasteryGainMultiplier;
            Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery changed: {steamID}: {Enum.GetName(type)}: {mastery.Mastery}");
            wd[type] = mastery;
            return wd;
        }

        public static void ResetMastery(ulong steamID, MasteryType type) {
            if (!EffectivenessSubSystemEnabled) {
                Output.SendMessage(steamID, LocalisationSystem.Get(LocalisationSystem.TemplateKey.EffectivenessDisabled).AddField("{system}", "mastery"));
                return;
            }
            if (Database.PlayerWeaponmastery.TryGetValue(steamID, out var wd))
            {
                var mastery = wd[type];
                // If it is already 0, then this won't have much effect.
                if (mastery.Mastery > 0)
                {
                    wd[type] = mastery.ResetMastery(MaxEffectiveness, GrowthPerEffectiveness);
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Info, $"Mastery reset: {Enum.GetName(type)}: {mastery}");
                }
                Database.PlayerWeaponmastery[steamID] = wd;
            }
        }

        public static WeaponType GetWeaponType(Entity player)
        {
            var weaponEntity = _em.GetComponentData<Equipment>(player).WeaponSlot.SlotEntity._Entity;
            var weaponType = WeaponType.None;
            if (_em.HasComponent<EquippableData>(weaponEntity))
            {
                var weaponData = _em.GetComponentData<EquippableData>(weaponEntity);
                weaponType = weaponData.WeaponType;
            }
            return weaponType;
        }
        
        public static MasteryType WeaponToMasteryType(WeaponType weapon)
        {
            // Note: we are not just simply casting the int value of weapon to a MasteryType to help ensure forwards compatibility.
            switch (weapon)
            {
                case WeaponType.None:
                    return MasteryType.Unarmed;
                case WeaponType.Spear:
                    return MasteryType.Spear;
                case WeaponType.Sword:
                    return MasteryType.Sword;
                case WeaponType.Scythe:
                    return MasteryType.Scythe;
                case WeaponType.Crossbow:
                    return MasteryType.Crossbow;
                case WeaponType.Mace:
                    return MasteryType.Mace;
                case WeaponType.Slashers:
                    return MasteryType.Slasher;
                case WeaponType.Axes:
                    return MasteryType.Axe;
                case WeaponType.FishingPole:
                    return MasteryType.FishingPole;
                case WeaponType.Rapier:
                    return MasteryType.Rapier;
                case WeaponType.Pistols:
                    return MasteryType.Pistol;
                case WeaponType.GreatSword:
                    return MasteryType.GreatSword;
                case WeaponType.Longbow:
                    return MasteryType.LongBow;
                case WeaponType.Whip:
                    return MasteryType.Whip;
                default:
                    Plugin.Log(Plugin.LogSystem.Mastery, LogLevel.Error, $"Cannot convert new weapon to mastery: {Enum.GetName(weapon)}. Defaulting to Spell.");
                    return MasteryType.Spell;
            }
        }

        public static LazyDictionary<MasteryType, List<StatConfig>> DefaultMasteryConfig()
        {
            return new LazyDictionary<MasteryType, List<StatConfig>>
            {
                { MasteryType.Unarmed, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0, 0.25f), new(UnitStatType.MovementSpeed, 0, 0.01f) } },
                { MasteryType.Spear, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0, 0.25f) } },
                { MasteryType.Sword, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0,  0.125f ), new(UnitStatType.SpellPower, 0,  0.125f ) } },
                { MasteryType.Scythe, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0,  0.125f ), new(UnitStatType.PhysicalCriticalStrikeChance, 0,  0.00125f ) } },
                { MasteryType.Crossbow, new List<StatConfig>() { new(UnitStatType.PhysicalCriticalStrikeChance, 0,  0.0025f ) } },
                { MasteryType.Mace, new List<StatConfig>() { new(UnitStatType.MaxHealth, 0,  1f ) } },
                { MasteryType.Slasher, new List<StatConfig>() { new(UnitStatType.PhysicalCriticalStrikeChance, 0,  0.00125f ), new(UnitStatType.MovementSpeed, 0,  0.005f ) } },
                { MasteryType.Axe, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0,  0.125f ), new(UnitStatType.MaxHealth, 0,  0.5f ) } },
                { MasteryType.FishingPole, new List<StatConfig>() },
                { MasteryType.Rapier, new List<StatConfig>() { new(UnitStatType.PhysicalCriticalStrikeChance, 0,  0.00125f ), new(UnitStatType.PhysicalCriticalStrikeDamage, 0,  0.00125f ) } },
                { MasteryType.Pistol, new List<StatConfig>() { new(UnitStatType.PhysicalCriticalStrikeChance, 0,  0.00125f ), new(UnitStatType.PhysicalCriticalStrikeDamage, 0,  0.00125f ) } },
                { MasteryType.GreatSword, new List<StatConfig>() { new(UnitStatType.PhysicalPower, 0,  0.125f ), new(UnitStatType.PhysicalCriticalStrikeDamage, 0,  0.00125f ) } },
                { MasteryType.Spell, new List<StatConfig>() { new(UnitStatType.SpellCooldownRecoveryRate, 0,  0.01f )} }
            };
        }
    }
}
