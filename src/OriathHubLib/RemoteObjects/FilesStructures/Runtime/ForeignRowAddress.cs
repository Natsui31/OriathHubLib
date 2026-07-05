using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Runtime representation of a ForeignRow: row pointer plus table pointer.
    /// </summary>
    internal readonly record struct ForeignRowAddress(IntPtr RowPointer, IntPtr TablePointer)
    {
        /// <summary>Gets a value indicating whether the row pointer is null.</summary>
        public bool IsNull => RowPointer == IntPtr.Zero;

        public override string ToString()
        {
            return $"row=0x{RowPointer.ToInt64():X} table=0x{TablePointer.ToInt64():X}";
        }
    }
}
