using System;
using System.Reflection;
using System.Runtime.InteropServices;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects.FilesStructures.Runtime
{
    /// <summary>
    ///     Runtime metadata extracted from a concrete DatRow type.
    /// </summary>
    internal sealed class DatRowTypeDescriptor
    {
        private DatRowTypeDescriptor(Type rowType, Type memoryRowType, string filePath, int rowSize)
        {
            RowType = rowType;
            MemoryRowType = memoryRowType;
            FilePath = filePath;
            RowSize = rowSize;
        }

        /// <summary>Gets the concrete row type exposed to consumers.</summary>
        public Type RowType { get; }

        /// <summary>Gets the unmanaged row layout type used for the batch read.</summary>
        public Type MemoryRowType { get; }

        /// <summary>Gets the in-game .dat file path.</summary>
        public string FilePath { get; }

        /// <summary>Gets the row stride used to compute indexes.</summary>
        public int RowSize { get; }

        /// <summary>
        ///     Creates a descriptor from the row inheritance and DatTable attribute.
        /// </summary>
        public static DatRowTypeDescriptor Create(Type rowType)
        {
            ArgumentNullException.ThrowIfNull(rowType);

            if (!typeof(DatRow).IsAssignableFrom(rowType))
                throw new InvalidOperationException($"{rowType.FullName} must inherit {nameof(DatRow)}.");

            var datRowBase = FindDatRowBase(rowType);
            if (datRowBase is null)
                throw new InvalidOperationException($"{rowType.FullName} must inherit DatRow<TSelf, TMemoryRow>.");

            var genericArguments = datRowBase.GetGenericArguments();
            var selfType = genericArguments[0];
            var memoryRowType = genericArguments[1];

            if (selfType != rowType)
            {
                throw new InvalidOperationException(
                    $"{rowType.FullName} must use itself as DatRow<TSelf, TMemoryRow> TSelf argument.");
            }

            var attribute = rowType.GetCustomAttribute<DatTableAttribute>();
            if (attribute is null)
                throw new InvalidOperationException($"{rowType.FullName} is missing {nameof(DatTableAttribute)}.");

            if (string.IsNullOrWhiteSpace(attribute.FilePath))
                throw new InvalidOperationException($"{rowType.FullName} has an empty dat table file path.");

            var rowSize = attribute.RowSize > 0 ? attribute.RowSize : Marshal.SizeOf(memoryRowType);
            if (rowSize <= 0)
                throw new InvalidOperationException($"{rowType.FullName} resolved an invalid row size: {rowSize}.");

            return new DatRowTypeDescriptor(rowType, memoryRowType, attribute.FilePath, rowSize);
        }

        private static Type? FindDatRowBase(Type rowType)
        {
            var current = rowType.BaseType;
            while (current is not null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(DatRow<,>))
                    return current;

                current = current.BaseType;
            }

            return null;
        }
    }
}
