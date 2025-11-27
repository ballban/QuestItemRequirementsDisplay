using SodaCraft.Localizations;
using System.Collections.Generic;
using UnityEngine;

namespace QuestItemRequirementsDisplay
{
    public static class LocalizedText
    {
        /// <summary>
        /// Dictionary of localized strings for different languages.
        /// </summary>
        public static readonly Dictionary<string, string> Dic = new Dictionary<string, string>
        {
            { "GetRequiredQuestText_ChineseSimplified", "需要准备该物品的任务:" },
            { "GetRequiredQuestText_ChineseTraditional", "需要準備該物品的任務:" },
            { "GetRequiredQuestText_Japanese", "このアイテムが必要なクエスト:" },
            { "GetRequiredQuestText_Korean", "이아이템이 필요한 퀘스트:" },
            { "GetRequiredQuestText_Russian", "Квесты, требующие предмет:" },
            { "GetRequiredQuestText_German", "Item wird benötigt für Quest:" },
            { "GetRequiredQuestText_Default", "Quests required this item:" },

            { "GetRequiredSubmittingQuestText_ChineseSimplified", "需要提交该物品的任务:" },
            { "GetRequiredSubmittingQuestText_ChineseTraditional", "需要提交該物品的任務:" },
            { "GetRequiredSubmittingQuestText_Japanese", "このアイテム納品が必要なクエスト:" },
            { "GetRequiredSubmittingQuestText_Korean", "이아이템 제출이 필요한 퀘스트:" },
            { "GetRequiredSubmittingQuestText_Russian", "Квесты на сдачу предмета:" },
            { "GetRequiredSubmittingQuestText_German", "Item kann für folgende Quests versendet werden:" },
            { "GetRequiredSubmittingQuestText_Default", "Quests required submit this item:" },

            { "GetRequiredUseQuestText_ChineseSimplified", "需要使用该物品的任务:" },
            { "GetRequiredUseQuestText_ChineseTraditional", "需要使用該物品的任務:" },
            { "GetRequiredUseQuestText_Japanese", "このアイテムの使用が必要なクエスト:" },
            { "GetRequiredUseQuestText_Korean", "이 아이템 사용이 필요한 퀘스트:" },
            { "GetRequiredUseQuestText_Russian", "Квесты на использование предмета:" },
            { "GetRequiredUseQuestText_German", "Item kann für folgende Quests benutzt werden:" },
            { "GetRequiredUseQuestText_Default", "Quests required use this item:" },

            { "GetRequiredPerkText_ChineseSimplified", "需要该物品解锁的天赋:" },
            { "GetRequiredPerkText_ChineseTraditional", "需要該物品解鎖的天賦:" },
            { "GetRequiredPerkText_Japanese", "このアイテムが必要なスキル:" },
            { "GetRequiredPerkText_Korean", "이 아이템이 필요한 스킬:" },
            { "GetRequiredPerkText_Russian", "Перки, требующие предмет:" },
            { "GetRequiredPerkText_German", "Item wird für folgende Verbesserungen benötigt:" },
            { "GetRequiredPerkText_Default", "Perks required this item:" },

            { "GetRequiredBuildingText_ChineseSimplified", "需要该物品解锁的建筑:" },
            { "GetRequiredBuildingText_ChineseTraditional", "需要該物品解鎖的建築:" },
            { "GetRequiredBuildingText_Japanese", "このアイテムが必要な建物:" },
            { "GetRequiredBuildingText_Korean", "이 아이템이 필요한 건물:" },
            { "GetRequiredBuildingText_Russian", "Здания, требующие предмет:" },
            { "GetRequiredBuildingText_German", "Item wird für folgende Gebäude benötigt:" },
            { "GetRequiredBuildingText_Default", "Buildings required this item:" },

            { "totalRequiredItemAmount_ChineseSimplified", "总数/总需求:" },
            { "totalRequiredItemAmount_ChineseTraditional", "總數/總需求:" },
            { "totalRequiredItemAmount_Japanese", "合計/必要数:" },
            { "totalRequiredItemAmount_Korean", "총합/필요개수:" },
            { "totalRequiredItemAmount_Russian", "Всего/Требуется:" },
            { "totalRequiredItemAmount_German", "Gesamt/Benötigte Menge:" },
            { "totalRequiredItemAmount_Default", "Total/Required amount:" },

            { "pressShift_ChineseSimplified", "按下 Shift" },
            { "pressShift_ChineseTraditional", "按下 Shift" },
            { "pressShift_Japanese", "Shift キー" },
            { "pressShift_Korean", "Shift 버튼" },
            { "pressShift_Russian", "Нажмите Shift" },
            { "pressShift_German", "Drücke Umschalt" },
            { "pressShift_Default", "Press Shift" },
        };

        /// <summary>
        /// Get the localized string for the given key and language.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(string key, bool isChangLine = true)
        {
            var currentLanguage = LocalizationManager.CurrentLanguage;
            var systemLanguageStr = currentLanguage switch
            {
                SystemLanguage.Chinese => "ChineseSimplified",
                SystemLanguage.ChineseSimplified => currentLanguage.ToString(),
                SystemLanguage.ChineseTraditional => currentLanguage.ToString(),
                SystemLanguage.Japanese => currentLanguage.ToString(),
                SystemLanguage.Korean => currentLanguage.ToString(),
                SystemLanguage.Russian => currentLanguage.ToString(),
                SystemLanguage.German => currentLanguage.ToString(),
                _ => "Default",
            };
            var text = Dic.TryGetValue($"{key}_{systemLanguageStr}", out var value) ? value : "";
            text = isChangLine ? $"\n{text}" : text;
            return text;
        }
    }
}
