﻿using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if SN
using Sprite = Atlas.Sprite;
using RecipeData = SMLHelper.V2.Crafting.TechData;
#endif
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SMLHelper.V2.Utility;
using UnityEngine;
using System.Collections;

namespace SonarChip
{
    internal class SonarChipItem : Equipable
    {
        public static TechType thisTechType;

        public override string AssetsFolder => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets");

        public SonarChipItem() : base("SonarChip", "Sonar Chip", "Allows use of a sonar ping")
        {
            OnFinishedPatching += () =>
            {
                thisTechType = TechType;
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Chip;
        public override TechType RequiredForUnlock => TechType.BaseMapRoom;
        public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Equipment;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };
        public override float CraftingTime => 3f;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(AssetsFolder, "SonarChipIcon.png"));
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Magnetite, 2),
#if SN
                        new Ingredient(TechType.SpikePlantSeed, 1),
#else
                        new Ingredient(TechType.Beacon, 1),
#endif
                        new Ingredient(TechType.CopperWire, 1)
                    }
                )
            };
        }
#if SN1
        public override GameObject GetGameObject()
        {
            var prefab = CraftData.GetPrefabForTechType(TechType.MapRoomHUDChip);
            var obj = GameObject.Instantiate(prefab);
            return obj;
        }
#endif
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            var task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip);
            yield return task;
            gameObject.Set(task.GetResult());
        }
    }
}
