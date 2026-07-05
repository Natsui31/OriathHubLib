using System;
using System.Reflection;
using System.Threading;

#pragma warning disable IDE0130
namespace OriathHub.RemoteObjects
{
    internal static class RemoteObjectExtensions
    {
        private static readonly Lazy<Action<RemoteObjectBase>> _toImGuiFactory =
            new(
                CreateToImGuiDelegate,
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        private static Action<RemoteObjectBase> CreateToImGuiDelegate()
        {
            var method = typeof(RemoteObjectBase).GetMethod(
                "ToImGui",
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public
            ) ?? throw new MissingMethodException(
                    typeof(RemoteObjectBase).FullName,
                    "ToImGui"
                );
            return (Action<RemoteObjectBase>)Delegate.CreateDelegate(
                typeof(Action<RemoteObjectBase>),
                method
            );
        }

        public static void ToImGuiInternal(this RemoteObjectBase instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            _toImGuiFactory.Value(instance);
        }
    }
}
