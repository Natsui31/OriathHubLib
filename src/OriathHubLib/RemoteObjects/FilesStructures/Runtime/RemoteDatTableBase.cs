using ImGuiNET;
using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Non-generic bridge used by FileStructureStore to cache heterogeneous tables.
    /// </summary>
    internal abstract class RemoteDatTableBase
    {
        protected RemoteDatTableBase(DatRowTypeDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        /// <summary>Gets the row type metadata for this table.</summary>
        public DatRowTypeDescriptor Descriptor { get; }

        /// <summary>Gets the loaded row count.</summary>
        public abstract int RowCount { get; }

        /// <summary>Gets a value indicating whether the table loaded successfully.</summary>
        public abstract bool IsLoaded { get; }

        /// <summary>Gets the last load error for this table.</summary>
        public abstract string LastError { get; }

        /// <summary>Returns the strongly typed row array as <see cref="Array"/>.</summary>
        public abstract Array GetRowsAsArray();

        /// <summary>
        ///     Resolves a runtime row/table pointer pair through this table.
        /// </summary>
        public abstract bool TryResolveRuntimeRow(
            IntPtr rowPointer,
            IntPtr datTablePointer,
            out DatRow? row,
            out int index);

        /// <summary>
        ///     Clears cached runtime table ranges.
        /// </summary>
        public abstract void ClearRuntimeCache();

        /// <summary>
        ///     Draws a debug view of the loaded table and its rows.
        /// </summary>
        public virtual void ToImGui()
        {
            ImGui.TextUnformatted($"FilePath: {Descriptor.FilePath}");
            ImGui.TextUnformatted($"RowType: {Descriptor.RowType.Name}");
            ImGui.TextUnformatted($"MemoryRowType: {Descriptor.MemoryRowType.Name}");
            ImGui.TextUnformatted($"RowSize: {Descriptor.RowSize}");
            ImGui.TextUnformatted($"Loaded: {IsLoaded}");
            ImGui.TextUnformatted($"Rows: {RowCount}");

            if (!string.IsNullOrEmpty(LastError))
                ImGui.TextUnformatted($"LastError: {LastError}");

            var rows = GetRowsAsArray();
            if (!ImGui.TreeNode($"Rows ({rows.Length})##{Descriptor.RowType.FullName}"))
                return;

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows.GetValue(i) is not DatRow row)
                    continue;

                if (ImGui.TreeNode($"Row #{i}##{Descriptor.RowType.FullName}-{i}"))
                {
                    row.ToImGuiInternal();
                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }
}
