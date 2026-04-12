using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI.Flex.Core
{
    internal static class FlexDrivenRegistry
    {
        private readonly struct ContributionKey
        {
            public ContributionKey(int ownerId, int targetId)
            {
                OwnerId = ownerId;
                TargetId = targetId;
            }

            public int OwnerId { get; }
            public int TargetId { get; }
        }

        private struct ContributionEntry
        {
            public Object Owner;
            public RectTransform Target;
            public FlexDriveMask Mask;
        }

        private sealed class TargetState
        {
            public DrivenRectTransformTracker Tracker;
            public bool Applied;
        }

        private static readonly Dictionary<ContributionKey, ContributionEntry> s_Contributions = new();
        private static readonly Dictionary<int, TargetState> s_TargetStates = new();
        private static readonly List<ContributionKey> s_ContributionPrune = new();
        private static readonly List<int> s_TargetPrune = new();

        public static void ClearOwner(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            var ownerId = owner.GetInstanceID();
            s_ContributionPrune.Clear();
            foreach (var pair in s_Contributions)
            {
                if (pair.Key.OwnerId == ownerId)
                {
                    s_ContributionPrune.Add(pair.Key);
                }
            }

            for (var i = 0; i < s_ContributionPrune.Count; i++)
            {
                s_Contributions.Remove(s_ContributionPrune[i]);
            }

            ReapplyAllTargets();
        }

        public static void SetContribution(Object owner, RectTransform target, FlexDriveMask mask)
        {
            if (owner == null || target == null)
            {
                return;
            }

            var key = new ContributionKey(owner.GetInstanceID(), target.GetInstanceID());
            if (mask.IsNone())
            {
                s_Contributions.Remove(key);
                ReapplyAllTargets();
                return;
            }

            s_Contributions[key] = new ContributionEntry
            {
                Owner = owner,
                Target = target,
                Mask = mask,
            };
            ReapplyAllTargets();
        }

        public static void ClearAll()
        {
            s_Contributions.Clear();
            foreach (var pair in s_TargetStates)
            {
                var state = pair.Value;
                if (state != null && state.Applied)
                {
                    state.Tracker.Clear();
                    state.Applied = false;
                }
            }

            s_TargetStates.Clear();
        }

        private static void ReapplyAllTargets()
        {
            PruneDestroyedEntries();

            var aggregate = new Dictionary<int, (RectTransform target, Object driver, FlexDriveMask mask)>();
            foreach (var pair in s_Contributions)
            {
                var entry = pair.Value;
                var targetId = pair.Key.TargetId;
                if (!aggregate.TryGetValue(targetId, out var current))
                {
                    aggregate[targetId] = (entry.Target, entry.Owner, entry.Mask);
                    continue;
                }

                aggregate[targetId] = (current.target, current.driver != null ? current.driver : entry.Owner, current.mask | entry.Mask);
            }

            foreach (var pair in aggregate)
            {
                var targetId = pair.Key;
                var aggregated = pair.Value;
                if (!s_TargetStates.TryGetValue(targetId, out var state) || state == null)
                {
                    state = new TargetState();
                    s_TargetStates[targetId] = state;
                }

                if (state.Applied)
                {
                    state.Tracker.Clear();
                    state.Applied = false;
                }

                var driven = aggregated.mask.ToDrivenTransformProperties();
                var target = aggregated.target;
                var driver = aggregated.driver != null ? aggregated.driver : aggregated.target;
                if (driven != DrivenTransformProperties.None && target != null && driver != null)
                {
                    state.Tracker.Add(driver, target, driven);
                    state.Applied = true;
                }
            }

            s_TargetPrune.Clear();
            foreach (var pair in s_TargetStates)
            {
                if (!aggregate.ContainsKey(pair.Key))
                {
                    var state = pair.Value;
                    if (state != null && state.Applied)
                    {
                        state.Tracker.Clear();
                        state.Applied = false;
                    }

                    s_TargetPrune.Add(pair.Key);
                }
            }

            for (var i = 0; i < s_TargetPrune.Count; i++)
            {
                s_TargetStates.Remove(s_TargetPrune[i]);
            }
        }

        private static void PruneDestroyedEntries()
        {
            s_ContributionPrune.Clear();
            foreach (var pair in s_Contributions)
            {
                var entry = pair.Value;
                if (entry.Owner == null || entry.Target == null)
                {
                    s_ContributionPrune.Add(pair.Key);
                }
            }

            for (var i = 0; i < s_ContributionPrune.Count; i++)
            {
                s_Contributions.Remove(s_ContributionPrune[i]);
            }
        }
    }
}
