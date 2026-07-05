using ImGuiNET;
using OriathHub.RemoteObjects.FilesStructures.Runtime;
using System;
using System.Collections.Generic;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures
{
    /// <summary>
    ///     Lazily loads and caches runtime .dat rows by concrete row type.
    /// </summary>
    public static class FileStructureStore
    {
        private static readonly Dictionary<Type, RemoteDatTableBase> Tables = [];
        private static readonly Dictionary<Type, DatRowTypeDescriptor> Descriptors = [];

        /// <summary>
        ///     Gets the last table loading error, when a requested table failed to load.
        /// </summary>
        public static string LastError { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the canonical rows for the requested row type, loading the table on first access.
        /// </summary>
        public static TRow[] GetRows<TRow>()
            where TRow : DatRow
        {
            var table = GetOrCreateTable(typeof(TRow));
            LastError = table.LastError;
            return (TRow[])table.GetRowsAsArray();
        }

        /// <summary>
        ///     Resolves a runtime row/table pointer pair into a canonical row.
        /// </summary>
        public static bool TryResolveRuntimeRow<TRow>(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out TRow? row)
            where TRow : DatRow
        {
            return TryResolveRuntimeRow(rowPointer, datTablePointer, out row, out _);
        }

        /// <summary>
        ///     Resolves a runtime row/table pointer pair into a canonical row and index.
        /// </summary>
        public static bool TryResolveRuntimeRow<TRow>(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out TRow? row,
            out int index)
            where TRow : DatRow
        {
            row = null;
            index = -1;

            var table = GetOrCreateTable(typeof(TRow));
            LastError = table.LastError;
            if (!table.TryResolveRuntimeRow(rowPointer, datTablePointer, out var resolvedRow, out index))
                return false;

            row = (TRow)resolvedRow!;
            return true;
        }

        /// <summary>
        ///     Clears loaded tables and runtime range caches.
        /// </summary>
        internal static void Clear()
        {
            Tables.Clear();
            LastError = string.Empty;
        }

        /// <summary>
        ///     Draws the current table cache state for debugging.
        /// </summary>
        internal static void DrawLoadedTablesImGui()
        {
            if (!string.IsNullOrEmpty(LastError))
                ImGui.TextUnformatted($"LastError: {LastError}");

            ImGui.TextUnformatted($"Loaded tables: {Tables.Count}");
            foreach (var (rowType, table) in Tables)
            {
                if (!ImGui.TreeNode($"{rowType.Name} ({table.RowCount})##{rowType.FullName}"))
                    continue;

                table.ToImGui();
                ImGui.TreePop();
            }
        }

        private static RemoteDatTableBase GetOrCreateTable(Type rowType)
        {
            if (Tables.TryGetValue(rowType, out var table))
            {
                if (table.IsLoaded)
                    return table;

                Tables.Remove(rowType);
            }

            var descriptor = GetOrCreateDescriptor(rowType);
            var tableType = typeof(RemoteDatTable<,>).MakeGenericType(rowType, descriptor.MemoryRowType);
            var createdTable = (RemoteDatTableBase?)Activator.CreateInstance(tableType, descriptor);
            if (createdTable is null)
                throw new InvalidOperationException($"Unable to create runtime dat table for {rowType.FullName}.");

            Tables.Add(rowType, createdTable);
            return createdTable;
        }

        private static DatRowTypeDescriptor GetOrCreateDescriptor(Type rowType)
        {
            if (Descriptors.TryGetValue(rowType, out var descriptor))
                return descriptor;

            descriptor = DatRowTypeDescriptor.Create(rowType);
            Descriptors.Add(rowType, descriptor);
            return descriptor;
        }
    }
}
