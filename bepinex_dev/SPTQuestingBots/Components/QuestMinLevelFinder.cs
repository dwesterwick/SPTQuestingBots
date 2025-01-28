using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Quests;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models.Questing;

namespace SPTQuestingBots.Components
{
    public class QuestMinLevelFinder
    {
        private static Dictionary<string, int> cachedMinLevelsForQuestIds = new Dictionary<string, int>();

        private Dictionary<Quest, int> minLevelsForQuests = new Dictionary<Quest, int>();

        private Quest targetQuest;

        public QuestMinLevelFinder(Quest _quest)
        {
            targetQuest = _quest;
        }

        public static void ClearCache()
        {
            cachedMinLevelsForQuestIds.Clear();
        }

        public int FindMinLevel() => FindMinLevel(targetQuest);

        private int FindMinLevel(Quest quest)
        {
            // Check if this instance has already checked for the min level of this quest
            if (minLevelsForQuests.ContainsKey(quest))
            {
                return minLevelsForQuests[quest];
            }

            // If this is an EFT quest, check if a cached value exists
            if (cachedMinLevelsForQuestIds.ContainsKey(quest.Template?.Id))
            {
                return cachedMinLevelsForQuestIds[quest.Template.Id];
            }

            UpdateMinLevelFromQuestRequirements(quest);

            UpdateCache();

            return minLevelsForQuests[quest];
        }

        private void UpdateCache()
        {
            foreach (Quest preReqQuest in minLevelsForQuests.Keys)
            {
                if (preReqQuest.Template == null)
                {
                    continue;
                }

                UpdateCachedMinLevel(preReqQuest.Template.Id, minLevelsForQuests[preReqQuest]);
            }
        }

        private void UpdateMinLevelFromQuestRequirements(Quest quest)
        {
            int startingMinLevel = quest.Template?.Level ?? 0;
            minLevelsForQuests.Add(quest, startingMinLevel);

            EQuestStatus eQuestStatus = EQuestStatus.AvailableForStart;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) != true)
            {
                return;
            }

            foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
            {
                // Check if a condition-check exists for player level. If so, use that value if it's higher than the current minimum level. 
                ConditionLevel conditionLevel = condition as ConditionLevel;
                if (conditionLevel != null)
                {
                    UpdateMinLevel(quest, GetLevelFromConditionLevel(conditionLevel));
                }

                // Check if another quest must be completed first. If so, use its minimum player level if it's higher than the current minimum level. 
                ConditionQuest conditionQuest = condition as ConditionQuest;
                if (conditionQuest != null)
                {
                    UpdateMinLevel(quest, GetLevelFromConditionQuest(conditionQuest));
                }
            }
        }

        private int GetLevelFromConditionLevel(ConditionLevel conditionLevel)
        {
            // TO DO: This might be needed to set maximum player levels for quests in the future, but I don't think this exists in EFT right now. 
            if ((conditionLevel.compareMethod != ECompareMethod.MoreOrEqual) && (conditionLevel.compareMethod != ECompareMethod.More))
            {
                return 0;
            }

            return (int)conditionLevel.value;
        }

        private int GetLevelFromConditionQuest(ConditionQuest conditionQuest)
        {
            // Find the required quest
            Quest preReqQuest = BotJobAssignmentFactory.FindQuest(conditionQuest.target);
            if (preReqQuest == null)
            {
                LoggingController.LogWarning("Cannot find prerequisite quest " + conditionQuest.target + " for quest " + targetQuest.Name);
                return 0;
            }

            return FindMinLevel(preReqQuest);
        }

        private void UpdateMinLevel(Quest quest, int level)
        {
            if (!minLevelsForQuests.ContainsKey(quest))
            {
                return;
            }

            if (level > minLevelsForQuests[quest])
            {
                minLevelsForQuests[quest] = level;
            }
        }

        private void UpdateCachedMinLevel(string questId, int level)
        {
            if (!cachedMinLevelsForQuestIds.ContainsKey(questId))
            {
                cachedMinLevelsForQuestIds.Add(questId, level);
                return;
            }

            if (level > cachedMinLevelsForQuestIds[questId])
            {
                cachedMinLevelsForQuestIds[questId] = level;
            }
        }
    }
}
