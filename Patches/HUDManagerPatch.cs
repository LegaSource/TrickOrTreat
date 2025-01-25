using HarmonyLib;
using TrickOrTreat.Managers;
using UnityEngine;

namespace TrickOrTreat.Patches
{
    internal class HUDManagerPatch
    {
        public static GameObject halloweenCandyImage;

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Start))]
        [HarmonyPostfix]
        private static void StartHUDManager()
        {
            if (!ConfigManager.isHalloweenCandyIconOn.Value) return;

            Transform parent = GameObject.Find("Systems/UI/Canvas/Panel/GameObject/PlayerScreen").transform;
            halloweenCandyImage = Object.Instantiate(TrickOrTreat.halloweenCandySprite, Vector3.zero, Quaternion.identity);
            halloweenCandyImage.transform.localPosition = Vector3.zero;

            RectTransform rectTransform = halloweenCandyImage.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, worldPositionStays: false);
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(ConfigManager.halloweenCandyPosX.Value, ConfigManager.halloweenCandyPosY.Value);
            rectTransform.sizeDelta = new Vector2(40f, 40f);

            SetActive(false);
        }

        public static void SetActive(bool enable)
        {
            if (!ConfigManager.isHalloweenCandyIconOn.Value || halloweenCandyImage == null) return;
            halloweenCandyImage.SetActive(enable);
        }
    }
}
