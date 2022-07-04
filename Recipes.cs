using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.IO;

namespace AlternateRecipes {

    public static class ModRecipes {

        public static Dictionary<int, List<ItemInstance_Recipe>> alternate_recipes = new Dictionary<int, List<ItemInstance_Recipe>>();

        /**
         ModifyRecipe(): Modifies a recipe for a vanilla raft item
            Params:
                * ItemBase: The item to be crafted
                * int: The number of items to be crafted by this recipe
                * CraftingCategory: Which category the recipe falls under
                * bool: Whether or not the recipe is unlocked at the start of the game (Defaults to True)
                * CostMultiple[]: The array of items required for this recipe
            Returns:
                * ItemInstance_Recipe: The recipe instance created by this method
        **/
        public static ItemInstance_Recipe ModifyRecipe(Item_Base resulting_item,
                                                        int amountCrafted,
                                                        CraftingCategory category,
                                                        bool unlockedByDefault = true,
                                                        params CostMultiple[] pCosts) {
            Traverse.Create(resulting_item.settings_recipe).Field("newCostToCraft").SetValue(pCosts);
            Traverse.Create(resulting_item.settings_recipe).Field("learned").SetValue(unlockedByDefault);
            Traverse.Create(resulting_item.settings_recipe).Field("learnedFromBeginning").SetValue(unlockedByDefault);
            Traverse.Create(resulting_item.settings_recipe).Field("craftingCategory").SetValue(category);
            Traverse.Create(resulting_item.settings_recipe).Field("amountToCraft").SetValue(amountCrafted);

            return resulting_item.settings_recipe;
        }

        /**
         AddAlternateRecipe(): Adds an alternate recipe for the item
            Params:
                * ItemBase: The item that will have the alternate recipe added
                * int: How much of the item you get from this recipe
                * CostMultiple: The items needed to craft under this alternate recipe
        **/
        public static void AddAlternateRecipe(Item_Base resulting_item, int amountCrafted, params CostMultiple[] cost) {
            ItemInstance_Recipe newRecipe = new ItemInstance_Recipe(resulting_item.settings_recipe.CraftingCategory,
                                                                resulting_item.settings_recipe.Learned,
                                                                resulting_item.settings_recipe.LearnedFromBeginning,
                                                                resulting_item.settings_recipe.SubCategory,
                                                                resulting_item.settings_recipe.SubCategoryOrder);
            Traverse.Create(newRecipe).Field("newCostToCraft").SetValue(cost);
            Traverse.Create(newRecipe).Field("amountToCraft").SetValue(amountCrafted);

            List<ItemInstance_Recipe> RList;
            if (has_alternates(resulting_item)) {
                RList = alternate_recipes[resulting_item.UniqueIndex];
                RList.Add(newRecipe);
                alternate_recipes[resulting_item.UniqueIndex] = RList;
            }
            else {
                RList = new List<ItemInstance_Recipe>();
                RList.Add(newRecipe);
                alternate_recipes.Add(resulting_item.UniqueIndex, RList);
            }                 
        }

        public static bool has_alternates(Item_Base item) {
            if (!alternate_recipes.ContainsKey(item.UniqueIndex)) {
                return false;
            }
            return alternate_recipes[item.UniqueIndex].Count > 0;
        }

        public static int alternate_count(Item_Base item) {
            if (!has_alternates(item)) {
                return 0;
            }
            return alternate_recipes[item.UniqueIndex].Count;
        }

        public static List<ItemInstance_Recipe> get_alternates(Item_Base item) {
            if (!has_alternates(item)) {
                return null;
            }
            return alternate_recipes[item.UniqueIndex];
        }

        /**
          * 
          * Helper resources
         **/
        public static void DumpItemIDs() {
            SortedDictionary<int, string> itemList = new SortedDictionary<int, string>();

            foreach (Item_Base item in ItemManager.GetAllItems()) {
                itemList.Add(item.UniqueIndex, item.UniqueName);
            }

            using (StreamWriter writer = new StreamWriter("item_ids.txt")) {
                foreach (var item in itemList) {
                    writer.WriteLine(item.Key.ToString() + ": " + item.Value);
                }

                writer.WriteLine("C# Code For VanillaItems Struct\n\n");

                int curIdx = 0;
                foreach (var item in itemList) {
                    if (curIdx == 0) {
                        writer.WriteLine("// Pre-Story / Alphas");
                    }
                    else if (curIdx == 197) {
                        writer.WriteLine("// First Chapter");
                    }
                    else if (curIdx == 289) {
                        writer.WriteLine("// Second Chapter");
                    }
                    else if (curIdx == 360) {
                        writer.WriteLine("// Final Chapter / Renovation");
                    }
                    //Add future chapters as listed above.
                    writer.WriteLine("public static readonly Item_Base " + item.Value + " = ItemManager.GetItemByIndex(" + item.Key.ToString() + ");");
                    curIdx = item.Key;
                }
            }

        }

    }

}