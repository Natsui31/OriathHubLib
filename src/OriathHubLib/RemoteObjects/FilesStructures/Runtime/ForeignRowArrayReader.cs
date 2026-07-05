using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Reads arrays of runtime ForeignRow pointer pairs.
    /// </summary>
    internal static class ForeignRowArrayReader
    {
        private const int PointerCountPerForeignRow = 2;

        /// <summary>
        ///     Reads row/table pointer pairs from a remote ForeignRow array.
        /// </summary>
        public static bool TryRead(
            IntPtr foreignRowArrayAddress,
            int foreignRowArraySize,
            int maxRows,
            out ForeignRowAddress[] rows)
        {
            rows = [];

            if (foreignRowArrayAddress == IntPtr.Zero || foreignRowArraySize <= 0)
                return true;

            if (maxRows > 0 && foreignRowArraySize > maxRows)
                return false;

            var pointerCount = foreignRowArraySize * PointerCountPerForeignRow;
            if (!Core.Process.ReadMemoryArray<IntPtr>(foreignRowArrayAddress, pointerCount, out var pointers))
                return false;

            var resolvedRows = new ForeignRowAddress[foreignRowArraySize];
            for (var i = 0; i < foreignRowArraySize; i++)
            {
                var pointerOffset = i * PointerCountPerForeignRow;
                resolvedRows[i] = new ForeignRowAddress(
                    pointers[pointerOffset],
                    pointers[pointerOffset + 1]);
            }

            rows = resolvedRows;
            return true;
        }
    }
}
