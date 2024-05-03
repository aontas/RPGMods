using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using VampireCommandFramework;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using OpenRPG.Commands;
using OpenRPG.Systems;
using OpenRPG.Utils;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using VRising.GameData;
using OpenRPG.Configuration;
using OpenRPG.Components.RandomEncounters;
using ProjectM;

namespace OpenRPG
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin
    {
        public static Harmony harmony;

        internal static Plugin Instance { get; private set; }

        private static ConfigEntry<int> WaypointLimit;

        private static ConfigEntry<bool> EnableVIPSystem;
        private static ConfigEntry<bool> EnableVIPWhitelist;
        private static ConfigEntry<int> VIP_Permission;
        private static ConfigEntry<bool> ShouldAllowGearLevel;
        private static ConfigEntry<bool> EnableLevelRewards;
        private static ConfigEntry<bool> EasyLevel15;

        private static ConfigEntry<double> VIP_InCombat_ResYield;
        private static ConfigEntry<double> VIP_InCombat_DurabilityLoss;
        private static ConfigEntry<double> VIP_InCombat_MoveSpeed;
        private static ConfigEntry<double> VIP_InCombat_GarlicResistance;
        private static ConfigEntry<double> VIP_InCombat_SilverResistance;

        private static ConfigEntry<double> VIP_OutCombat_ResYield;
        private static ConfigEntry<double> VIP_OutCombat_DurabilityLoss;
        private static ConfigEntry<double> VIP_OutCombat_MoveSpeed;
        private static ConfigEntry<double> VIP_OutCombat_GarlicResistance;
        private static ConfigEntry<double> VIP_OutCombat_SilverResistance;
        
        private static ConfigEntry<bool> HunterHuntedEnabled;
        private static ConfigEntry<int> HeatCooldown;
        private static ConfigEntry<int> Ambush_Interval;
        private static ConfigEntry<int> Ambush_Chance;
        private static ConfigEntry<float> Ambush_Despawn_Unit_Timer;
        private static ConfigEntry<int> VBloodHeatMultiplier;

        private static ConfigEntry<bool> EnableExperienceSystem;
        private static ConfigEntry<int> MaxLevel;
        private static ConfigEntry<float> EXPMultiplier;
        private static ConfigEntry<float> VBloodEXPMultiplier;
        private static ConfigEntry<float> EXPFormula_1;
        private static ConfigEntry<double> EXPGroupModifier;
        private static ConfigEntry<float> EXPGroupMaxDistance;
        private static ConfigEntry<int> EXPGroupLevelScheme;

        private static ConfigEntry<float> pvpXPLoss;
        private static ConfigEntry<float> pvpXPLossPerLevel;
        private static ConfigEntry<float> pvpXPLossPercent;
        private static ConfigEntry<float> pvpXPLossPercentPerLevel;
        private static ConfigEntry<float> pvpXPLossMultPerLvlDiff;
        private static ConfigEntry<float> pvpXPLossMultPerLvlDiffSq;
        private static ConfigEntry<float> pveXPLoss;
        private static ConfigEntry<float> pveXPLossPerLevel;
        private static ConfigEntry<float> pveXPLossPercent;
        private static ConfigEntry<float> pveXPLossPercentPerLevel;
        private static ConfigEntry<float> pveXPLossMultPerLvlDiff;
        private static ConfigEntry<float> pveXPLossMultPerLvlDiffSq;

        private static ConfigEntry<bool> xpLossOnDown;
        private static ConfigEntry<bool> xpLossOnRelease;

        private static ConfigEntry<bool> EnableWeaponMaster;
        private static ConfigEntry<bool> EnableWeaponMasterDecay;
        private static ConfigEntry<double> WeaponMasterMultiplier;
        private static ConfigEntry<int> WeaponDecayInterval;
        private static ConfigEntry<double> WeaponMaxMastery;
        private static ConfigEntry<double> WeaponMastery_VBloodMultiplier;
        private static ConfigEntry<int> Offline_Weapon_MasteryDecayValue;
        private static ConfigEntry<int> MasteryCombatTick;
        private static ConfigEntry<int> MasteryMaxCombatTicks;
        private static ConfigEntry<bool> WeaponMasterySpellMasteryNeedsNoneToUse;
        private static ConfigEntry<bool> WeaponMasterySpellMasteryNeedsNoneToLearn;
        private static ConfigEntry<bool> WeaponLinearSpellMastery;
        private static ConfigEntry<bool> WeaponSpellMasteryCDRStacks;
        private static ConfigEntry<string> UnarmedStats;
        private static ConfigEntry<string> UnarmedRates;
        private static ConfigEntry<string> SpearStats;
        private static ConfigEntry<string> SpearRates;
        private static ConfigEntry<string> SwordStats;
        private static ConfigEntry<string> SwordRates;
        private static ConfigEntry<string> ScytheStats;
        private static ConfigEntry<string> ScytheRates;
        private static ConfigEntry<string> CrossbowStats;
        private static ConfigEntry<string> CrossbowRates;
        private static ConfigEntry<string> MaceStats;
        private static ConfigEntry<string> MaceRates;
        private static ConfigEntry<string> SlasherStats;
        private static ConfigEntry<string> SlasherRates;
        private static ConfigEntry<string> AxeStats;
        private static ConfigEntry<string> AxeRates;
        private static ConfigEntry<string> FishingPoleStats;
        private static ConfigEntry<string> FishingPoleRates;
        private static ConfigEntry<string> SpellStats;
        private static ConfigEntry<string> SpellRates;
        private static ConfigEntry<string> RapierStats;
        private static ConfigEntry<string> RapierRates;
        private static ConfigEntry<string> PistolStats;
        private static ConfigEntry<string> PistolRates;
        private static ConfigEntry<string> GreatswordStats;
        private static ConfigEntry<string> GreatswordRates;
        
        private static ConfigEntry<bool> effectivenessSubSystemEnabled;
        private static ConfigEntry<bool> growthSubSystemEnabled;
        private static ConfigEntry<float> maxEffectiveness;
        private static ConfigEntry<float> minGrowth;
        private static ConfigEntry<float> maxGrowth;
        private static ConfigEntry<float> growthPerEfficency;
        
        private static ConfigEntry<string> draculaBloodlineStats;
        private static ConfigEntry<string> draculaBloodlineMinStrengths;
        private static ConfigEntry<string> draculaBloodlineRates;
        private static ConfigEntry<string> arwenBloodlineStats;
        private static ConfigEntry<string> arwenBloodlineMinStrengths;
        private static ConfigEntry<string> arwenBloodlineRates;
        private static ConfigEntry<string> ilvrisBloodlineStats;
        private static ConfigEntry<string> ilvrisBloodlineMinStrengths;
        private static ConfigEntry<string> ilvrisBloodlineRates;
        private static ConfigEntry<string> ayaBloodlineStats;
        private static ConfigEntry<string> ayaBloodlineMinStrengths;
        private static ConfigEntry<string> ayaBloodlineRates;
        private static ConfigEntry<string> nytheriaBloodlineStats;
        private static ConfigEntry<string> nytheriaBloodlineMinStrengths;
        private static ConfigEntry<string> nytheriaBloodlineRates;
        private static ConfigEntry<string> hadubertBloodlineStats;
        private static ConfigEntry<string> hadubertBloodlineMinStrengths;
        private static ConfigEntry<string> hadubertBloodlineRates;
        private static ConfigEntry<string> reiBloodlineStats;
        private static ConfigEntry<string> reiBloodlineMinStrengths;
        private static ConfigEntry<string> reiBloodlineRates;
        private static ConfigEntry<string> semikaBloodlineStats;
        private static ConfigEntry<string> semikaBloodlineMinStrengths;
        private static ConfigEntry<string> semikaBloodlineRates;

        private static ConfigEntry<bool> bloodlinesEnabled;
        private static ConfigEntry<bool> mercilessBloodlines;
        private static ConfigEntry<bool>  draculaGetsAll;
        private static ConfigEntry<double> bloodlineGrowthMultiplier;
        private static ConfigEntry<double> bloodlineVBloodMultiplier;
        private static ConfigEntry<bool> bloodlineEfficencySubSystem;
        private static ConfigEntry<bool> bloodlineGrowthSubsystem;
        private static ConfigEntry<double> MaxBloodlineStrength;
        private static ConfigEntry<double> maxBloodlineEfficency;
        private static ConfigEntry<double> maxBloodlineGrowth;
        private static ConfigEntry<double> minBloodlineGrowth;
        private static ConfigEntry<double> bloodlineGrowthPerEfficency;
        private static ConfigEntry<string> bloodlineNames;

        private static ConfigEntry<bool> EnableWorldDynamics;
        private static ConfigEntry<bool> WDGrowOnKill;

        private static ConfigEntry<int> buffID;
        private static ConfigEntry<int> forbiddenBuffID;
        private static ConfigEntry<int> appliedBuff;
        private static ConfigEntry<bool> humanReadablePercentageStats;
        private static ConfigEntry<bool> inverseMultiplersDisplayReduction;
        private static ConfigEntry<bool> disableCommandAdminRequirement;

        public static bool isInitialized = false;

        private static ManualLogSource Logger;
        private static World _serverWorld;
        public static World Server
        {
            get
            {
                if (_serverWorld != null) return _serverWorld;

                _serverWorld = GetWorld("Server")
                    ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
                return _serverWorld;
            }
        }

        public static bool IsServer => Application.productName == "VRisingServer";

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }

        public void InitConfig()
        {
            WaypointLimit = Config.Bind("Config", "Waypoint Limit", 2, "Set a waypoint limit for per non-admin user.");

            EnableVIPSystem = Config.Bind("VIP", "Enable VIP System", false, "Enable the VIP System.");
            EnableVIPWhitelist = Config.Bind("VIP", "Enable VIP Whitelist", false, "Enable the VIP user to ignore server capacity limit.");
            VIP_Permission = Config.Bind("VIP", "Minimum VIP Permission", 50, "The minimum permission level required for the user to be considered as VIP.");

            VIP_InCombat_DurabilityLoss = Config.Bind("VIP.InCombat", "Durability Loss Multiplier", 0.5, "Multiply durability loss when user is in combat. -1.0 to disable.\n" +
                "Does not affect durability loss on death.");
            VIP_InCombat_GarlicResistance = Config.Bind("VIP.InCombat", "Garlic Resistance Multiplier", -1.0, "Multiply garlic resistance when user is in combat. -1.0 to disable.");
            VIP_InCombat_SilverResistance = Config.Bind("VIP.InCombat", "Silver Resistance Multiplier", -1.0, "Multiply silver resistance when user is in combat. -1.0 to disable.");
            VIP_InCombat_MoveSpeed = Config.Bind("VIP.InCombat", "Move Speed Multiplier", -1.0, "Multiply move speed when user is in combat. -1.0 to disable.");
            VIP_InCombat_ResYield = Config.Bind("VIP.InCombat", "Resource Yield Multiplier", 2.0, "Multiply resource yield (not item drop) when user is in combat. -1.0 to disable.");

            VIP_OutCombat_DurabilityLoss = Config.Bind("VIP.OutCombat", "Durability Loss Multiplier", 0.5, "Multiply durability loss when user is out of combat. -1.0 to disable.\n" +
                "Does not affect durability loss on death.");
            VIP_OutCombat_GarlicResistance = Config.Bind("VIP.OutCombat", "Garlic Resistance Multiplier", 2.0, "Multiply garlic resistance when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_SilverResistance = Config.Bind("VIP.OutCombat", "Silver Resistance Multiplier", 2.0, "Multiply silver resistance when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_MoveSpeed = Config.Bind("VIP.OutCombat", "Move Speed Multiplier", 1.25, "Multiply move speed when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_ResYield = Config.Bind("VIP.OutCombat", "Resource Yield Multiplier", 2.0, "Multiply resource yield (not item drop) when user is out of combat. -1.0 to disable.");

            HunterHuntedEnabled = Config.Bind("HunterHunted", "Enable", true, "Enable/disable the HunterHunted system.");
            HeatCooldown = Config.Bind("HunterHunted", "Heat Cooldown", 10, "Set the reduction value for player heat per minute.");
            Ambush_Interval = Config.Bind("HunterHunted", "Ambush Interval", 60, "Set how many seconds player can be ambushed again since last ambush.");
            Ambush_Chance = Config.Bind("HunterHunted", "Ambush Chance", 50, "Set the percentage that an ambush may occur for every cooldown interval.");
            Ambush_Despawn_Unit_Timer = Config.Bind("HunterHunted", "Ambush Despawn Timer", 300f, "Despawn the ambush squad after this many second if they are still alive.\n" +
                "Must be higher than 1.");
            VBloodHeatMultiplier = Config.Bind("HunterHunted", "VBlood Heat Multiplier", 20, "Multiply the heat generated by VBlood kills.");

            EnableExperienceSystem = Config.Bind("Experience", "Enable", true, "Enable/disable the the Experience System.");
            ShouldAllowGearLevel = Config.Bind("Experience", "Allow Gear Level", false, "Enable/disable gear level adjustment.");
            EnableLevelRewards = Config.Bind("Experience", "Enable Level Rewards", false, "Enable rewards per level.");
            EasyLevel15 = Config.Bind("Experience", "Easy lvl 15", true, "Makes level 15 much easier to reach so players dont get held up by the quest on it.");

            MaxLevel = Config.Bind("Experience", "Max Level", 80, "Configure the experience system max level.");
            EXPMultiplier = Config.Bind("Experience", "Multiplier", 1.0f, "Multiply the EXP gained by player.\n" +
                "Ex.: 0.7f -> Will reduce the EXP gained by 30%\nFormula: UnitKilledLevel * EXPMultiplier");
            VBloodEXPMultiplier = Config.Bind("Experience", "VBlood Multiplier", 15f, "Multiply EXP gained from VBlood kills.\n" +
                "Formula: EXPGained * VBloodMultiplier * EXPMultiplier");
            EXPFormula_1 = Config.Bind("Experience", "Constant", 0.2f, "Increase or decrease the required EXP to level up.\n" +
                "Formula: (level/constant)^2\n" +
                "EXP Table & Formula: https://bit.ly/3npqdJw");
            EXPGroupModifier = Config.Bind("Experience", "Group Modifier", 0.75, "Set the modifier for EXP gained for each ally(player) in vicinity.\n" +
                "Example if you have 2 ally nearby, EXPGained = ((EXPGained * Modifier)*Modifier)");
            EXPGroupMaxDistance = Config.Bind("Experience", "Group Range", 50f, "Set the maximum distance an ally (player) has to be from the player for them to share EXP with the player");
            EXPGroupLevelScheme = Config.Bind("Experience", "Group level Scheme", 2, "Configure the group levelling scheme. See documentation.");

            pvpXPLoss = Config.Bind("Rates, Experience", "PvP XP Loss", 0f, "Sets the flat XP Lost on a PvP death");
            pvpXPLossPerLevel = Config.Bind("Rates, Experience", "PvP XP Loss per Level", 0f, "Sets the XP Lost per level of the dying player on a PvP death");
            pvpXPLossPercent = Config.Bind("Rates, Experience", "PvP XP Loss Percent", 0f, "Sets the percentage of XP to the next level lost on a PvP death");
            pvpXPLossPercentPerLevel = Config.Bind("Rates, Experience", "PvP XP Loss Percent per Level", 0f, "Sets the percentage of XP to the next level lost per level of the dying player on a PvP death");
            pvpXPLossMultPerLvlDiff = Config.Bind("Rates, Experience", "PvP XP Loss Per lvl Diff", 0f, "Adds this times the number of levels higher than your killer you are as an additional percent to your xp lost on a PvP death.");
            pvpXPLossMultPerLvlDiffSq = Config.Bind("Rates, Experience", "PvP XP Loss Per lvl Diff squared", 0f, "Adds this times the square of the number of levels higher than your killer you are as an additional percent to your xp lost on a PvP death.");
            
            pveXPLoss = Config.Bind("Rates, Experience", "PvE XP Loss", 0f, "Sets the flat XP Lost on a PvE death");
            pveXPLossPerLevel = Config.Bind("Rates, Experience", "PvE XP Loss per Level", 0f, "Sets the XP Lost per level of the dying player on a PvE death");
            pveXPLossPercent = Config.Bind("Rates, Experience", "PvE XP Loss Percent", 10f, "Sets the percentage of XP to the next level lost on a PvE death");
            pveXPLossPercentPerLevel = Config.Bind("Rates, Experience", "PvE XP Loss Percent per Level", 0f, "Sets the percentage of XP to the next level lost per level of the dying player on a PvE death");
            pveXPLossMultPerLvlDiff = Config.Bind("Rates, Experience", "PvE XP Loss Mult Per lvl Diff", 0f, "Adds this times the number of levels higher than your killer you are as an additional percent to your xp lost on a PvE death.");
            pveXPLossMultPerLvlDiffSq = Config.Bind("Rates, Experience", "PvE XP Loss Per lvl Diff squared", 0f, "Adds this times the square of the number of levels higher than your killer you are as an additional percent to your xp lost on a PvE death.");

            xpLossOnDown = Config.Bind("Rates, Experience", "XP Lost on Down", false, "Vampires are treated as dead for the XP system when they are downed.");
            xpLossOnRelease = Config.Bind("Rates, Experience", "XP Lost on Release", true, "Vampires are treated as dead for the XP system when they release, incentivising saving allies.");

            EnableWeaponMaster = Config.Bind("Mastery", "Enable Weapon Mastery", true, "Enable/disable the weapon mastery system.");
            EnableWeaponMasterDecay = Config.Bind("Mastery", "Enable Mastery Decay", false, "Enable/disable the decay of weapon mastery when the user is offline.");
            WeaponMaxMastery = Config.Bind("Mastery", "Max Mastery Value", 100d, "Configure the maximum mastery the user can atain. (100000 is 100%)");
            MasteryCombatTick = Config.Bind("Mastery", "Mastery Value/Combat Ticks", 5, "Configure the amount of mastery gained per combat ticks. (5 -> 0.005%)");
            MasteryMaxCombatTicks = Config.Bind("Mastery", "Max Combat Ticks", 12, "Mastery will no longer increase after this many ticks is reached in combat. (1 tick = 5 seconds)");
            WeaponMasterMultiplier = Config.Bind("Mastery", "Mastery Multiplier", 1d, "Multiply the gained mastery value by this amount.");
            WeaponMastery_VBloodMultiplier = Config.Bind("Mastery", "VBlood Mastery Multiplier", 15d, "Multiply Mastery gained from VBlood kill.");

            UnarmedStats = Config.Bind("Rates, Mastery", "Unarmed Stats", " 0, 5 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            UnarmedRates = Config.Bind("Rates, Mastery", "Unarmed Rates", " 0.25, 0.01 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SpearStats = Config.Bind("Rates, Mastery", "Spear Stats", " 0 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SpearRates = Config.Bind("Rates, Mastery", "Spear Rates", " 0.25", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SwordStats = Config.Bind("Rates, Mastery", "Sword Stats", " 0, 25 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SwordRates = Config.Bind("Rates, Mastery", "Sword Rates", " 0.125, 0.125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            ScytheStats = Config.Bind("Rates, Mastery", "Scythe Stats", " 0, 29 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            ScytheRates = Config.Bind("Rates, Mastery", "Scythe Rates", " 0.125, 0.00125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            CrossbowStats = Config.Bind("Rates, Mastery", "Crossbow Stats", " 29 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            CrossbowRates = Config.Bind("Rates, Mastery", "Crossbow Rates", " 0.0025", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            MaceStats = Config.Bind("Rates, Mastery", "Mace Stats", " 4 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            MaceRates = Config.Bind("Rates, Mastery", "Mace Rates", " 1 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SlasherStats = Config.Bind("Rates, Mastery", "Slasher Stats", " 29, 5 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SlasherRates = Config.Bind("Rates, Mastery", "Slasher Rates", " 0.00125, 0.005 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            AxeStats = Config.Bind("Rates, Mastery", "Axe Stats", " 0, 4 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            AxeRates = Config.Bind("Rates, Mastery", "Axe Rates", " 0.125, 0.5 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            FishingPoleStats = Config.Bind("Rates, Mastery", "Fishing Pole Stats", " ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            FishingPoleRates = Config.Bind("Rates, Mastery", "Fishing Pole Rates", " ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SpellStats = Config.Bind("Rates, Mastery", "Spell Stats", " 7 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SpellRates = Config.Bind("Rates, Mastery", "Spell Rates", " 100 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            RapierStats = Config.Bind("Rates, Mastery", "Rapier Stats", " 29, 32 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            RapierRates = Config.Bind("Rates, Mastery", "Rapier Rates", " 0.00125, 0.00125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            PistolStats = Config.Bind("Rates, Mastery", "Pistol Stats", " 29, 30 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            PistolRates = Config.Bind("Rates, Mastery", "Pistol Rates", " 0.00125, 0.0125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            GreatswordStats = Config.Bind("Rates, Mastery", "Greatsword Stats", " 0, 30 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            GreatswordRates = Config.Bind("Rates, Mastery", "Greatsword Rates", " 0.125, 0.0125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");

            effectivenessSubSystemEnabled = Config.Bind("Mastery", "Enable Effectiveness Subsystem", true, "Enables the Effectiveness mastery subsystem, which lets you reset your mastery to gain a multiplier to the effects of the matching mastery.");
            maxEffectiveness = Config.Bind("Mastery", "Maximum Effectiveness", 5f, "The maximum mastery effectiveness where 1 is 100%.");
            growthSubSystemEnabled = Config.Bind("Mastery", "Enable Growth Subsystem", true, "Enables the growth subsystem, when you reset mastery either increases or decreases your matching mastery growth rate, depending on config.");
            minGrowth = Config.Bind("Mastery", "Minimum Growth Rate", 0.1f, "The minimum growth rate, where 1 is 100%");
            maxGrowth = Config.Bind("Mastery", "Maximum Growth Rate", 10f, "the maximum growth rate where 1 is 100%");
            growthPerEfficency = Config.Bind("Mastery", "Growth per efficency", -1f, "The amount of growth gained per point of efficency gained, if negative will reduce accordingly (gaining 100% efficency with -1 here will halve your current growth)");

            WeaponDecayInterval = Config.Bind("Mastery", "Decay Interval", 60, "Every amount of seconds the user is offline by the configured value will translate as 1 decay tick.");
            Offline_Weapon_MasteryDecayValue = Config.Bind("Mastery", "Decay Value", 1, "Mastery will decay by this amount for every decay tick.(1 -> 0.001%)");
            WeaponMasterySpellMasteryNeedsNoneToUse = Config.Bind("Mastery", "Unarmed Only Spell Mastery Use", false, "Gain the benefits of spell mastery only when you have no weapon equipped.");
            WeaponMasterySpellMasteryNeedsNoneToLearn = Config.Bind("Mastery", "Unarmed Only Spell Mastery Learning", true, "Progress spell mastery only when you have no weapon equipped.");
            WeaponLinearSpellMastery = Config.Bind("Mastery", "Linear Mastery CDR", true, "Changes CDR from mastery to provide a linear increase to spells able to be cast in a given time by making the cdr diminishing.");
            WeaponSpellMasteryCDRStacks = Config.Bind("Mastery", "Mastery CDR stacks", true, "Allows mastery cdr to stack with that from other sources, the reduction is multiplicative. E.G. Mist signet (10% cdr) and 100% mastery (50% cdr) will result in 55% total cdr, or 120%ish faster cooldowns.");

            bloodlinesEnabled = Config.Bind("Bloodlines", "Enable Bloodlines", true, "Enables the Effectiveness mastery subsystem, which lets you reset your mastery to gain a multiplier to the effects of the matching mastery.");
            mercilessBloodlines = Config.Bind("Bloodlines", "Merciless Bloodlines", true, "Causes bloodlines to only grow when you kill something with a matching bloodline of higher strength, finally, a reward when you accidentally kill that 100% blood you found");
            draculaGetsAll = Config.Bind("Bloodlines", "Dracula inherits all bloodlines", true, "Determines if Dracula (Frail) blood should inherit a portion of the other bloodlines.");
            bloodlineGrowthMultiplier = Config.Bind("Bloodlines", "Bloodline growth multiplier", 1.0, "The multiplier applied to all bloodline gains.");
            bloodlineVBloodMultiplier = Config.Bind("Bloodlines", "Bloodline VBlood Multiplier", 25.0, "The multiplier applied to the effective level of VBlood enemies for bloodline gains.");
            bloodlineEfficencySubSystem = Config.Bind("Bloodlines", "Enable Effectiveness Subsystem", true, "Enables the Effectiveness bloodline subsystem, which lets you reset your bloodline to gain a multiplier to the effects of the matching bloodline.");
            bloodlineGrowthSubsystem = Config.Bind("Bloodlines", "Enable Growth Subsystem", true, "Enables the Growth Bloodline subsystem, same as the one for mastery");
            MaxBloodlineStrength = Config.Bind("Bloodlines", "Maximum Strength", 100.0, "The maximum strength for a bloodline in percentage.");
            maxBloodlineEfficency = Config.Bind("Bloodlines", "Maximum Effectiveness", 5.0, "The maximum bloodline effectiveness where 1 is 100%.");
            minBloodlineGrowth = Config.Bind("Bloodlines", "Minimum Growth Rate", 0.1, "The minimum growth rate, where 1 is 100%");
            maxBloodlineGrowth = Config.Bind("Bloodlines", "Maximum Growth Rate", 10.0, "the maximum growth rate where 1 is 100%");
            bloodlineGrowthPerEfficency = Config.Bind("Bloodlines", "Growth per efficency", -1.0, "The amount of growth gained per point of efficency gained, if negative will reduce accordingly (gaining 100% efficency with -1 here will halve your current growth)");

            draculaBloodlineStats = Config.Bind("Rates, Bloodline", "Dracula Bloodline Stats", "", "The stat IDs for the frailed bloodline of Dracula the Progenitor, Active only with frailed blood.");
            draculaBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Dracula Bloodline Minimum Strengths", "", "The minimum bloodline strength to recieve the specified stat.");
            draculaBloodlineRates = Config.Bind("Rates, Bloodline", "Dracula Bloodline Rates", "", "The amount per bloodline strength % recieved once strength is met, Note that Dracula's Bloodline recieves a portion of all your other bloodlines.");

            arwenBloodlineStats = Config.Bind("Rates, Bloodline", "Arwen Bloodline Stats", "10, 5, 39", "The stat IDs for the bloodline of Arwen the Godeater, Active only with creature blood.");
            arwenBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Arwen Bloodline Minimum Strengths", "0, 50, 100", "The minimum bloodline strength to recieve the specified stat.");
            arwenBloodlineRates = Config.Bind("Rates, Bloodline", "Arwen Bloodline Rates", "0.25, 0.005, 0.0025", "The amount per bloodline strength % recieved once strength is met.");

            ilvrisBloodlineStats = Config.Bind("Rates, Bloodline", "Ilvris Bloodline Stats", "9, 0, 42", "The stat IDs for the bloodline of Ilvris Dragonblood, Active only with warrior blood.");
            ilvrisBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Ilvris Bloodline Minimum Strengths", "0, 50, 100 ", "The minimum bloodline strength to recieve the specified stat.");
            ilvrisBloodlineRates = Config.Bind("Rates, Bloodline", "Ilvris Bloodline Rates", "0.25, 0.1, 0.0025 ", "The amount per bloodline strength % recieved once strength is met.");

            ayaBloodlineStats = Config.Bind("Rates, Bloodline", "Aya Bloodline Stats", "19, 29, 44", "The stat IDs for the bloodline of Aya the Shadowlord, Active only with rogue blood.");
            ayaBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Aya Bloodline Minimum Strengths", "0, 50, 100 ", "The minimum bloodline strength to recieve the specified stat.");
            ayaBloodlineRates = Config.Bind("Rates, Bloodline", "Aya Bloodline Rates", "0.25, 0.001, 0.0025 ", "The amount per bloodline strength % recieved once strength is met.");

            nytheriaBloodlineStats = Config.Bind("Rates, Bloodline", "Nytheria Bloodline Stats", "11, 30, 38", "The stat IDs for the bloodline of Nytheria the Destroyer, Active only with brute blood.");
            nytheriaBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Nytheria Bloodline Minimum Strengths", "0, 50, 100 ", "The minimum bloodline strength to recieve the specified stat.");
            nytheriaBloodlineRates = Config.Bind("Rates, Bloodline", "Nytheria Bloodline Rates", "0.25, 0.01, 0.0025", "The amount per bloodline strength % recieved once strength is met.");
            
            hadubertBloodlineStats = Config.Bind("Rates, Bloodline", "Hadubert Bloodline Stats", "25, 7, 40", "The stat IDs for the bloodline of Hadubert the Inferno, Active only with scholar blood.");
            hadubertBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Hadubert Bloodline Minimum Strengths", "0, 50, 100 ", "The minimum bloodline strength to recieve the specified stat.");
            hadubertBloodlineRates = Config.Bind("Rates, Bloodline", "Hadubert Bloodline Rates", "0.1, 200, 0.0025", "The amount per bloodline strength % recieved once strength is met.");

            reiBloodlineStats = Config.Bind("Rates, Bloodline", "Rei Bloodline Stats", "20, 3, 52, 53, 54", "The stat IDs for the bloodline of Rei the Binder, Active only with worker blood.");
            reiBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Rei Bloodline Minimum Strengths", "0, 50, 100, 100, 100", "The minimum bloodline strength to recieve the specified stat.");
            reiBloodlineRates = Config.Bind("Rates, Bloodline", "Rei Bloodline Rates", "0.25, 0.01, 0.0025, 0.0025, 0.0025", "The amount per bloodline strength % recieved once strength is met.");

            semikaBloodlineStats = Config.Bind("Rates, Bloodline", "Semika Bloodline Stats", "31, 5, 39", "The stat IDs for the bloodline of Semika the Evershifting, Active only with mutant blood.");
            semikaBloodlineMinStrengths = Config.Bind("Rates, Bloodline", "Semika Bloodline Minimum Strengths", "0, 50, 100", "The minimum bloodline strength to recieve the specified stat.");
            semikaBloodlineRates = Config.Bind("Rates, Bloodline", "Semika Bloodline Rates", "0.005, 0.005, 0.0025", "The amount per bloodline strength % recieved once strength is met.");

            bloodlineNames = Config.Bind("Rates, Bloodline", "Bloodline Names", "Dracula the Progenitor, Arwen the Godeater, Ilvris Dragonblood, Aya the Shadowlord, Nytheria the Destroyer, Hadubert the Inferno, Rei the Binder, Semika the Evershifting", "Rename the bloodlines here, the starting names are from supporters, Seperate names with commas, must contain exactly 8 names.");

            buffID = Config.Bind("Buff System", "Buff GUID", Helper.buffGUID, "The GUID of the buff you want to hijack for the buffs from mastery, bloodlines, and everything else from this mod\nDefault is now boneguard set bonus 2, 1409441911 is cloak, but you can set anything else too");
            forbiddenBuffID = Config.Bind("Buff System", "Forbidden Buff GUID", Helper.forbiddenBuffGUID, "The GUID of the buff that prohibits you from getting mastery buffs\nDefault is boneguard set bonus 1, so you cant double up.");
            appliedBuff = Config.Bind("Buff System", "Applied Buff", Helper.buffGUID, "The GUID of the buff that gets applied when mastery, bloodline, etc changes. Doesnt need to be the same as the Buff GUID.");
            humanReadablePercentageStats = Config.Bind("Buff System", "Human Readable Percentage Stats", false, "Determines if rates for percentage stats should be read as out of 100 instead of 1, off by default for compatability.");
            inverseMultiplersDisplayReduction = Config.Bind("Buff System", "Inverse Multipliers Display Reduction", true, "Determines if inverse multiplier stats dispay their reduction, or the final value.");

            EnableWorldDynamics = Config.Bind("World Dynamics", "Enable Faction Dynamics", false, $"All other faction dynamics data & config is within {AutoSaveSystem.WorldDynamicsJson} file.");
            WDGrowOnKill = Config.Bind("World Dynamics", "Factions grow on kill", false, "Inverts the faction dynamic system, so that they grow stronger when killed and weaker over time.");
            
            disableCommandAdminRequirement = Config.Bind("Admin", "Disable command admin requirement", false, "Disables all \"isAdmin\" checks for running commands.");
        }

        public override void Load()
        {
            // Ensure the logger is accessible in static contexts.
            Logger = base.Log;
            if(!IsServer)
            {
                Plugin.Log(LogSystem.Plugin, LogLevel.Warning, $"This is a server plugin. Not continuing to load on client.", true);
                return;
            }
            
            InitConfig();
            CommandRegistry.RegisterAll();
            GameData.OnInitialize += GameDataOnInitialize;
            GameData.OnDestroy += GameDataOnDestroy;
            Instance = this;
            RandomEncounters.Load();
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, $"Plugin is loaded", true);
        }

        private static void GameDataOnInitialize(World world)
        {
            RandomEncounters.GameData_OnInitialize();
            RandomEncounters._encounterTimer = new Timer();
            if (RandomEncountersConfig.Enabled.Value)
            {
                RandomEncounters.StartEncounterTimer();
            }
            TaskRunner.Initialize();
            
            // Load config
            DebugLoggingConfig.Initialise();
            
            Initialize();
        }

        private static void GameDataOnDestroy()
        {
        }

        public override bool Unload()
        {
            Config.Clear();
            harmony.UnpatchSelf();
            return true;
        }

        public static void Initialize()
        {
            Plugin.Log(LogSystem.Plugin, LogLevel.Warning, $"Trying to Initialize {MyPluginInfo.PLUGIN_NAME}: isInitialized == {isInitialized}", isInitialized);
            if (isInitialized) return;
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, $"Initializing {MyPluginInfo.PLUGIN_NAME}...", true);
            
            //-- Initialize System
            Helper.GetServerGameSettings(out Helper.SGS);
            Helper.GetServerGameManager(out Helper.SGM);
            Helper.GetUserActivityGridSystem(out Helper.UAGS);

            //-- Apply configs
            Waypoint.WaypointLimit = WaypointLimit.Value;

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading permission config");
            PermissionSystem.isVIPSystem = EnableVIPSystem.Value;
            PermissionSystem.isVIPWhitelist = EnableVIPWhitelist.Value;
            PermissionSystem.VIP_Permission = VIP_Permission.Value;

            PermissionSystem.VIP_InCombat_ResYield = VIP_InCombat_ResYield.Value;
            PermissionSystem.VIP_InCombat_DurabilityLoss = VIP_InCombat_DurabilityLoss.Value;
            PermissionSystem.VIP_InCombat_MoveSpeed = VIP_InCombat_MoveSpeed.Value;
            PermissionSystem.VIP_InCombat_GarlicResistance = VIP_InCombat_GarlicResistance.Value;
            PermissionSystem.VIP_InCombat_SilverResistance = VIP_InCombat_SilverResistance.Value;

            PermissionSystem.VIP_OutCombat_ResYield = VIP_OutCombat_ResYield.Value;
            PermissionSystem.VIP_OutCombat_DurabilityLoss = VIP_OutCombat_DurabilityLoss.Value;
            PermissionSystem.VIP_OutCombat_MoveSpeed = VIP_OutCombat_MoveSpeed.Value;
            PermissionSystem.VIP_OutCombat_GarlicResistance = VIP_OutCombat_GarlicResistance.Value;
            PermissionSystem.VIP_OutCombat_SilverResistance = VIP_OutCombat_SilverResistance.Value;

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading HunterHunted config");
            HunterHuntedSystem.isActive = HunterHuntedEnabled.Value;
            HunterHuntedSystem.heat_cooldown = HeatCooldown.Value;
            HunterHuntedSystem.ambush_interval = Ambush_Interval.Value;
            HunterHuntedSystem.ambush_chance = Ambush_Chance.Value;
            HunterHuntedSystem.vBloodMultiplier = VBloodHeatMultiplier.Value;

            if (Ambush_Despawn_Unit_Timer.Value < 1) Ambush_Despawn_Unit_Timer.Value = 300f;
            HunterHuntedSystem.ambush_despawn_timer = Ambush_Despawn_Unit_Timer.Value + 0.44444f;


            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading XP config");
            ExperienceSystem.isEXPActive = EnableExperienceSystem.Value;
            ExperienceSystem.ShouldAllowGearLevel = ShouldAllowGearLevel.Value;
            ExperienceSystem.LevelRewardsOn = EnableLevelRewards.Value;
            ExperienceSystem.MaxLevel = MaxLevel.Value;
            ExperienceSystem.EXPMultiplier = EXPMultiplier.Value;
            ExperienceSystem.VBloodMultiplier = VBloodEXPMultiplier.Value;
            ExperienceSystem.EXPConstant = EXPFormula_1.Value;
            ExperienceSystem.GroupModifier = EXPGroupModifier.Value;
            ExperienceSystem.GroupMaxDistance = EXPGroupMaxDistance.Value;
            ExperienceSystem.easyLvl15 = EasyLevel15.Value;

            ExperienceSystem.pvpXPLoss = pvpXPLoss.Value;
            ExperienceSystem.pvpXPLossPerLevel = pvpXPLossPerLevel.Value;
            ExperienceSystem.pvpXPLossPercent = pvpXPLossPercent.Value;
            ExperienceSystem.pvpXPLossPercentPerLevel = pvpXPLossPercentPerLevel.Value;
            ExperienceSystem.pvpXPLossMultPerLvlDiff = pvpXPLossMultPerLvlDiff.Value;
            ExperienceSystem.pvpXPLossMultPerLvlDiffSq = pvpXPLossMultPerLvlDiffSq.Value;

            ExperienceSystem.pveXPLoss = pveXPLoss.Value;
            ExperienceSystem.pveXPLossPerLevel = pveXPLossPerLevel.Value;
            ExperienceSystem.pveXPLossPercent = pveXPLossPercent.Value;
            ExperienceSystem.pveXPLossPercentPerLevel = pveXPLossPercentPerLevel.Value;
            ExperienceSystem.pveXPLossMultPerLvlDiff = pveXPLossMultPerLvlDiff.Value;
            ExperienceSystem.pveXPLossMultPerLvlDiffSq = pveXPLossMultPerLvlDiffSq.Value;

            ExperienceSystem.xpLostOnDown = xpLossOnDown.Value;
            ExperienceSystem.xpLostOnRelease = xpLossOnRelease.Value;

            if (Enum.IsDefined(typeof(ExperienceSystem.GroupLevelScheme), EXPGroupLevelScheme.Value)) {
                ExperienceSystem.groupLevelScheme = (ExperienceSystem.GroupLevelScheme)EXPGroupLevelScheme.Value;
            }

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading weapon mastery config");
            WeaponMasterSystem.isMasteryEnabled = EnableWeaponMaster.Value;
            WeaponMasterSystem.isDecaySystemEnabled = EnableWeaponMasterDecay.Value;
            WeaponMasterSystem.Offline_DecayValue = Offline_Weapon_MasteryDecayValue.Value;
            WeaponMasterSystem.DecayInterval = WeaponDecayInterval.Value;
            WeaponMasterSystem.VBloodMultiplier = WeaponMastery_VBloodMultiplier.Value;
            WeaponMasterSystem.MasteryMultiplier = WeaponMasterMultiplier.Value;
            WeaponMasterSystem.MaxMastery = WeaponMaxMastery.Value;
            WeaponMasterSystem.MasteryCombatTick = MasteryCombatTick.Value;
            WeaponMasterSystem.MaxCombatTick = MasteryMaxCombatTicks.Value;
            WeaponMasterSystem.spellMasteryNeedsNoneToUse = WeaponMasterySpellMasteryNeedsNoneToUse.Value;
            WeaponMasterSystem.spellMasteryNeedsNoneToLearn = WeaponMasterySpellMasteryNeedsNoneToLearn.Value;
            WeaponMasterSystem.linearCDR = WeaponLinearSpellMastery.Value;
            WeaponMasterSystem.CDRStacks = WeaponSpellMasteryCDRStacks.Value;
            WeaponMasterSystem.growthSubSystemEnabled = growthSubSystemEnabled.Value;

            WeaponMasterSystem.UnarmedStats = parseIntArrayConifg(UnarmedStats.Value);
            WeaponMasterSystem.UnarmedRates = parseDoubleArrayConifg(UnarmedRates.Value);
            WeaponMasterSystem.SpearStats = parseIntArrayConifg(SpearStats.Value);
            WeaponMasterSystem.SpearRates = parseDoubleArrayConifg(SpearRates.Value);
            WeaponMasterSystem.SwordStats = parseIntArrayConifg(SwordStats.Value);
            WeaponMasterSystem.SwordRates = parseDoubleArrayConifg(SwordRates.Value);
            WeaponMasterSystem.ScytheStats = parseIntArrayConifg(ScytheStats.Value);
            WeaponMasterSystem.ScytheRates = parseDoubleArrayConifg(ScytheRates.Value);
            WeaponMasterSystem.CrossbowStats = parseIntArrayConifg(CrossbowStats.Value);
            WeaponMasterSystem.CrossbowRates = parseDoubleArrayConifg(CrossbowRates.Value);
            WeaponMasterSystem.SlasherStats = parseIntArrayConifg(SlasherStats.Value);
            WeaponMasterSystem.SlasherRates = parseDoubleArrayConifg(SlasherRates.Value);
            WeaponMasterSystem.MaceStats = parseIntArrayConifg(MaceStats.Value);
            WeaponMasterSystem.MaceRates = parseDoubleArrayConifg(MaceRates.Value);
            WeaponMasterSystem.AxeStats = parseIntArrayConifg(AxeStats.Value);
            WeaponMasterSystem.AxeRates = parseDoubleArrayConifg(AxeRates.Value);
            WeaponMasterSystem.FishingPoleStats = parseIntArrayConifg(FishingPoleStats.Value);
            WeaponMasterSystem.FishingPoleRates = parseDoubleArrayConifg(FishingPoleRates.Value);
            WeaponMasterSystem.SpellStats = parseIntArrayConifg(SpellStats.Value);
            WeaponMasterSystem.SpellRates = parseDoubleArrayConifg(SpellRates.Value);
            WeaponMasterSystem.RapierStats = parseIntArrayConifg(RapierStats.Value);
            WeaponMasterSystem.RapierRates = parseDoubleArrayConifg(RapierRates.Value);
            WeaponMasterSystem.PistolStats = parseIntArrayConifg(PistolStats.Value);
            WeaponMasterSystem.PistolRates = parseDoubleArrayConifg(PistolRates.Value);
            WeaponMasterSystem.GreatSwordStats = parseIntArrayConifg(GreatswordStats.Value);
            WeaponMasterSystem.GreatSwordRates = parseDoubleArrayConifg(GreatswordRates.Value);

            WeaponMasterSystem.masteryStats = new int[][] { WeaponMasterSystem.SpellStats, WeaponMasterSystem.UnarmedStats, WeaponMasterSystem.SpearStats, WeaponMasterSystem.SwordStats, WeaponMasterSystem.ScytheStats, WeaponMasterSystem.CrossbowStats, WeaponMasterSystem.MaceStats, WeaponMasterSystem.SlasherStats, WeaponMasterSystem.AxeStats, WeaponMasterSystem.FishingPoleStats, WeaponMasterSystem.RapierStats, WeaponMasterSystem.PistolStats, WeaponMasterSystem.GreatSwordStats };
            WeaponMasterSystem.masteryRates = new double[][] { WeaponMasterSystem.SpellRates, WeaponMasterSystem.UnarmedRates, WeaponMasterSystem.SpearRates, WeaponMasterSystem.SwordRates, WeaponMasterSystem.ScytheRates, WeaponMasterSystem.CrossbowRates, WeaponMasterSystem.MaceRates, WeaponMasterSystem.SlasherRates, WeaponMasterSystem.AxeRates, WeaponMasterSystem.FishingPoleRates, WeaponMasterSystem.RapierRates, WeaponMasterSystem.PistolRates, WeaponMasterSystem.GreatSwordRates };

            WeaponMasterSystem.effectivenessSubSystemEnabled = effectivenessSubSystemEnabled.Value;
            WeaponMasterSystem.maxEffectiveness = maxEffectiveness.Value;
            WeaponMasterSystem.growthSubSystemEnabled = effectivenessSubSystemEnabled.Value;
            WeaponMasterSystem.minGrowth = minGrowth.Value;
            WeaponMasterSystem.maxGrowth = maxGrowth.Value;
            WeaponMasterSystem.growthPerEfficency = growthPerEfficency.Value;


            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading bloodlines config");
            Bloodlines.draculaStats = parseIntArrayConifg(draculaBloodlineStats.Value);
            Bloodlines.draculaMinStrength = parseDoubleArrayConifg(draculaBloodlineMinStrengths.Value);
            Bloodlines.draculaRates = parseDoubleArrayConifg(draculaBloodlineRates.Value);
            Bloodlines.arwenStats = parseIntArrayConifg(arwenBloodlineStats.Value);
            Bloodlines.arwenMinStrength = parseDoubleArrayConifg(arwenBloodlineMinStrengths.Value);
            Bloodlines.arwenRates = parseDoubleArrayConifg(arwenBloodlineRates.Value);
            Bloodlines.ilvrisStats = parseIntArrayConifg(ilvrisBloodlineStats.Value);
            Bloodlines.ilvrisMinStrength = parseDoubleArrayConifg(ilvrisBloodlineMinStrengths.Value);
            Bloodlines.ilvrisRates = parseDoubleArrayConifg(ilvrisBloodlineRates.Value);
            Bloodlines.ayaStats = parseIntArrayConifg(ayaBloodlineStats.Value);
            Bloodlines.ayaMinStrength = parseDoubleArrayConifg(ayaBloodlineMinStrengths.Value);
            Bloodlines.ayaRates = parseDoubleArrayConifg(ayaBloodlineRates.Value);
            Bloodlines.nytheriaStats = parseIntArrayConifg(nytheriaBloodlineStats.Value);
            Bloodlines.nytheriaMinStrength = parseDoubleArrayConifg(nytheriaBloodlineMinStrengths.Value);
            Bloodlines.nytheriaRates = parseDoubleArrayConifg(nytheriaBloodlineRates.Value);
            Bloodlines.hadubertStats = parseIntArrayConifg(hadubertBloodlineStats.Value);
            Bloodlines.hadubertMinStrength = parseDoubleArrayConifg(hadubertBloodlineMinStrengths.Value);
            Bloodlines.hadubertRates = parseDoubleArrayConifg(hadubertBloodlineRates.Value);
            Bloodlines.reiStats = parseIntArrayConifg(reiBloodlineStats.Value);
            Bloodlines.reiMinStrength = parseDoubleArrayConifg(reiBloodlineMinStrengths.Value);
            Bloodlines.reiRates = parseDoubleArrayConifg(reiBloodlineRates.Value);
            Bloodlines.semikaStats = parseIntArrayConifg(semikaBloodlineStats.Value);
            Bloodlines.semikaMinStrength = parseDoubleArrayConifg(semikaBloodlineMinStrengths.Value);
            Bloodlines.semikaRates = parseDoubleArrayConifg(semikaBloodlineRates.Value);
            string[] blNames = parseStringArrayConifg(bloodlineNames.Value);
            Bloodlines.names = blNames;
            for(int i = 0; i < blNames.Length; i++) {
                Bloodlines.nameMap.TryAdd(blNames[i].ToLower().Trim(), i);
            }

            Bloodlines.stats = new int[][] { Bloodlines.draculaStats, Bloodlines.arwenStats, Bloodlines.ilvrisStats, Bloodlines.ayaStats, Bloodlines.nytheriaStats, Bloodlines.hadubertStats, Bloodlines.reiStats, Bloodlines.semikaStats, };
            Bloodlines.minStrengths = new double[][] { Bloodlines.draculaMinStrength, Bloodlines.arwenMinStrength, Bloodlines.ilvrisMinStrength, Bloodlines.ayaMinStrength, Bloodlines.nytheriaMinStrength, Bloodlines.hadubertMinStrength, Bloodlines.reiMinStrength, Bloodlines.semikaMinStrength };
            Bloodlines.rates = new double[][] { Bloodlines.draculaRates, Bloodlines.arwenRates, Bloodlines.ilvrisRates, Bloodlines.ayaRates, Bloodlines.nytheriaRates, Bloodlines.hadubertRates, Bloodlines.reiRates, Bloodlines.semikaRates };

            Bloodlines.areBloodlinesEnabled = bloodlinesEnabled.Value;
            Bloodlines.mercilessBloodlines = mercilessBloodlines.Value;
            Bloodlines.draculaGetsAll = draculaGetsAll.Value;
            Bloodlines.effectivenessSubSystemEnabled = bloodlineEfficencySubSystem.Value;
            Bloodlines.growthSubsystemEnabled = bloodlineGrowthSubsystem.Value;

            Bloodlines.MaxBloodlineStrength = MaxBloodlineStrength.Value;
            Bloodlines.maxBloodlineEfficency = maxBloodlineEfficency.Value;
            Bloodlines.maxBloodlineGrowth = maxBloodlineGrowth.Value;
            Bloodlines.minBloodlineGrowth = minBloodlineGrowth.Value;
            Bloodlines.growthPerEfficency = bloodlineGrowthPerEfficency.Value;
            Bloodlines.VBloodMultiplier = bloodlineVBloodMultiplier.Value;
            Bloodlines.growthMultiplier = bloodlineGrowthMultiplier.Value;

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Loading world dynamics config");
            WorldDynamicsSystem.isFactionDynamic = EnableWorldDynamics.Value;
            WorldDynamicsSystem.growOnKill = WDGrowOnKill.Value;

            Helper.buffGUID = buffID.Value;
            Helper.AppliedBuff = new PrefabGUID(appliedBuff.Value);
            Helper.forbiddenBuffGUID = forbiddenBuffID.Value;
            Helper.humanReadablePercentageStats = humanReadablePercentageStats.Value;
            Helper.inverseMultipersDisplayReduction = inverseMultiplersDisplayReduction.Value;
            
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Initialising player cache and internal database...");
            Helper.CreatePlayerCache();
            AutoSaveSystem.LoadDatabase();
            
            // Validate any potential change in permissions
            var commands = Command.GetAllCommands();
            Command.ValidatedCommandPermissions(commands);
            // Note for devs: To regenerate Command.md and PermissionSystem.DefaultCommandPermissions, uncomment the following:
            // Command.GenerateCommandMd(commands);
            // Command.GenerateDefaultCommandPermissions(commands);
            
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, $"Setting CommandRegistry middleware");
            if (disableCommandAdminRequirement.Value)
            {
                Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Removing admin privilege requirements");
                CommandRegistry.Middlewares.Clear();                
            }
            CommandRegistry.Middlewares.Add(new Command.PermissionMiddleware());

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, "Finished initialising", true);

            isInitialized = true;
        }

        // TODO move to a util file
        public static int[] parseIntArrayConifg(string data) {
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>parsing int array: " + data);
            var match = Regex.Match(data, "([0-9]+)");
            List<int> list = new List<int>();
            while (match.Success) {
                try {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>got int: " + match.Value);
                    int temp = int.Parse(match.Value, CultureInfo.InvariantCulture);
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>int parsed into: " + temp);
                    list.Add(temp);
                }
                catch {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Warning, "Error interperting integer value: " + match.ToString());
                }
                match = match.NextMatch();
            }
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>done parsing int array");
            return list.ToArray();
        }
        public static float[] parseFloatArrayConifg(string data) {
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>parsing float array: " + data);
            var match = Regex.Match(data, "[-+]?[0-9]*\\.?[0-9]+");
            List<float> list = new List<float>();
            while (match.Success) {
                try {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>got float: " + match.Value);
                    float temp = float.Parse(match.Value, CultureInfo.InvariantCulture);
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>float parsed into: " + temp);
                    list.Add(temp);
                }
                catch {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Warning, "Error interperting float value: " + match.ToString());
                }

                match = match.NextMatch();
            }

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>done parsing float array");
            return list.ToArray();
        }
        public static double[] parseDoubleArrayConifg(string data) {
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>parsing double array: " + data);
            var match = Regex.Match(data, "[-+]?[0-9]*\\.?[0-9]+");
            List<double> list = new List<double>();
            while (match.Success) {
                try {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>got double: " + match.Value);
                    double temp = double.Parse(match.Value, CultureInfo.InvariantCulture);
                    Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>double parsed into: " + temp);
                    list.Add(temp);
                }
                catch {
                    Plugin.Log(LogSystem.Plugin, LogLevel.Warning, "Error interperting double value: " + match.ToString());
                }

                match = match.NextMatch();
            }

            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>done parsing double array");
            return list.ToArray();
        }
        public static string[] parseStringArrayConifg(string data) {
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>parsing comma seperated String array: " + data);
            List<string> list = new List<string>();
            while (data.IndexOf(",") > 0) {
                string str = data.Substring(0, data.IndexOf(","));
                str.Trim();
                list.Add(str);
                data = data.Substring(data.IndexOf(",") + 1);
            }
            data.Trim();
            list.Add(data);
            Plugin.Log(LogSystem.Plugin, LogLevel.Info, ">>>done parsing string array");
            return list.ToArray();
        }

        public enum LogSystem
        {
            Bloodline,
            Buff,
            Death,
            Faction,
            Plugin,
            PowerUp,
            RandomEncounter,
            SquadSpawn,
            Wanted,
            Xp
        }
        
        public new static void Log(LogSystem system, LogLevel logLevel, string message, bool forceLog = false)
        {
            var isLogging = forceLog || DebugLoggingConfig.IsLogging(system);
            if (isLogging) Logger.Log(logLevel, $"{DateTime.Now.ToString("u")}: [{Enum.GetName(system)}] {message}");
        }
    }
}
