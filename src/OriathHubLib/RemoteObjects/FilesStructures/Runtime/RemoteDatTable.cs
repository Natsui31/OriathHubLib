using System;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Lazy-loaded canonical table for a concrete DatRow type.
    /// </summary>
    internal sealed class RemoteDatTable<TSelf, TMemoryRow> : RemoteDatTableBase
        where TSelf : DatRow<TSelf, TMemoryRow>
        where TMemoryRow : unmanaged
    {
        private static readonly Func<IntPtr, int, TMemoryRow, TSelf> RowFactory = CreateRowFactory();

        private readonly RuntimeDatTableResolver runtimeResolver = new();
        private DatTableRange canonicalRange;
        private TSelf[] rows = [];
        private string lastError = string.Empty;
        private bool isLoaded;

        public RemoteDatTable(DatRowTypeDescriptor descriptor)
            : base(descriptor)
        {
            Load();
        }

        /// <summary>Gets the loaded canonical rows.</summary>
        public TSelf[] Rows => rows;

        public override int RowCount => rows.Length;

        public override bool IsLoaded => isLoaded;

        public override string LastError => lastError;

        public override Array GetRowsAsArray() => rows;

        public override bool TryResolveRuntimeRow(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out DatRow? row,
            out int index)
        {
            row = null;
            if (TryResolveRuntimeRowCore(rowPointer, datTablePointer, out var resolvedRow, out index))
            {
                row = resolvedRow;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Resolves a runtime row pointer into the canonical row array.
        /// </summary>
        public bool TryResolveRuntimeRowCore(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out TSelf? row,
            out int index)
        {
            row = null;
            index = -1;

            if (rows.Length == 0 || rowPointer == IntPtr.Zero)
                return false;

            if (datTablePointer != IntPtr.Zero)
            {
                if (!runtimeResolver.TryResolveRuntimeIndex(
                        rowPointer,
                        datTablePointer,
                        Descriptor.RowSize,
                        rows.Length,
                        out index))
                {
                    return false;
                }

                row = rows[index];
                return true;
            }

            if (!canonicalRange.TryGetIndexFromRowAddress(rowPointer, out index))
                return false;

            if ((uint)index >= (uint)rows.Length)
                return false;

            row = rows[index];
            return true;
        }

        public override void ClearRuntimeCache()
        {
            runtimeResolver.Clear();
        }

        private void Load()
        {
            isLoaded = false;
            lastError = string.Empty;
            canonicalRange = default;
            rows = [];
            runtimeResolver.Clear();

            if (!DatTableRangeReader.TryReadRows<TMemoryRow>(
                    Descriptor.FilePath,
                    Descriptor.RowSize,
                    out var range,
                    out var memoryRows,
                    out var error))
            {
                lastError = error;
                return;
            }

            var loadedRows = new TSelf[memoryRows.Length];
            for (var i = 0; i < memoryRows.Length; i++)
            {
                var row = RowFactory(range.GetRowAddress(i), i, memoryRows[i]);
                row.InitializeFromMemoryRow();
                loadedRows[i] = row;
            }

            canonicalRange = range;
            rows = loadedRows;
            isLoaded = true;
        }

        private static Func<IntPtr, int, TMemoryRow, TSelf> CreateRowFactory()
        {
            var constructor = typeof(TSelf).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: [typeof(IntPtr), typeof(int), typeof(TMemoryRow)],
                modifiers: null);

            if (constructor is null)
            {
                throw new MissingMethodException(
                    typeof(TSelf).FullName,
                    $".ctor({nameof(IntPtr)}, {nameof(Int32)}, {typeof(TMemoryRow).Name})");
            }

            var address = Expression.Parameter(typeof(IntPtr), "address");
            var index = Expression.Parameter(typeof(int), "index");
            var memoryRow = Expression.Parameter(typeof(TMemoryRow), "memoryRow");
            var body = Expression.New(constructor, address, index, memoryRow);
            return Expression.Lambda<Func<IntPtr, int, TMemoryRow, TSelf>>(body, address, index, memoryRow).Compile();
        }
    }
}
