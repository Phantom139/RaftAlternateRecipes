using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace AlternateRecipes {

    /**
     * Harmony Patches
     **/

    [HarmonyPatch(typeof(CraftingMenu), "CraftItem")]
    internal class Patch_CraftingMenu_CraftItem {
        private static bool Prefix(CraftingMenu __instance) {
            if (__instance.selectedRecipeBox.ItemToCraft == null) {
                return true;
            }
            Network_Player player = RAPI.GetLocalPlayer();
            int selected = __instance.selectedRecipeBox.GetAdditionalData().selected_alternate;
            Item_Base itemToCraft = __instance.selectedRecipeBox.ItemToCraft;
            if (itemToCraft.settings_recipe.HasSkins) {
                itemToCraft = itemToCraft.settings_recipe.selectedSkin;
            }

            ItemInstance_Recipe r;
            if (__instance.selectedRecipeBox.GetAdditionalData().selected_alternate > 0) {
                // "Selected" starts at 0 for the "generic" recipe, so "Selected" of 1 refers to the alt-recipe in list index 0
                r = ModRecipes.get_alternates(itemToCraft)[selected - 1];
            }
            else {
                r = itemToCraft.settings_recipe;
            }

            if (!GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources) {
                player.Inventory.RemoveCostMultipleIncludeSecondaryInventories(r.NewCost);
            }
            player.Inventory.AddItem(itemToCraft.UniqueName, r.AmountToCraft);
            return false;
        }
    }

    [HarmonyPatch(typeof(SelectedRecipeBox), "Initialize")]
    static class Patch_SelectedRecipeBox_Initialize {
        static void Prefix(SelectedRecipeBox __instance) {
            if (__instance.GetAdditionalData().nextAlternateButton == null &&
                __instance.GetAdditionalData().prevAlternateButton == null) {
                // Locate the features in the window, we need to find the "bottom"
                var rTransform = __instance.GetComponent<RectTransform>();
                var cButton = rTransform.FindChildRecursively("Craft button").GetComponent<RectTransform>();
                var iCostList = rTransform.FindChildRecursively("ItemCostList").GetComponent<RectTransform>();
                // Create two new objects (These will be the buttons)
                var u1 = Object.Instantiate(cButton, cButton.parent, true);
                var u2 = Object.Instantiate(cButton, cButton.parent, true);
                u1.name = "Next Alt Recipe button";
                u2.name = "Prev Alt Recipe button";
                // Adjust the sizes.
                u1.sizeDelta = new Vector2(cButton.rect.width / 2, cButton.rect.height);
                u2.sizeDelta = new Vector2(cButton.rect.width / 2, cButton.rect.height);
                // Finalize the new buttons
                Object.DestroyImmediate(u1.GetComponentInChildren<I2.Loc.Localize>());
                Object.DestroyImmediate(u2.GetComponentInChildren<I2.Loc.Localize>());
                //u1.GetComponentInChildren<Image>().sprite = Main.asset.LoadAsset<Sprite>("CraftingMenu_Next");
                //u2.GetComponentInChildren<Image>().sprite = Main.asset.LoadAsset<Sprite>("CraftingMenu_Prev");
                u1.GetComponentInChildren<Text>().text = ">";
                u2.GetComponentInChildren<Text>().text = "<";
                __instance.GetAdditionalData().nextAlternateButton = u1.GetComponent<Button>();
                __instance.GetAdditionalData().prevAlternateButton = u2.GetComponent<Button>();
                __instance.GetAdditionalData().nextAlternateButton.onClick = new Button.ButtonClickedEvent();
                __instance.GetAdditionalData().nextAlternateButton.onClick.AddListener(delegate {
                    Main.NextAlternButton_OnClick(__instance);
                });
                __instance.GetAdditionalData().prevAlternateButton.onClick = new Button.ButtonClickedEvent();
                __instance.GetAdditionalData().prevAlternateButton.onClick.AddListener(delegate {
                    Main.PrevAlternButton_OnClick(__instance);
                });
                var b1 = __instance.GetAdditionalData().nextAlternateButton.gameObject.AddComponent<Main.AlternateButtonUpdater>();
                b1.button = __instance.GetAdditionalData().nextAlternateButton;
                b1.recipeBox = __instance;
                var b2 = __instance.GetAdditionalData().prevAlternateButton.gameObject.AddComponent<Main.AlternateButtonUpdater>();
                b2.button = __instance.GetAdditionalData().nextAlternateButton;
                b2.recipeBox = __instance;

                GameObject container = new GameObject("Alt Button Container", typeof(RectTransform));
                RectTransform rect = container.GetComponent<RectTransform>();
                rect.transform.SetParent(iCostList.gameObject.transform);
                rect.transform.localPosition = new Vector3(0, 0, 0);

                b2.transform.SetParent(container.transform);
                b1.transform.SetParent(container.transform);

                b2.transform.localPosition = new Vector3(0, 0, 0);
                b1.transform.localPosition = new Vector3((float)((cButton.rect.width / 2) * 1.3), 0, 0);

                var s = cButton.rect.height * 1.15f;
                rTransform.offsetMin -= new Vector2(0, s);
            }
        }
    }

    [HarmonyPatch(typeof(SelectedRecipeBox), "DisplayRecipe")]
    internal class Patch_SelectedRecipeBox_DisplayRecipe {
        static void Postfix(SelectedRecipeBox __instance) {
            __instance.GetAdditionalData().selected_alternate = 0;
            var nab = __instance.GetAdditionalData().nextAlternateButton;
            var pab = __instance.GetAdditionalData().prevAlternateButton;

            // Check the selected recipe, do we need alternate buttons?
            if (ModRecipes.has_alternates(__instance.selectedRecipeItem)) {
                nab.interactable = true;
                pab.interactable = true;
            }
            else {
                nab.interactable = false;
                pab.interactable = false;
            }

            // Reposition the buttons to be at the end of the recipe list
            var rTransform = __instance.GetComponent<RectTransform>();
            var aContainer = rTransform.FindChildRecursively("Alt Button Container").GetComponent<RectTransform>();
            aContainer.transform.SetAsLastSibling();

            // Menu Re-Open? In this case we reset the alternate to zero, so we also need to update the CostCollection instance as well.
            //  This is all done in the pushBoxUpdates() method in PhantomsMod.Main(), so just call it here to save some code.
            Main.pushBoxUpdates(__instance);
        }
    }

    [HarmonyPatch(typeof(SelectedRecipeBox), "DisplaySkin")]
    internal class Patch_SelectedRecipeBox_DisplaySkin {
        static void Postfix(SelectedRecipeBox __instance) {
            __instance.GetAdditionalData().selected_alternate = 0;
            var nab = __instance.GetAdditionalData().nextAlternateButton;
            var pab = __instance.GetAdditionalData().prevAlternateButton;

            // Check the selected recipe, do we need alternate buttons?
            if (ModRecipes.has_alternates(__instance.selectedRecipeItem.settings_recipe.selectedSkin)) {
                nab.interactable = true;
                pab.interactable = true;
            }
            else {
                nab.interactable = false;
                pab.interactable = false;
            }

            // Reposition the buttons to be at the end of the recipe list
            var rTransform = __instance.GetComponent<RectTransform>();
            var aContainer = rTransform.FindChildRecursively("Alt Button Container").GetComponent<RectTransform>();
            aContainer.transform.SetAsLastSibling();
        }
    }

    [HarmonyPatch(typeof(SelectedRecipeBox), "Update")]
    static class Patch_SelectedRecipeBox_Update {
        static bool Prefix(SelectedRecipeBox __instance) {
            // If either of the original "fail-clauses" on Update() would be called, just halt execution of the Prefix.
            if (__instance.ItemToCraft == null || CanvasHelper.ActiveMenu != MenuType.Inventory) {
                return true;
            }
            // Check if we don't need to do anything here...
            int alt = __instance.GetAdditionalData().selected_alternate;
            Item_Base selectedItem = __instance.ItemToCraft;
            if (selectedItem.settings_recipe.HasSkins) {
                selectedItem = selectedItem.settings_recipe.selectedSkin;
            }
            if (alt == 0 || !ModRecipes.has_alternates(selectedItem)) {
                return true;
            }
            // If we've reached this point, we're using an alternate recipe and must maintain the UI with the alternate
            ItemInstance_Recipe r;
            if (alt > 0) {
                // "Selected" starts at 0 for the "generic" recipe, so "Selected" of 1 refers to the alt-recipe in list index 0
                r = ModRecipes.get_alternates(selectedItem)[alt - 1];
            }
            else {
                r = selectedItem.settings_recipe;
            }
            CostCollection c = Traverse.Create(__instance).Field("costCollection").GetValue() as CostCollection;
            c.ShowCost(r.NewCost);
            __instance.craftButton.interactable = c.MeetsRequirements();
            return false;
        }
    }

    /**
    [HarmonyPatch(typeof(CostCollection), "ShowCost", new Type[] { typeof(CostMultiple[]) })]
    internal class Patch_CostCollection_ShowCost {
        static void Prefix(CostMultiple[] cost) {
            Debug.Log("Calling ShowCost()");
            for (int i = 0; i < cost.Length; i++) {
                Debug.Log(cost[i].amount + "x " + cost[i].items[0].UniqueName);
            }
        }
    }
    **/

}