using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    // Holds the pre-computed display label alongside the raw defName key so we
    // avoid re-allocating CapitalizeFirst() strings every frame during draw.
    internal readonly struct ListEntry
    {
        public readonly string key;
        public readonly string label;
        public readonly string labelLower;

        public ListEntry(string key, string label)
        {
            this.key = key;
            this.label = label;
            this.labelLower = label.ToLowerInvariant();
        }
    }

    // Per-tab cache of partitioned + filtered entries. Rebuilds only when the
    // source dictionary size or the search string actually changes — the old
    // code rebuilt these lists on every IMGUI frame, which was the main source
    // of scroll-lag on heavily modded games.
    internal class QualityListCache
    {
        private readonly List<ListEntry> left = new List<ListEntry>();
        private readonly List<ListEntry> right = new List<ListEntry>();

        public List<ListEntry> FilteredLeft { get; private set; }
        public List<ListEntry> FilteredRight { get; private set; }

        private int lastDictCount = -1;
        private string lastSearchLower;
        private bool partitionDirty = true;

        public QualityListCache()
        {
            FilteredLeft = left;
            FilteredRight = right;
        }

        // Force a partition rebuild on next EnsureBuilt. Called after callers
        // mutate the dict in ways Count can't detect (rare — Populate only adds
        // missing keys, Clear zeroes Count, so normally Count is sufficient).
        public void Invalidate()
        {
            partitionDirty = true;
        }

        // Rebuilds partition and/or filter on demand. `partitioner` may be null
        // to put every entry into the left list (used by flat tabs like weapons
        // and apparel).
        public void EnsureBuilt(
            Dictionary<string, bool> dict,
            string searchRaw,
            Func<ThingDef, bool> partitioner)
        {
            string searchLower = string.IsNullOrEmpty(searchRaw)
                ? string.Empty
                : searchRaw.ToLowerInvariant();

            if (partitionDirty || dict.Count != lastDictCount)
            {
                RebuildPartition(dict, partitioner);
                lastDictCount = dict.Count;
                partitionDirty = false;
                // Partition changed, so the filtered view must be recomputed.
                lastSearchLower = null;
            }

            if (!string.Equals(searchLower, lastSearchLower, StringComparison.Ordinal))
            {
                RebuildFilter(searchLower);
                lastSearchLower = searchLower;
            }
        }

        private void RebuildPartition(Dictionary<string, bool> dict, Func<ThingDef, bool> partitioner)
        {
            left.Clear();
            right.Clear();

            foreach (string key in dict.Keys)
            {
                ListEntry entry = new ListEntry(key, key.CapitalizeFirst());

                if (partitioner == null)
                {
                    left.Add(entry);
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(key);
                if (def == null)
                {
                    // Missing def (mod removed?) — keep it in the right column
                    // so the user can still see and de-select it.
                    right.Add(entry);
                    continue;
                }

                if (partitioner(def)) left.Add(entry);
                else right.Add(entry);
            }

            left.Sort(CompareByLabel);
            right.Sort(CompareByLabel);
        }

        private static int CompareByLabel(ListEntry a, ListEntry b)
        {
            return string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
        }

        private void RebuildFilter(string searchLower)
        {
            if (string.IsNullOrEmpty(searchLower))
            {
                // Empty search — expose the partition lists directly, zero allocation.
                FilteredLeft = left;
                FilteredRight = right;
                return;
            }

            List<ListEntry> leftOut = new List<ListEntry>(left.Count);
            List<ListEntry> rightOut = new List<ListEntry>(right.Count);

            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].labelLower.Contains(searchLower)) leftOut.Add(left[i]);
            }
            for (int i = 0; i < right.Count; i++)
            {
                if (right[i].labelLower.Contains(searchLower)) rightOut.Add(right[i]);
            }

            FilteredLeft = leftOut;
            FilteredRight = rightOut;
        }
    }

    internal static class SearchableListUI
    {
        public const float RowHeight = 24f;
        public const float SearchBarHeight = 28f;
        private const float ClearButtonWidth = 22f;

        // Draws a search text field with a clear button. Writes back through the ref.
        public static void DrawSearchBar(Rect rect, ref string searchText)
        {
            float fieldWidth = rect.width - ClearButtonWidth - 4f;
            Rect fieldRect = new Rect(rect.x, rect.y + 2f, fieldWidth, rect.height - 4f);
            string updated = Widgets.TextField(fieldRect, searchText ?? string.Empty);
            if (!string.Equals(updated, searchText, StringComparison.Ordinal))
            {
                searchText = updated;
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                Rect clearRect = new Rect(
                    rect.xMax - ClearButtonWidth,
                    rect.y + 2f,
                    ClearButtonWidth,
                    rect.height - 4f);
                if (Widgets.ButtonText(clearRect, "x"))
                {
                    searchText = string.Empty;
                }
            }
        }

        // Virtualized checkbox list. Only the rows whose Y intersects the
        // visible viewport are drawn, so cost stays O(viewport) instead of
        // O(totalItems). The scroll bar still reflects the full item count
        // because the inner viewRect keeps its full height.
        public static void DrawVirtualizedCheckboxList(
            Rect outRect,
            ref Vector2 scrollPos,
            List<ListEntry> entries,
            Dictionary<string, bool> dict)
        {
            if (entries.Count == 0)
            {
                Widgets.Label(outRect, "QEverything.NoResults".Translate());
                return;
            }

            float contentHeight = entries.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, contentHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect, true);

            int first = Mathf.Max(0, Mathf.FloorToInt(scrollPos.y / RowHeight));
            int lastExclusive = Mathf.Min(
                entries.Count,
                Mathf.CeilToInt((scrollPos.y + outRect.height) / RowHeight) + 1);

            for (int i = first; i < lastExclusive; i++)
            {
                ListEntry entry = entries[i];
                if (!dict.TryGetValue(entry.key, out bool value))
                {
                    // Dict mutated out from under the cache; skip until next rebuild.
                    continue;
                }

                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                Widgets.CheckboxLabeled(rowRect, entry.label, ref value);
                dict[entry.key] = value;
            }

            Widgets.EndScrollView();
        }
    }
}
