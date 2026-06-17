using Duckov.Quests.Tasks;
using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace QuestItemRequirementsDisplay
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public readonly string ModName = "QuestItemRequirementsDisplay";
        public readonly string Version = "0.6";

        private Harmony? harmony;

        private Item? _currentItem = null;
        private bool _isDetailShown = false;

        TextMeshProUGUI? _text = null;
        TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    _text = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
                }
                return _text!;
            }
        }
        void Awake()
        {
            Debug.Log("DisplayRequiredItemCount Loaded!!!");
        }
        void OnDestroy()
        {
            if (_text != null)
                Destroy(_text);
        }
        void OnEnable()
        {
            GetRequiredItemAmount.SubscribeRequirementEvents();
            ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta += OnSetupMeta;
            harmony = new Harmony("DisplayRequiredItemCount");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        void OnDisable()
        {
            ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta -= OnSetupMeta;
            GetRequiredItemAmount.UnsubscribeRequirementEvents();
        }
        private void OnSetupMeta(ItemHoveringUI uI, ItemMetaData data)
        {
            Text.gameObject.SetActive(false);
        }

        void Update()
        {
#if DEBUG
            // Run tests when Shift + P is pressed
            if (Input.GetKey(KeyCode.P) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) Test.RunTests();
#endif
            // Return if detail is already shown or no current item or text is not active
            if (_isDetailShown || _currentItem == null || !Text.gameObject.activeSelf) return;

            // Update the UI if shift is held
            var isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShiftHeld) UpdateItemUI(true, _currentItem);
        }

        private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item)
        {
            _currentItem = item;
            _isDetailShown = false;

            if (item == null)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            // Setup the text UI
            Text.gameObject.SetActive(true);
            Text.transform.SetParent(uiInstance.LayoutParent);
            Text.transform.localScale = Vector3.one;
            Text.fontSize = 20f;

            // Check if shift is held
            var isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            UpdateItemUI(isShiftHeld, item);
        }

        /// <summary>
        /// Update the item UI with required item amount.
        /// </summary>
        /// <param name="isShiftHeld"></param>
        /// <param name="item"></param>
        private void UpdateItemUI(bool isShiftHeld, Item item)
        {
            _isDetailShown = isShiftHeld;

            // Clear previous text
            Text.text = "";
#if DEBUG
            Text.text += "\n <color=red><size=25>---------- Debug Build ----------<size=25></color>";
#endif

            // Get required item information
            (var requiredQuestText, var requiredQuestItemAmount) = GetRequiredQuestText(item);
            (var requiredSubmitQuestText, var requiredSubmittingQuestItemAmount) = GetRequiredSubmittingQuestText(item);
            (var requiredUseQuestText, var requiredUseQuestItemAmount) = GetRequiredUseQuestText(item);
            (var requiredPerkText, var requiredPerkItemAmount) = GetRequiredPerkText(item);
            (var requiredBuildingText, var requiredBuildingItemAmount) = GetRequiredBuildingText(item);

            // Calculate total required item amount
            var totalRequiredItemAmount = requiredQuestItemAmount + requiredSubmittingQuestItemAmount + requiredUseQuestItemAmount + requiredPerkItemAmount + requiredBuildingItemAmount;
            if (totalRequiredItemAmount == 0)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            // Get total required item amount text
            var totalItemAmount = GetItemAmount.GetTotalItemAmount(item.TypeID);

            // Determine color based on whether the player has enough items
            var colorOfTotalitemAmount = totalItemAmount >= totalRequiredItemAmount ? "green" : "red";

            // Show Text [Total required amount of this item: N]
            var totalRequiredItemAmountText = LocalizedText.Get(nameof(totalRequiredItemAmount));
            Text.text += $"{totalRequiredItemAmountText} <color={colorOfTotalitemAmount}>{totalItemAmount}</color> / {totalRequiredItemAmount}";

            // If shift is held, show detailed information
            if (isShiftHeld)
            {
                Text.text += requiredQuestText;
                Text.text += requiredSubmitQuestText;
                Text.text += requiredUseQuestText;
                Text.text += requiredPerkText;
                Text.text += requiredBuildingText;
            }
            else
            {
                // Display text: ----- Press Shift -----
                Text.text += $"\n\t<color=yellow><size=17>----- {LocalizedText.Get("pressShift", false)} -----<size=17></color>";
            }


            //// Show item amounts
            //Text.text += "\n";
            //Text.text += $"\n In Character Inventory: {itemAmountInCharacterInventory}";
            //Text.text += $"\n In Player Storage: {itemAmountInPlayerStorage}";
            //Text.text += $"\n In Inventory Items: {itemAmountInInventoryItems}";
        }

        /// <summary>
        /// Get the display text for quests that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public (string, int) GetRequiredQuestText(Item item)
        {
            var text = string.Empty;
            var amount = 0;
            var requiredQuests = GetRequiredItemAmount.GetRequiredQuests(item);
            if (requiredQuests.Count == 0) return (text, amount);

            var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => $"{x.RequiredItemCount}  -  {x.DisplayName}"));
#if DEBUG
            questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => $"{x.RequiredItemCount}  -  {x.DisplayName}  -  Id: {x.ID}"));
#endif
            amount = requiredQuests.Sum(x => x.RequiredItemCount);
            text = LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
            text += $"\n\t{questDisplayNames}";

            return (text, amount);
        }

        /// <summary>
        /// Get the display text for quests that require submitting the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public (string, int) GetRequiredSubmittingQuestText(Item item)
        {
            var text = string.Empty;
            var amount = 0;
            var requiredSubmitItems = GetRequiredItemAmount.GetRequiredSubmitItems(item);

            if (requiredSubmitItems.Count == 0) return (text, amount);

            text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
            foreach (var submitRequirement in requiredSubmitItems)
            {
                var questName = submitRequirement.Task != null && submitRequirement.Task.Master != null
                    ? submitRequirement.Task.Master.DisplayName
                    : submitRequirement.QuestName;
                text += $"\n\t{submitRequirement.Remaining}  -  {questName}";
                amount += submitRequirement.Remaining;
            }

            return (text, amount);
        }

        /// <summary>
        /// Get the display text for quests that require using the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public (string, int) GetRequiredUseQuestText(Item item)
        {
            var text = string.Empty;
            var amount = 0;
            var requiredUseItems = GetRequiredItemAmount.GetRequiredUseItems(item);
            if (requiredUseItems.Count == 0) return (text, amount);

            text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
            foreach (var useRequirement in requiredUseItems)
            {
                var questName = useRequirement.Task != null && useRequirement.Task.Master != null
                    ? useRequirement.Task.Master.DisplayName
                    : useRequirement.QuestName;
                text += $"\n\t{useRequirement.Remaining}  -  {questName}";
                amount += useRequirement.Remaining;
            }

            return (text, amount);
        }

        /// <summary>
        /// Get the display text for perks that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public (string, int) GetRequiredPerkText(Item item)
        {
            var text = string.Empty;
            var amount = 0L;
            var requiredPerkEntries = GetRequiredItemAmount.GetRequiredPerkEntries(item);
            if (requiredPerkEntries.Count == 0) return (text, (int)amount);

            text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
            foreach (var entry in requiredPerkEntries)
            {
                text += $"\n\t{entry.Amount}  -  {entry.PerkTreeName}/{entry.PerkName}";
                amount += entry.Amount;
            }

            return (text, (int)amount);
        }

        /// <summary>
        /// Get the display text for buildings that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public (string, int) GetRequiredBuildingText(Item item)
        {
            var text = string.Empty;
            var amount = 0L;
            var requiredBuildings = GetRequiredItemAmount.GetRequiredBuildings(item);
            if (requiredBuildings.Count == 0) return (text, (int)amount);

            text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
            foreach (var entry in requiredBuildings)
            {
                text += $"\n\t{entry.Amount}  -  {entry.BuildingName}";
                amount += entry.Amount;
            }

            return (text, (int)amount);
        }
    }
}
