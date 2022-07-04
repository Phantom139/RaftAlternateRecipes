using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace AlternateRecipes {

    /**
     * Class Extensions
     **/
    [Serializable]
    public class SelectedRecipeBox_AdditionalData {
        public int selected_alternate;
        public Button nextAlternateButton;
        public Button prevAlternateButton;

        public SelectedRecipeBox_AdditionalData() {
            selected_alternate = 0;
            nextAlternateButton = null;
            prevAlternateButton = null;
        }
    }

    public static class SelectedRecipeBox_Extension {
        private static readonly ConditionalWeakTable<SelectedRecipeBox, SelectedRecipeBox_AdditionalData> data =
            new ConditionalWeakTable<SelectedRecipeBox, SelectedRecipeBox_AdditionalData>();

        public static SelectedRecipeBox_AdditionalData GetAdditionalData(this SelectedRecipeBox item) {
            return data.GetOrCreateValue(item);
        }

        public static void AddData(this SelectedRecipeBox item, SelectedRecipeBox_AdditionalData value) {
            try {
                data.Add(item, value);
            }
            catch (Exception) { }
        }
    }
}