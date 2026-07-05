using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Describes a contiguous runtime row block and its stride.
    /// </summary>
    internal readonly struct DatTableRange(IntPtr tablePointer, IntPtr rowsBegin, IntPtr rowsEnd, int rowSize)
    {
        /// <summary>Gets the runtime table pointer when the range came from a live table pointer.</summary>
        public IntPtr TablePointer { get; } = tablePointer;

        /// <summary>Gets the address of the first row.</summary>
        public IntPtr RowsBegin { get; } = rowsBegin;

        /// <summary>Gets the address one past the last row.</summary>
        public IntPtr RowsEnd { get; } = rowsEnd;

        /// <summary>Gets the row stride in bytes.</summary>
        public int RowSize { get; } = rowSize;

        /// <summary>Gets the number of rows in the range.</summary>
        public int RowCount { get; } = ComputeRowCount(rowsBegin, rowsEnd, rowSize);

        /// <summary>Gets a value indicating whether the range can be used for index resolution.</summary>
        public bool IsValid => RowsBegin != IntPtr.Zero &&
                               RowsEnd.ToInt64() >= RowsBegin.ToInt64() &&
                               RowSize > 0;

        /// <summary>
        ///     Creates a canonical range from a DatFileReader result.
        /// </summary>
        public static DatTableRange FromDatTable(DatTable table, int rowSize) =>
            new(IntPtr.Zero, table.RowsBegin, table.RowsEnd, rowSize);

        /// <summary>
        ///     Gets the row address for a canonical index.
        /// </summary>
        public IntPtr GetRowAddress(int index)
        {
            if ((uint)index >= (uint)RowCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return RowsBegin + index * RowSize;
        }

        /// <summary>
        ///     Converts a row address from this range into its zero-based index.
        /// </summary>
        public bool TryGetIndexFromRowAddress(IntPtr rowAddress, out int index)
        {
            index = -1;

            if (!IsValid || rowAddress == IntPtr.Zero)
                return false;

            var begin = RowsBegin.ToInt64();
            var end = RowsEnd.ToInt64();
            var value = rowAddress.ToInt64();

            if (value < begin || value >= end)
                return false;

            var delta = value - begin;
            if (delta % RowSize != 0)
                return false;

            var resolvedIndex = (int)(delta / RowSize);
            if ((uint)resolvedIndex >= (uint)RowCount)
                return false;

            index = resolvedIndex;
            return true;
        }

        private static int ComputeRowCount(IntPtr rowsBegin, IntPtr rowsEnd, int rowSize)
        {
            if (rowsBegin == IntPtr.Zero || rowSize <= 0)
                return 0;

            var byteLength = rowsEnd.ToInt64() - rowsBegin.ToInt64();
            return byteLength >= 0 ? (int)(byteLength / rowSize) : 0;
        }
    }
}
