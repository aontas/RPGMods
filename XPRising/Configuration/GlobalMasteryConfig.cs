﻿using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Configuration;

public static class GlobalMasteryConfig
{
    private static ConfigFile _configFile;
    
    public static void Initialize()
    {
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Loading Global Mastery config");
        var configPath = AutoSaveSystem.ConfirmFile(AutoSaveSystem.ConfigPath, "GlobalMasteryConfig.cfg");
        _configFile = new ConfigFile(configPath, true);
        
        // Currently, we are never updating and saving the config file in game, so just load the values.
        var globalVBloodMultiplier = _configFile.Bind("Global Mastery", "VBlood Mastery Multiplier", 15.0, "Multiply Mastery gained from VBlood kill.").Value;
        WeaponMasterySystem.VBloodMultiplier = globalVBloodMultiplier;
        BloodlineSystem.VBloodMultiplier = globalVBloodMultiplier;
        GlobalMasterySystem.SpellMasteryRequiresUnarmed = _configFile.Bind("Global Mastery", "Spell mastery only applies on unarmed", false, "Toggle whether the spell mastery bonus should be always applied or only applied when unarmed").Value;
        GlobalMasterySystem.MasteryConfigPreset = _configFile.Bind("Global Mastery", "Mastery Config Preset", "none", "Used to change the mastery config preset. ANY CHANGES to `Data\\globalMasteryConfig.json` will be overwritten with the preset.\nSet to \"custom\" to modify the config manually.\nCurrent preset options: basic, fixed, decay, decay-op, none.").Value;
        GlobalMasterySystem.EffectivenessSubSystemEnabled = _configFile.Bind("Global Mastery", "Enable Effectiveness Subsystem", false, "Enables the Effectiveness mastery subsystem, which lets you reset your mastery to gain a multiplier for each mastery. Max effectiveness is set via 'globalMasteryConfig.json'").Value;
        GlobalMasterySystem.DecaySubSystemEnabled = _configFile.Bind("Global Mastery", "Enable Effectiveness Subsystem", false, "Enables the Decay Mastery subsystem. This will decay mastery over time. Decay rate is set via 'globalMasteryConfig.json'").Value;
        GlobalMasterySystem.DecayInterval = _configFile.Bind("Global Mastery", "Decay Tick Interval", 60, "Amount of seconds per decay tick.").Value;

        // Weapon mastery specific config
        WeaponMasterySystem.MasteryGainMultiplier = _configFile.Bind("Mastery - Weapon", "Mastery Gain Multiplier", 1.0, "Multiply the gained mastery value by this amount.").Value;
        
        // Blood mastery specific config
        BloodlineSystem.MercilessBloodlines = _configFile.Bind("Mastery - Blood", "Merciless Bloodlines", true, "Causes blood mastery to only grow when you kill something with a matching blood type of that has a quality higher than the current blood mastery").Value;
        BloodlineSystem.MasteryGainMultiplier = _configFile.Bind("Mastery - Blood", "Mastery Gain Multiplier", 1.0, "Multiply the gained mastery value by this amount.").Value;
    }
}