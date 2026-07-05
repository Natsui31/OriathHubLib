using OriathHub.RemoteObjects.FilesStructures.Runtime;
using System;
using System.Collections.Generic;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures
{
    /// <summary>
    ///     Base type for a canonical row read from a runtime .dat table.
    /// </summary>
    public abstract class DatRow : RemoteObjectBase
    {
        /// <summary>
        ///     Initializes a row at its canonical runtime address.
        /// </summary>
        protected internal DatRow(IntPtr address, int index)
            : base(address, false, true)
        {
            Index = index;
        }

        /// <summary>
        ///     Gets the canonical zero-based row index in the table.
        /// </summary>
        public int Index { get; }
    }

    /// <summary>
    ///     Base type for rows backed by a pre-read unmanaged memory row.
    /// </summary>
    public abstract class DatRow<TSelf, TMemoryRow> : DatRow
        where TSelf : DatRow<TSelf, TMemoryRow>
        where TMemoryRow : unmanaged
    {
        private bool isResolved;

        /// <summary>
        ///     Initializes a row from the table batch read result.
        /// </summary>
        protected internal DatRow(IntPtr address, int index, TMemoryRow memoryRow)
            : base(address, index)
        {
            MemoryRow = memoryRow;
        }

        /// <summary>
        ///     Gets the raw row data already read by the table loader.
        /// </summary>
        protected TMemoryRow MemoryRow { get; }

        /// <summary>
        ///     Forces the first pointer/reference resolution after batch loading.
        /// </summary>
        internal void InitializeFromMemoryRow()
        {
            UpdateData(true);
        }

        /// <summary>
        ///     Resolves only data that depends on pointers stored in <see cref="MemoryRow"/>.
        /// </summary>
        protected sealed override void UpdateData(bool hasAddressChanged)
        {
            if (isResolved && !hasAddressChanged)
                return;

            ResolveData();
            isResolved = true;
        }

        /// <summary>
        ///     Clears resolved fields when the row address is invalidated.
        /// </summary>
        protected sealed override void CleanUpData()
        {
            isResolved = false;
            CleanUpFields();
        }

        /// <summary>
        ///     Resolves strings, arrays, and foreign rows from <see cref="MemoryRow"/>.
        /// </summary>
        protected abstract void ResolveData();

        /// <summary>
        ///     Resets public fields exposed by the concrete row.
        /// </summary>
        protected abstract void CleanUpFields();

        /// <summary>
        ///     Reads a UTF-16 string pointer from the remote process.
        /// </summary>
        protected static string ReadString(IntPtr pointer)
        {
            return pointer == IntPtr.Zero ? string.Empty : Core.Process.ReadUnicodeString(pointer);
        }

        /// <summary>
        ///     Resolves a runtime ForeignRow pointer pair into a canonical row.
        /// </summary>
        protected static bool TryResolveRuntimeRow<TTarget>(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out TTarget? row)
            where TTarget : DatRow
        {
            return FileStructureStore.TryResolveRuntimeRow(rowPointer, datTablePointer, out row);
        }

        /// <summary>
        ///     Resolves a runtime ForeignRow pointer pair into a canonical row and index.
        /// </summary>
        protected static bool TryResolveRuntimeRow<TTarget>(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out TTarget? row,
            out int index)
            where TTarget : DatRow
        {
            return FileStructureStore.TryResolveRuntimeRow(rowPointer, datTablePointer, out row, out index);
        }

        /// <summary>
        ///     Resolves a runtime ForeignRow array into canonical rows and indexes.
        /// </summary>
        protected static TTarget[] ResolveForeignRowArray<TTarget>(
            IntPtr foreignRowArrayAddress,
            int foreignRowArraySize,
            int maxRows,
            out int[] indexes)
            where TTarget : DatRow
        {
            indexes = [];

            if (!ForeignRowArrayReader.TryRead(foreignRowArrayAddress, foreignRowArraySize, maxRows, out var foreignRows) ||
                foreignRows.Length == 0)
            {
                return [];
            }

            var resolvedRows = new List<TTarget>(foreignRows.Length);
            var resolvedIndexes = new List<int>(foreignRows.Length);

            foreach (var foreignRow in foreignRows)
            {
                if (foreignRow.IsNull)
                    continue;

                if (!TryResolveRuntimeRow<TTarget>(
                        foreignRow.RowPointer,
                        foreignRow.TablePointer,
                        out var row,
                        out var index) ||
                    row is null)
                {
                    continue;
                }

                resolvedRows.Add(row);
                resolvedIndexes.Add(index);
            }

            indexes = [.. resolvedIndexes];
            return [.. resolvedRows];
        }
    }
}
