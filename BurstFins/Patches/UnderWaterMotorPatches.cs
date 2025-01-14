﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace BurstFins.Patches
{
    [HarmonyPatch(typeof(UnderwaterMotor))]
    internal class UnderWaterMotorPatches
    {
        [HarmonyPatch(nameof(UnderwaterMotor.AlterMaxSpeed))]
        public static void Postfix(ref float __result)
        {
            var mono = Player.main.GetComponent<BurstFinsMono>();
            if (mono && mono.hudItemIcon != null && mono.hudItemIcon.active)
            {
                ErrorMessage.AddMessage($"Found mono, icon active, __result before: {__result}");
                var num = __result;
                num = num + 2.5f;
                num = num * 2f;
                __result = num;
                ErrorMessage.AddMessage($"__result after: {__result} from num: {num}");
            }
            else if(mono && mono.hudItemIcon != null && mono.hudItemIcon.equipped)
            {
                ErrorMessage.AddMessage($"Found mono, icon equipped, __result before: {__result}");
                __result = __result + 2.5f;
                ErrorMessage.AddMessage($"__result after: {__result}");
            }
        }
        [HarmonyPatch(nameof(UnderwaterMotor.UpdateMove))]
        [HarmonyPostfix]
        public static void MotorPostfix(UnderwaterMotor __instance, ref Vector3 __result)
        {
            var mono = Player.main.GetComponent<BurstFinsMono>();
            if (mono && mono.hudItemIcon != null && mono.hudItemIcon.active)
            {
                var num = __result;
                num = new Vector3(num.x * 1.025f, num.y * 1.025f, num.z * 1.025f);

                __instance.rb.velocity = num;
                __instance.vel = num;

                __result = num;
            }
        }
    }
}
