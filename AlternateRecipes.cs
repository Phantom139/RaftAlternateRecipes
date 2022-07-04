using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;

namespace AlternateRecipes {

    public class Main : Mod {

        Harmony harmonyInstance;
        public static Main instance;

        public Main() : base() {
            instance = this;
        }

        public void Start() {
            // Init the mod recipes
            loadModRecipes();

            // Apply our Harmony patches to Raft
            //  Make sure this happens last such that everything that needs to be initalized ahead of time is done.
            (harmonyInstance = new Harmony("com.raftmodding.alternaterecipes")).PatchAll();

            // Phantom139: Whenever a new version of Raft is released, use this function call to generate a new item_ids.txt
            //  file which contains all the Item IDs to be added to the big structure below.
            //DumpItemIDs();

            Debug.Log("Alternate Recipes has been loaded!");
        }

        public void OnModUnload() {
            harmonyInstance.UnpatchAll(harmonyInstance.Id);

            Destroy(gameObject);

            Debug.Log("Alternate Recipes has been unloaded!");
        }

        public void loadModRecipes() {
            // Ingots => Scrap
            ModRecipes.ModifyRecipe(VanillaItems.Scrap, 5, CraftingCategory.Resources, true, new CostMultiple(new Item_Base[] { VanillaItems.MetalIngot }, 1));
            ModRecipes.AddAlternateRecipe(VanillaItems.Scrap, 10, new CostMultiple(new Item_Base[] { VanillaItems.CopperIngot }, 1));
            ModRecipes.AddAlternateRecipe(VanillaItems.Scrap, 20, new CostMultiple(new Item_Base[] { VanillaItems.TitaniumIngot }, 1));
            // Metal Ingot => 10 Nails
            ModRecipes.AddAlternateRecipe(VanillaItems.Nail, 10, new CostMultiple(new Item_Base[] { VanillaItems.MetalIngot }, 1));
        }

        /**
         * External Functions
        **/

        public static void PrevAlternButton_OnClick(SelectedRecipeBox box) {
            int currentAlt = box.GetAdditionalData().selected_alternate;
            int totalAlts = ModRecipes.alternate_count(box.selectedRecipeItem);

            currentAlt--;
            if (currentAlt < 0) {
                currentAlt = totalAlts;
            }

            box.GetAdditionalData().selected_alternate = currentAlt;
            pushBoxUpdates(box);
        }

        public static void NextAlternButton_OnClick(SelectedRecipeBox box) {
            int currentAlt = box.GetAdditionalData().selected_alternate;
            int totalAlts = ModRecipes.alternate_count(box.selectedRecipeItem);

            currentAlt++;
            if (currentAlt > totalAlts) {
                currentAlt = 0;
            }

            box.GetAdditionalData().selected_alternate = currentAlt;
            pushBoxUpdates(box);
        }

        public class AlternateButtonUpdater : MonoBehaviour {
            public Button button;
            public SelectedRecipeBox recipeBox;
            void Update() {
                button.interactable = ModRecipes.alternate_count(recipeBox.selectedRecipeItem) > 0;
            }
        }

        public static void pushBoxUpdates(SelectedRecipeBox b) {
            int alt = b.GetAdditionalData().selected_alternate;
            Item_Base selectedItem = b.selectedRecipeItem;
            if (selectedItem.settings_recipe.HasSkins) {
                selectedItem = selectedItem.settings_recipe.selectedSkin;
            }

            ItemInstance_Recipe r;
            if (alt > 0) {
                // "Selected" starts at 0 for the "generic" recipe, so "Selected" of 1 refers to the alt-recipe in list index 0
                r = ModRecipes.get_alternates(selectedItem)[alt - 1];
            }
            else {
                r = selectedItem.settings_recipe;
            }
            // Show the updated cost collection
            CostCollection c = Traverse.Create(b).Field("costCollection").GetValue() as CostCollection;
            c.ShowCost(r.NewCost);

            // Add the "multiplier text" if present.
            bool flag = r.AmountToCraft > 1;
            b.craftAmountText.gameObject.SetActive(flag);
            if (flag) {
                b.craftAmountText.text = "x" + r.AmountToCraft.ToString();
            }
            // Quick check to see if the "craft button" should be disabled or not.
            b.craftButton.interactable = c.MeetsRequirements();
        }

    }

}