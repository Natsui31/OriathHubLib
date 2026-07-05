using System;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures
{
    /// <summary>
    ///     Declares the runtime .dat file path for a concrete <see cref="DatRow"/> type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DatTableAttribute(string filePath) : Attribute
    {
        /// <summary>
        ///     Gets the in-game file path used by <see cref="DatFileReader"/>.
        /// </summary>
        public string FilePath { get; } = filePath;

        /// <summary>
        ///     Gets or sets the row stride when it differs from the unmanaged row size.
        /// </summary>
        public int RowSize { get; set; }
    }
}
