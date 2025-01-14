﻿using EquippableItemIcons.API;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilityStuffs;
using UWE;
using WarpChip.Monobehaviours;

namespace WarpChip
{
    internal class WarpChipFunction : MonoBehaviour
    {
        private Player player;

        private static readonly FMODAsset teleportSound = Utility.GetFmodAsset("event:/creature/warper/portal_open");

        private float teleportDistanceOutside => Mathf.Clamp(QMod.config.DefaultWarpDistanceOutside, 0, MaxTeleportDistance);
        private float teleportDistanceInside => Mathf.Clamp(QMod.config.DefaultWarpDistanceInside, 0, MaxTeleportDistance);
        private float teleportCooldown => Mathf.Clamp(QMod.config.DefaultWarpCooldown, 0.1f, 20);

        private const float MaxTeleportDistance = 25;
        private const float teleportWallOffset = 1;//used so that you don't teleport partially inside of a wall, puts you slightly away from the wall

        public ActivatedEquippableItem itemIcon;
        public ChargableEquippableItem chargingIcon;
        //public bool UpgradedItemEquipped = false;
        public int FramesSinceCheck = 0;
        bool justTeleportedToBase = false;

        public void Awake()
        {
            SetUpIcons();

            player = GetComponent<Player>();
        }
        public void SetUpIcons()
        {
            itemIcon = new ActivatedEquippableItem("WarpChipIcon", ImageUtils.LoadSpriteFromFile(Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets"), "WarpChipIconRotate.png")), WarpChipItem.thisTechType);
            itemIcon.DetailedActivate += TryTeleport;
            itemIcon.activateKey = QMod.config.ControlKey;
            itemIcon.MaxCharge = 5;
            itemIcon.ChargeRate = itemIcon.MaxCharge / teleportCooldown;
            itemIcon.DrainRate = 0;
            itemIcon.ActivateSound = teleportSound;
            itemIcon.DeactivateSound = null;
            itemIcon.DetailedCanActivate += CanActivate;
            itemIcon.OnKeyDown = false;
            itemIcon.itemTechTypes.Add(UltimateWarpChip.thisTechType, EquipmentType.Chip);
            itemIcon.activationType = ActivatedEquippableItem.ActivationType.OnceOff;
            itemIcon.AutoIconFade = false;
            Registries.RegisterHudItemIcon(itemIcon);

            chargingIcon = new ChargableEquippableItem("WarpChargeIcon", null, WarpChipItem.thisTechType);
            chargingIcon.ChargingReleasedSound = teleportSound;
            chargingIcon.ChargingStartSound = null;
            chargingIcon.itemTechTypes.Add(UltimateWarpChip.thisTechType, EquipmentType.Chip);
            chargingIcon.ShouldMakeIcon = false;
            chargingIcon.activateKey = QMod.config.ControlKey;
            chargingIcon.AutoIconFade = false;
            chargingIcon.IsIconActive += () => false;
            chargingIcon.ReleasedCharging += ReturnToBase;
            chargingIcon.MinChargeRequiredToTrigger = chargingIcon.MaxCharge;
            chargingIcon.AutoReleaseOnMaxCharge = true;

            chargingIcon.container = itemIcon.container;
            chargingIcon.itemIconObject = itemIcon.itemIconObject;
            chargingIcon.itemIcon = itemIcon.itemIcon;

            Registries.RegisterHudItemIcon(chargingIcon);


        }
        public void Update()
        {
            if (chargingIcon.charge >= 5f) chargingIcon.UpdateFill();
            else itemIcon.UpdateFill();
        }

        public void TryTeleport(List<TechType> techTypes)
        {
            if(justTeleportedToBase)
            {
                justTeleportedToBase = false;
                return;
            }
            if(player != null && !player.isPiloting && player.mode == Player.Mode.Normal)
            {
                Teleport(techTypes.Contains(UltimateWarpChip.thisTechType));
            }
        }

        public void Teleport(bool ultimateChipEquipped)
        {
            float maxDistance = player.IsInside() ? teleportDistanceInside : teleportDistanceOutside;

            float distance = maxDistance;

            if (Targeting.GetTarget(player.gameObject, maxDistance, out var _, out float wallDistance))
            {
                distance = wallDistance - teleportWallOffset;
            }

            Transform aimingTransform = player.camRoot.GetAimingTransform();
            player.SetPosition(player.transform.position + aimingTransform.forward * distance);

            CoroutineHost.StartCoroutine(TeleportFX());

            if (itemIcon != null)
            {
                if (!ultimateChipEquipped)
                    itemIcon.charge -= Mathf.Lerp(0f, itemIcon.MaxCharge, (100f / (maxDistance / distance)) / 100f);
                else
                    itemIcon.charge -= Mathf.Lerp(0f, itemIcon.MaxCharge, (100f / (maxDistance / distance)) / 100f) / 2f;
            }
        }

        public static IEnumerator TeleportFX(float delay = 0.25f, bool setCinematicMode = false)
        {
            if (setCinematicMode) Player.main.cinematicModeActive = true;
            TeleportScreenFXController fxController = MainCamera.camera.GetComponent<TeleportScreenFXController>();
#if SN
            fxController.StartTeleport();
#else
            fxController.StartTeleport(null);//fucking WHY?????? the argument isnt even used!
#endif
                yield return new WaitForSeconds(delay);

            if (setCinematicMode) Player.main.cinematicModeActive = false;
            fxController.StopTeleport();
        }
        public static IEnumerator TeleportFXWorld(bool setCinematicMode = false)
        {
            yield return TeleportFX(1f, setCinematicMode);
            yield break;

            if (setCinematicMode) Player.main.cinematicModeActive = true;
            TeleportScreenFXController fxController = MainCamera.camera.GetComponent<TeleportScreenFXController>();
#if SN
            fxController.StartTeleport();
#else
            fxController.StartTeleport(null);
#endif
            yield return new WaitUntil(() => LargeWorldStreamer.main.IsWorldSettled());

            if (setCinematicMode) Player.main.cinematicModeActive = false;
            fxController.StopTeleport();
        }
        public bool CanActivate(List<TechType> techTypes)
        {
            if (!techTypes.Contains(UltimateWarpChip.thisTechType))
                return itemIcon.charge == itemIcon.MaxCharge && !Cursor.visible && player != null && !player.isPiloting && player.mode == Player.Mode.Normal;
            return itemIcon.charge >= itemIcon.MaxCharge / 2 && !Cursor.visible && player != null && !player.isPiloting && player.mode == Player.Mode.Normal;
        }

        public bool IsChargeIconActive()
        {
            return chargingIcon.charge > 5f;
        }
        public void ReturnToBase()
        {
            var ping = TelePingInstance.GetTelePing();
            if (ping)
            { 
                ping.Teleport();
                justTeleportedToBase = true;
                CoroutineHost.StartCoroutine(TeleportFXWorld(ping.ShouldUseCinematicMode));
                return;
            }



            if(player.currentSub && player.CheckSubValid(player.currentSub))
            {
                RespawnPoint respawn = player.currentSub.gameObject.GetComponentInChildren<RespawnPoint>();
                if (respawn)
                {
                    player.SetPosition(respawn.GetSpawnPosition());
                    justTeleportedToBase = true;
                    CoroutineHost.StartCoroutine(TeleportFXWorld(true));
                    return;
                }
            }

            if (player.lastValidSub && player.CheckSubValid(player.lastValidSub))
            {
                RespawnPoint respawn = player.lastValidSub.gameObject.GetComponentInChildren<RespawnPoint>();
                if (respawn)
                {
                    player.SetPosition(respawn.GetSpawnPosition());
                    player.SetCurrentSub(player.lastValidSub);
                    justTeleportedToBase = true; 
                    CoroutineHost.StartCoroutine(TeleportFXWorld(true));
                    return;
                }
            }
#if SN
            if(QMod.config.CanTeleportToLifepod)
            {
                if(player.lastEscapePod && (QMod.config.MaxDistanceToTeleportToLifepod == 0 || (player.lastEscapePod.transform.position - player.transform.position).magnitude <= QMod.config.MaxDistanceToTeleportToLifepod))
                {
                    ErrorMessage.AddMessage("Can't find safe base, teleporting to lifepod");
                    player.lastEscapePod.RespawnPlayer();
                    justTeleportedToBase = true;
                    CoroutineHost.StartCoroutine(TeleportFXWorld(true));
                    return;
                }
                else if(QMod.config.TeleportToLifepodOnOldSave && EscapePod.main && (QMod.config.MaxDistanceToTeleportToLifepod == 0 || (EscapePod.main.transform.position - player.transform.position).magnitude <= QMod.config.MaxDistanceToTeleportToLifepod))
                {
                    ErrorMessage.AddMessage("Can't find safe base, teleporting to lifepod");
                    EscapePod.main.RespawnPlayer();
                    justTeleportedToBase = true;
                    CoroutineHost.StartCoroutine(TeleportFXWorld(true));
                    return;
                }
            }
#endif
            ErrorMessage.AddMessage("Teleport failed, no safe location found");
        }
    }
}
