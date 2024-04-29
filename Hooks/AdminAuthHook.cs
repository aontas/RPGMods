﻿using ProjectM;
using HarmonyLib;
using OpenRPG.Systems;

namespace OpenRPG.Hooks
{
    [HarmonyPatch(typeof(AdminAuthSystem), nameof(AdminAuthSystem.IsAdmin))]
    public static class IsAdmin_Patch
    {
        public static void Postfix(ulong platformId, ref bool __result)
        {
            if (PermissionSystem.isVIPSystem && PermissionSystem.isVIPWhitelist)
            {
                if (PermissionSystem.IsUserVIP(platformId))
                {
                    __result = true;
                }
            }
        }
    }
}
