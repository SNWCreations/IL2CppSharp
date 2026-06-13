using System;

namespace IL2CppSharp;

/// <summary>
/// Wraps an uninitialized IL2CPP object (allocated but .ctor not called).
/// Guards against double construction.
/// </summary>
public class Il2CppUninitializedObject
{
    public IntPtr Pointer { get; }
    public bool IsNull => Pointer == IntPtr.Zero;
    private bool _constructed;

    internal Il2CppUninitializedObject(IntPtr pointer) { Pointer = pointer; }

    /// <summary>
    /// Call the constructor on this uninitialized object.
    /// Can only be called once; throws on double construction.
    /// </summary>
    /// <param name="paramCount">Constructor parameter count</param>
    /// <param name="args">Constructor argument pointers</param>
    /// <returns>Object ref wrapping the now-constructed object</returns>
    public Il2CppObjectRef CallConstructor(int paramCount = 0, IntPtr[] args = null)
    {
        if (IsNull) return default;
        if (_constructed)
            throw new InvalidOperationException("Object already constructed.");
        _constructed = true;
        var klass = Il2CppReflection.GetObjectClass(Pointer);
        var ctor = Il2CppReflection.FindMethod(klass, ".ctor", paramCount);
        if (ctor != IntPtr.Zero)
            Il2CppReflection.InvokeMethod(ctor, Pointer, args);
        return new Il2CppObjectRef(Pointer);
    }

    public static implicit operator IntPtr(Il2CppUninitializedObject o) => o?.Pointer ?? IntPtr.Zero;
    public static implicit operator Il2CppObjectRef(Il2CppUninitializedObject o)
        => new(o?.Pointer ?? IntPtr.Zero);
}
