﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sprite = Atlas.Sprite;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;

namespace ArmorSuit
{
    internal class ArmorSuitItem : Equipable
    {
        public static TechType thisTechType;
        public static Sprite sprite = SpriteManager.Get(TechType.UltraGlideFins);
        public override string AssetsFolder => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets");

        public ArmorSuitItem() : base("ArmorSuitItem", "Armor Suit", "A high tech adaptive suit which gives high damage reduction to a specific damage type")
        {
            OnFinishedPatching += () =>
            {
                thisTechType = TechType;
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Body;
        
        public override TechType RequiredForUnlock => TechType.ReinforcedDiveSuit;
        
        public override TechGroup GroupForPDA => TechGroup.Personal;
        
        public override TechCategory CategoryForPDA => TechCategory.Equipment;
        
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };
        
        public override float CraftingTime => 3f;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(ArmorSuitMono.AssetsFolder, "ArmorSuit.png"));
        }

        protected override TechData GetBlueprintRecipe()
        {
            return new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.UraniniteCrystal, 3),
                        new Ingredient(TechType.ToyCar, 1),
                        new Ingredient(TechType.UltraGlideFins, 1)
                    }
                ),
                LinkedItems = new List<TechType>() { ArmorGlovesItem.techType }
            };
        }

        public override GameObject GetGameObject()
        {
            var prefab = CraftData.GetPrefabForTechType(TechType.ReinforcedDiveSuit);
            var obj = GameObject.Instantiate(prefab);
            return obj;
        }
    }
}
