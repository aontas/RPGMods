﻿using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Entities;
using Stunlock.Core;
using XPRising.Models;
using XPRising.Utils;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Systems
{
    public class BloodlineSystem
    {
        private static EntityManager _em = Plugin.Server.EntityManager;

        public static bool MercilessBloodlines = false;

        public static double VBloodMultiplier = 15;
        public static double MasteryGainMultiplier = 1.0;
        
        public static bool IsDecaySystemEnabled = false;

        public static void UpdateBloodline(Entity killer, Entity victim, bool killOnly)
        {
            if (killer == victim) return;
            if (_em.HasComponent<Minion>(victim)) return;

            var victimLevel = _em.GetComponentData<UnitLevel>(victim);
            var killerUserEntity = _em.GetComponentData<PlayerCharacter>(killer).UserEntity;
            var steamID = _em.GetComponentData<User>(killerUserEntity).PlatformId;
            Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Updating bloodline mastery for {steamID}");
            
            double growthVal = Math.Clamp(victimLevel.Level.Value - ExperienceSystem.GetLevel(steamID), 1, 10);
            
            GlobalMasterySystem.MasteryType killerBloodType;
            if (_em.TryGetComponentData<Blood>(killer, out var killerBlood)){
                if (!GuidToBloodType(killerBlood.BloodType, true, out killerBloodType)) return;
            }
            else {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer does not have blood: Killer ({killer}), Victim ({victim})");
                return; 
            }

            GlobalMasterySystem.MasteryType victimBloodType;
            float victimBloodQuality;
            bool isVBlood;
            if (_em.TryGetComponentData<BloodConsumeSource>(victim, out var victimBlood)) {
                victimBloodQuality = victimBlood.BloodQuality;
                if (!GuidToBloodType(victimBlood.UnitBloodType, false, out victimBloodType)) return;
                isVBlood = Helper.IsVBlood(victimBlood);
                
                // If the killer is consuming the target and it is not VBlood, the blood type will be changing to the victims.
                if (!isVBlood && !killOnly) killerBloodType = victimBloodType;
            }
            else
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"victim does not have blood: Killer ({killer}), Victim ({victim}");
                return;
            }

            if (killerBloodType == GlobalMasterySystem.MasteryType.None)
            {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"killer has frail blood, not modifying: Killer ({killer}), Victim ({victim})");
                if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                {
                    Output.SendMessage(killerUserEntity, L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorBlood));
                }
                return;
            }
            
            var bld = Database.PlayerMastery[steamID];
            var bloodlineMastery = bld[killerBloodType];
            growthVal *= bloodlineMastery.Growth;
            
            if (MercilessBloodlines)
            {
                if (!isVBlood) // VBlood is allowed to boost all blood types
                {
                    if (killerBloodType != victimBloodType)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: Blood types are different: Killer ({Enum.GetName(killerBloodType)}), Victim ({Enum.GetName(victimBloodType)})");
                        if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                        {
                            var message =
                                L10N.Get(L10N.TemplateKey.BloodlineMercilessUnmatchedBlood);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                    
                    if (victimBloodQuality <= bloodlineMastery.Mastery)
                    {
                        Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                            $"merciless bloodlines exit: victim blood quality less than killer mastery: Killer ({bloodlineMastery.Mastery}), Victim ({victimBloodQuality})");
                        if (Database.PlayerLogConfig[steamID].LoggingBloodline)
                        {
                            var message =
                                L10N.Get(L10N.TemplateKey.BloodlineMercilessErrorWeak);
                            Output.SendMessage(killerUserEntity, message);
                        }
                        return;
                    }
                }

                var modifier = killOnly ? 0.4 : isVBlood ? VBloodMultiplier : 1.0;

                growthVal *= (1 + Math.Clamp((victimBloodQuality - bloodlineMastery.Mastery)/100, 0.0, 0.5))*5*modifier;
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info,
                    $"Merciless growth {GetBloodTypeName(killerBloodType)}: [{victimBloodQuality:F3},{bloodlineMastery.Mastery:F3},{growthVal:F3}]");
            }
            else if (isVBlood)
            {
                growthVal *= VBloodMultiplier;
            }
            else if (killOnly)
            {
                growthVal *= 0.4;
            }

            if (_em.HasComponent<PlayerCharacter>(victim))
            {
                var victimGear = _em.GetComponentData<Equipment>(victim);
                var bonusMastery = victimGear.ArmorLevel + victimGear.WeaponLevel + victimGear.SpellLevel;
                growthVal *= (1 + (bonusMastery * 0.01));
                
                Plugin.Log(LogSystem.Bloodline, LogLevel.Info, $"Bonus bloodline mastery {bonusMastery:F3}]");
            }

            growthVal *= 0.001 * MasteryGainMultiplier;
            GlobalMasterySystem.ModMastery(steamID, killerBloodType, growthVal);

            if (Database.PlayerLogConfig[steamID].LoggingBloodline)
            {
                var currentMastery = Database.PlayerMastery[steamID][killerBloodType].Mastery;
                var bloodTypeName = GetBloodTypeName(killerBloodType);
                var message =
                    L10N.Get(L10N.TemplateKey.MasteryGainOnKill)
                        .AddField("{masteryChange}", $"{growthVal:+##.###;-##.###;0}")
                        .AddField("{masteryType}", bloodTypeName)
                        .AddField("{currentMastery}", $"{currentMastery:F3}");
                Output.SendMessage(killerUserEntity, message);
            }
        }

        public static GlobalMasterySystem.MasteryType BloodMasteryType(Entity entity)
        {
            var bloodType = GlobalMasterySystem.MasteryType.None;
            if (_em.TryGetComponentData<Blood>(entity, out var entityBlood))
            {
                GuidToBloodType(entityBlood.BloodType, true, out bloodType);
            }
            return bloodType;
        }

        private static bool GuidToBloodType(PrefabGUID guid, bool isKiller, out GlobalMasterySystem.MasteryType bloodType)
        {
            bloodType = GlobalMasterySystem.MasteryType.None;
            if(!Enum.IsDefined(typeof(GlobalMasterySystem.MasteryType), guid.GuidHash)) {
                Plugin.Log(LogSystem.Bloodline, LogLevel.Warning, $"Bloodline not found for guid {guid.GuidHash}. isKiller ({isKiller})", true);
                return false;
            }

            bloodType = (GlobalMasterySystem.MasteryType)guid.GuidHash;
            return true;
        }

        public static string GetBloodTypeName(GlobalMasterySystem.MasteryType type)
        {
            return Enum.GetName(type);
        }
    }
}
