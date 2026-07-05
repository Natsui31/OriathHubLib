using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Reads canonical and runtime table ranges from the remote process.
    /// </summary>
    internal static class DatTableRangeReader
    {
        private const int RowsVectorOffset = 0x28;

        /// <summary>
        ///     Resolves a .dat file path and reads all rows with one batch read.
        /// </summary>
        public static bool TryReadRows<TRowOffsets>(
            string filePath,
            int rowSize,
            out DatTableRange range,
            out TRowOffsets[] rows,
            out string error)
            where TRowOffsets : unmanaged
        {
            range = default;
            rows = [];
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                error = "Dat file path is empty.";
                return false;
            }

            if (rowSize <= 0)
            {
                error = $"Invalid row size for {filePath}: {rowSize}.";
                return false;
            }

            if (!DatFileReader.TryGetDatTable(filePath, out var datTable))
            {
                error = $"Unable to resolve dat table: {filePath}.";
                return false;
            }

            range = DatTableRange.FromDatTable(datTable, rowSize);
            if (!range.IsValid || range.RowCount <= 0)
            {
                error = $"Resolved dat table is invalid or empty: {filePath}.";
                return false;
            }

            if (!Core.Process.ReadMemoryArray(range.RowsBegin, range.RowCount, out rows))
            {
                rows = [];
                error = $"Unable to read {range.RowCount} rows from {filePath}.";
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Reads the row vector from a runtime DatTable pointer carried by a ForeignRow.
        /// </summary>
        public static bool TryReadRuntimeRange(
            IntPtr datTablePointer,
            int rowSize,
            out DatTableRange range)
        {
            range = default;

            if (datTablePointer == IntPtr.Zero || rowSize <= 0)
                return false;

            if (!Core.Process.ReadMemory<IntPtr>(datTablePointer + RowsVectorOffset, out var rowsVectorPointer) ||
                rowsVectorPointer == IntPtr.Zero)
            {
                return false;
            }

            if (!Core.Process.ReadMemory<IntPtr>(rowsVectorPointer, out var rowsBegin) ||
                !Core.Process.ReadMemory<IntPtr>(rowsVectorPointer + IntPtr.Size, out var rowsEnd))
            {
                return false;
            }

            var resolvedRange = new DatTableRange(datTablePointer, rowsBegin, rowsEnd, rowSize);
            if (!resolvedRange.IsValid || resolvedRange.RowCount <= 0)
                return false;

            range = resolvedRange;
            return true;
        }
    }
}
