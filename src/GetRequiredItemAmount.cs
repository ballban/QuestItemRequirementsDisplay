using Duckov.Buildings;
using Duckov.PerkTrees;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace QuestItemRequirementsDisplay
{
    static public class GetRequiredItemAmount
    {
        // Trees to exclude from perk calculations:
        // - "Blueprint": weapon/blueprint unlocks; not considered character perk progression
        // - "PerkTree_Farming": farming-related tree currently out of scope
        private static readonly HashSet<string> ExcludedPerkTreeIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "Blueprint",
            "PerkTree_Farming",
        };

        public static List<Quest> TotalQuests = InitTotalQuests();

        /// <summary>
        /// Initialize the total quests list, excluding unimplemented quests and their children, as well as testing quests.
        /// </summary>
        /// <returns></returns>
        private static List<Quest> InitTotalQuests()
        {
            var unimplementedLevelRequirement = 999;
            var questCollection = GameplayDataSettings.QuestCollection
                .ConvertTo<List<Quest>>()
                .Where(q => q != null);

            // Build a parent-to-children mapping
            var parentToChildren = new Dictionary<int, List<int>>();
            var excludedQuests = new HashSet<int>();
            foreach (var quest in questCollection)
            {
                var parentIds = GetParentQuestIds(quest.ID);
                // First exclude quests with a high level requirement (unimplemented quests)
                if (quest.RequireLevel >= unimplementedLevelRequirement)
                {
                    excludedQuests.Add(quest.ID);
                }
                // Build parent-to-children mapping
                foreach (var parentId in parentIds)
                {
                    if (!parentToChildren.TryGetValue(parentId, out var children))
                        parentToChildren[parentId] = children = new List<int>();
                    children.Add(quest.ID);
                }
            }

            // Recursive function to exclude all children of a given quest
            void ExcludeChildren(int questId)
            {
                if (parentToChildren.TryGetValue(questId, out var children))
                {
                    foreach (var childId in children)
                    {
                        if (excludedQuests.Add(childId)) ExcludeChildren(childId);
                    }
                }
            }

            // Recursively exclude all children of excluded quests
            foreach (var questId in excludedQuests.ToList())
            {
                ExcludeChildren(questId);
            }

            var filteredQuests = questCollection
                // Filter out excluded quests
                .Where(q => !excludedQuests.Contains(q.ID))
                // Filter out testing quests
                .Where(q => !IsTestingObjectDisplayName(q.DisplayName))
                .ToList();

            return filteredQuests;
        }

        /// <summary>
        /// Get the parent quest IDs for the specified quest ID.
        /// </summary>
        /// <param name="questId"></param>
        /// <returns></returns>
        private static List<int> GetParentQuestIds(int questId)
        {
            var questRelation = GameplayDataSettings.QuestRelation;
            if (questRelation == null) return new List<int>();
            var parentQuestIds = questRelation.GetRequiredIDs(questId);
            return parentQuestIds;
        }

        /// <summary>
        /// Check if the object's display name is a testing name (starts and ends with '*')
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool IsTestingObjectDisplayName(string name)
        {
            return name.StartsWith("*") && name.EndsWith("*");
        }

        /// <summary>
        /// Get a list of quests that require the specified item to be prepared.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<Quest> GetRequiredQuests(Item item)
        {
            if (item == null) return new List<Quest>();
            RequirementCache.Ensure(item.TypeID);
            return RequirementCache.GetPreparedQuests(item.TypeID);
        }

        public struct RequiredPerk
        {
            public long Amount;
            public string PerkTreeName;
            public string PerkName;
        }

        public readonly struct SubmitItemRequirement
        {
            public SubmitItemRequirement(SubmitItems? task, int questId, string questName, int remaining)
            {
                Task = task;
                QuestId = questId;
                QuestName = questName;
                Remaining = remaining;
            }

            public SubmitItems? Task { get; }
            public int QuestId { get; }
            public string QuestName { get; }
            public int Remaining { get; }
        }

        public readonly struct UseItemRequirement
        {
            public UseItemRequirement(QuestTask_UseItem? task, int questId, string questName, int remaining)
            {
                Task = task;
                QuestId = questId;
                QuestName = questName;
                Remaining = remaining;
            }

            public QuestTask_UseItem? Task { get; }
            public int QuestId { get; }
            public string QuestName { get; }
            public int Remaining { get; }
        }
        /// <summary>
        /// Get a list of perks that require the specified item to unlock.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<RequiredPerk> GetRequiredPerkEntries(Item item)
        {
            if (item == null) return new List<RequiredPerk>();
            RequirementCache.Ensure(item.TypeID);
            return RequirementCache.GetPerkRequirements(item.TypeID);
        }

        /// <summary>
        /// Get a dictionary of SubmitItems tasks that require the specified item, along with the required amount.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<SubmitItemRequirement> GetRequiredSubmitItems(Item item)
        {
            if (item == null) return new List<SubmitItemRequirement>();
            RequirementCache.Ensure(item.TypeID);
            return RequirementCache.GetSubmitRequirements(item.TypeID);
        }

        public struct RequiredBuilding
        {
            public long Amount;
            public string BuildingName;
        }
        /// <summary>
        /// Get a list of buildings that require the specified item to unlock.
        /// WARNING: not UNLOCKED, but UNPLACED buildings
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static List<RequiredBuilding> GetRequiredBuildings(Item item)
        {
            if (item == null) return new List<RequiredBuilding>();
            RequirementCache.Ensure(item.TypeID);
            return RequirementCache.GetBuildingRequirements(item.TypeID);
        }

        /// <summary>
        /// Get quest that require the specified item, along with the required amount.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static List<UseItemRequirement> GetRequiredUseItems(Item item)
        {
            if (item == null) return new List<UseItemRequirement>();
            RequirementCache.Ensure(item.TypeID);
            return RequirementCache.GetUseRequirements(item.TypeID);
        }

        public static void EnsureItemRequirements(int itemTypeId)
        {
            if (itemTypeId <= 0) return;
            RequirementCache.Ensure(itemTypeId);
        }

        public static void SubscribeRequirementEvents()
        {
            RequirementCache.SubscribeEvents();
        }

        public static void UnsubscribeRequirementEvents()
        {
            RequirementCache.UnsubscribeEvents();
        }

        private sealed class ItemRequirementSnapshot
        {
            public List<Quest> PreparedQuests { get; set; } = new List<Quest>();
            public List<SubmitItemRequirement> SubmitRequirements { get; set; } = new List<SubmitItemRequirement>();
            public List<UseItemRequirement> UseRequirements { get; set; } = new List<UseItemRequirement>();
            public List<RequiredPerk> PerkRequirements { get; set; } = new List<RequiredPerk>();
            public List<RequiredBuilding> BuildingRequirements { get; set; } = new List<RequiredBuilding>();
        }

        private static class RequirementCache
        {
            private static readonly Dictionary<int, ItemRequirementSnapshot> Cache = new Dictionary<int, ItemRequirementSnapshot>();
            private static bool _isDirty = true;
            private static bool _eventsSubscribed;
            private static readonly List<PerkTree> SubscribedPerkTrees = new List<PerkTree>();
            private static readonly List<Perk> SubscribedPerks = new List<Perk>();

            public static void Ensure(int itemTypeId)
            {
                if (itemTypeId <= 0)
                {
                    return;
                }

                if (_isDirty)
                {
                    Cache.Clear();
                    _isDirty = false;
                }

                if (!Cache.TryGetValue(itemTypeId, out var snapshot))
                {
                    snapshot = BuildSnapshot(itemTypeId);
                    Cache[itemTypeId] = snapshot;
                }
            }

            public static List<Quest> GetPreparedQuests(int itemTypeId)
            {
                return Cache.TryGetValue(itemTypeId, out var snapshot)
                    ? new List<Quest>(snapshot.PreparedQuests)
                    : new List<Quest>();
            }

            public static List<SubmitItemRequirement> GetSubmitRequirements(int itemTypeId)
            {
                return Cache.TryGetValue(itemTypeId, out var snapshot)
                    ? new List<SubmitItemRequirement>(snapshot.SubmitRequirements)
                    : new List<SubmitItemRequirement>();
            }

            public static List<UseItemRequirement> GetUseRequirements(int itemTypeId)
            {
                return Cache.TryGetValue(itemTypeId, out var snapshot)
                    ? new List<UseItemRequirement>(snapshot.UseRequirements)
                    : new List<UseItemRequirement>();
            }

            public static List<RequiredPerk> GetPerkRequirements(int itemTypeId)
            {
                return Cache.TryGetValue(itemTypeId, out var snapshot)
                    ? new List<RequiredPerk>(snapshot.PerkRequirements)
                    : new List<RequiredPerk>();
            }

            public static List<RequiredBuilding> GetBuildingRequirements(int itemTypeId)
            {
                return Cache.TryGetValue(itemTypeId, out var snapshot)
                    ? new List<RequiredBuilding>(snapshot.BuildingRequirements)
                    : new List<RequiredBuilding>();
            }

            public static void SubscribeEvents()
            {
                if (_eventsSubscribed)
                {
                    return;
                }

                QuestManager.onQuestListsChanged += OnQuestListsChanged;
                QuestManager.OnTaskFinishedEvent += OnQuestTaskFinished;
                Quest.onQuestStatusChanged += OnQuestChanged;
                Quest.onQuestCompleted += OnQuestChanged;
                Quest.onQuestActivated += OnQuestChanged;

                BuildingManager.OnBuildingListChanged += OnBuildingListChanged;

                LevelManager.OnAfterLevelInitialized += OnLevelReady;
                if (LevelManager.LevelInited)
                {
                    RegisterPerkEvents();
                }

                _eventsSubscribed = true;
            }

            public static void UnsubscribeEvents()
            {
                if (!_eventsSubscribed)
                {
                    return;
                }

                QuestManager.onQuestListsChanged -= OnQuestListsChanged;
                QuestManager.OnTaskFinishedEvent -= OnQuestTaskFinished;
                Quest.onQuestStatusChanged -= OnQuestChanged;
                Quest.onQuestCompleted -= OnQuestChanged;
                Quest.onQuestActivated -= OnQuestChanged;

                BuildingManager.OnBuildingListChanged -= OnBuildingListChanged;

                LevelManager.OnAfterLevelInitialized -= OnLevelReady;

                UnregisterPerkEvents();

                _eventsSubscribed = false;
            }

            private static ItemRequirementSnapshot BuildSnapshot(int itemTypeId)
            {
                return new ItemRequirementSnapshot
                {
                    PreparedQuests = ComputePreparedQuests(itemTypeId),
                    SubmitRequirements = ComputeSubmitRequirements(itemTypeId),
                    UseRequirements = ComputeUseRequirements(itemTypeId),
                    PerkRequirements = ComputePerkRequirements(itemTypeId),
                    BuildingRequirements = ComputeBuildingRequirements(itemTypeId)
                };
            }

            private static List<Quest> ComputePreparedQuests(int itemTypeId)
            {
                var finishedIds = QuestManager.Instance != null
                    ? new HashSet<int>(QuestManager.Instance.HistoryQuests.Select(q => q.ID))
                    : new HashSet<int>();

                return TotalQuests
                    .Where(q => q != null && q.RequiredItemID == itemTypeId && !finishedIds.Contains(q.ID))
                    .ToList();
            }

            private static List<SubmitItemRequirement> ComputeSubmitRequirements(int itemTypeId)
            {
                var results = new List<SubmitItemRequirement>();
                var questManager = QuestManager.Instance;
                var requiredAmountRef = AccessTools.FieldRefAccess<int>(typeof(SubmitItems), "requiredAmount");
                var submittedAmountRef = AccessTools.FieldRefAccess<int>(typeof(SubmitItems), "submittedAmount");

                var activeQuests = questManager?.ActiveQuests ?? new List<Quest>();
                var activeQuestIds = new HashSet<int>(activeQuests.Where(q => q != null).Select(q => q.ID));

                foreach (var quest in activeQuests)
                {
                    if (quest?.Tasks == null) continue;

                    foreach (var submitItem in quest.Tasks.OfType<SubmitItems>())
                    {
                        if (submitItem.ItemTypeID != itemTypeId) continue;
                        if (submitItem.IsFinished()) continue;

                        var remaining = Math.Max(0, requiredAmountRef(submitItem) - submittedAmountRef(submitItem));
                        if (remaining <= 0) continue;

                        results.Add(new SubmitItemRequirement(
                            submitItem,
                            quest.ID,
                            quest.DisplayName,
                            remaining));
                    }
                }

                var finishedIds = questManager != null
                    ? new HashSet<int>(questManager.HistoryQuests.Select(q => q.ID))
                    : new HashSet<int>();

                foreach (var quest in TotalQuests)
                {
                    if (quest == null) continue;
                    if (finishedIds.Contains(quest.ID)) continue;
                    if (activeQuestIds.Contains(quest.ID)) continue;
                    if (quest.Tasks == null) continue;

                    foreach (var submitItem in quest.Tasks.OfType<SubmitItems>())
                    {
                        if (submitItem.ItemTypeID != itemTypeId) continue;
                        var required = requiredAmountRef(submitItem);
                        if (required <= 0) continue;

                        results.Add(new SubmitItemRequirement(
                            null,
                            quest.ID,
                            quest.DisplayName,
                            required));
                    }
                }

                return results;
            }

            private static List<UseItemRequirement> ComputeUseRequirements(int itemTypeId)
            {
                var results = new List<UseItemRequirement>();
                var questManager = QuestManager.Instance;
                var itemTypeRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "itemTypeID");
                var requireAmountRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "requireAmount");
                var usedAmountRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "amount");

                var activeQuests = questManager?.ActiveQuests ?? new List<Quest>();
                var activeQuestIds = new HashSet<int>(activeQuests.Where(q => q != null).Select(q => q.ID));

                foreach (var quest in activeQuests)
                {
                    if (quest?.Tasks == null) continue;

                    foreach (var useItem in quest.Tasks.OfType<QuestTask_UseItem>())
                    {
                        if (itemTypeRef(useItem) != itemTypeId) continue;
                        if (useItem.IsFinished()) continue;

                        var remaining = Math.Max(0, requireAmountRef(useItem) - usedAmountRef(useItem));
                        if (remaining <= 0) continue;

                        results.Add(new UseItemRequirement(
                            useItem,
                            quest.ID,
                            quest.DisplayName,
                            remaining));
                    }
                }

                var finishedIds = questManager != null
                    ? new HashSet<int>(questManager.HistoryQuests.Select(q => q.ID))
                    : new HashSet<int>();

                foreach (var quest in TotalQuests)
                {
                    if (quest == null) continue;
                    if (finishedIds.Contains(quest.ID)) continue;
                    if (activeQuestIds.Contains(quest.ID)) continue;
                    if (quest.Tasks == null) continue;

                    foreach (var useItem in quest.Tasks.OfType<QuestTask_UseItem>())
                    {
                        if (itemTypeRef(useItem) != itemTypeId) continue;
                        var required = requireAmountRef(useItem);
                        if (required <= 0) continue;

                        results.Add(new UseItemRequirement(
                            null,
                            quest.ID,
                            quest.DisplayName,
                            required));
                    }
                }

                return results;
            }

            private static List<RequiredPerk> ComputePerkRequirements(int itemTypeId)
            {
                var results = new List<RequiredPerk>();
                var manager = PerkTreeManager.Instance;
                if (manager == null) return results;

                foreach (var perkTree in manager.perkTrees)
                {
                    if (perkTree == null) continue;
                    if (ExcludedPerkTreeIds.Contains(perkTree.ID)) continue;

                    foreach (var perk in perkTree.Perks)
                    {
                        if (perk == null) continue;
                        if (perk.Unlocked) continue;
                        if (perk.Unlocking) continue;
                        var requirement = perk.Requirement;
                        if (requirement == null) continue;
                        var costItems = requirement.cost.items;
                        if (costItems == null) continue;

                        foreach (var entry in costItems)
                        {
                            if (entry.id != itemTypeId) continue;
                            results.Add(new RequiredPerk
                            {
                                Amount = entry.amount,
                                PerkTreeName = perkTree.DisplayName,
                                PerkName = perk.DisplayName
                            });
                        }
                    }
                }

                return results;
            }

            private static List<RequiredBuilding> ComputeBuildingRequirements(int itemTypeId)
            {
                var result = new List<RequiredBuilding>();
                var collection = BuildingDataCollection.Instance;
                if (collection == null)
                {
                    return result;
                }

                const string excludedBuildingId = "PetHouse";

                foreach (var info in collection.Infos)
                {
                    if (!info.Valid) continue;
                    if (info.CurrentAmount != 0) continue;
                    if (IsTestingObjectDisplayName(info.DisplayName)) continue;

                    if (info.requireBuildings != null && info.requireBuildings.Length > 0)
                    {
                        if (info.requireBuildings.Contains(info.id))
                        {
                            continue;
                        }

                        if (info.requireBuildings.Contains(excludedBuildingId))
                        {
                            continue;
                        }
                    }

                    if (info.cost.items == null) continue;

                    foreach (var itemEntry in info.cost.items)
                    {
                        if (itemEntry.id != itemTypeId) continue;
                        result.Add(new RequiredBuilding
                        {
                            Amount = itemEntry.amount,
                            BuildingName = info.DisplayName
                        });
                    }
                }

                return result;
            }

            private static void RegisterPerkEvents()
            {
                UnregisterPerkEvents();

                var manager = PerkTreeManager.Instance;
                if (manager == null) return;

                foreach (var perkTree in manager.perkTrees)
                {
                    if (perkTree == null) continue;
                    perkTree.onPerkTreeStatusChanged += OnPerkTreeStatusChanged;
                    SubscribedPerkTrees.Add(perkTree);

                    foreach (var perk in perkTree.Perks)
                    {
                        if (perk == null) continue;
                        perk.onUnlockStateChanged += OnPerkUnlockStateChanged;
                        SubscribedPerks.Add(perk);
                    }
                }

                Perk.OnPerkUnlockConfirmed += OnPerkUnlockConfirmed;
            }

            private static void UnregisterPerkEvents()
            {
                foreach (var perk in SubscribedPerks)
                {
                    if (perk != null)
                    {
                        perk.onUnlockStateChanged -= OnPerkUnlockStateChanged;
                    }
                }
                SubscribedPerks.Clear();

                foreach (var tree in SubscribedPerkTrees)
                {
                    if (tree != null)
                    {
                        tree.onPerkTreeStatusChanged -= OnPerkTreeStatusChanged;
                    }
                }
                SubscribedPerkTrees.Clear();

                Perk.OnPerkUnlockConfirmed -= OnPerkUnlockConfirmed;
            }

            private static void Invalidate()
            {
                _isDirty = true;
            }

            private static void OnQuestListsChanged(QuestManager manager)
            {
                Invalidate();
            }

            private static void OnQuestTaskFinished(Quest quest, Duckov.Quests.Task task)
            {
                Invalidate();
            }

            private static void OnQuestChanged(Quest quest)
            {
                Invalidate();
            }

            private static void OnBuildingListChanged()
            {
                Invalidate();
            }

            private static void OnPerkUnlockStateChanged(Perk perk, bool _)
            {
                Invalidate();
            }

            private static void OnPerkUnlockConfirmed(Perk perk)
            {
                Invalidate();
            }

            private static void OnPerkTreeStatusChanged(PerkTree tree)
            {
                Invalidate();
            }

            private static void OnLevelReady()
            {
                RegisterPerkEvents();
                Invalidate();
            }
        }
    }
}
