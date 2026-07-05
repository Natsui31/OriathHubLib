using System;
using System.Collections.Generic;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Resolves row indexes from runtime DatTable pointers supplied by ForeignRow fields.
    /// </summary>
    internal sealed class RuntimeDatTableResolver
    {
        private readonly Dictionary<RuntimeTableCacheKey, DatTableRange> runtimeRanges = [];

        /// <summary>
        ///     Resolves a row pointer against the exact runtime table instance that produced it.
        /// </summary>
        public bool TryResolveRuntimeIndex(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            int rowSize,
            int expectedRowCount,
            out int index)
        {
            index = -1;

            if (rowPointer == IntPtr.Zero || datTablePointer == IntPtr.Zero)
                return false;

            var cacheKey = new RuntimeTableCacheKey(datTablePointer, rowSize);
            if (!runtimeRanges.TryGetValue(cacheKey, out var range))
            {
                if (!DatTableRangeReader.TryReadRuntimeRange(datTablePointer, rowSize, out range))
                    return false;

                runtimeRanges.Add(cacheKey, range);
            }

            if (expectedRowCount > 0 && range.RowCount != expectedRowCount)
                return false;

            return range.TryGetIndexFromRowAddress(rowPointer, out index);
        }

        /// <summary>
        ///     Clears cached runtime ranges after area reloads.
        /// </summary>
        public void Clear() => runtimeRanges.Clear();

        private readonly record struct RuntimeTableCacheKey(IntPtr DatTablePointer, int RowSize);
    }
}
