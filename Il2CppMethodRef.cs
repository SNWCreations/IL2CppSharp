using System;

namespace IL2CppSharp;

/// <summary>
/// Zero-allocation wrapper around a MethodInfo* pointer.
/// Provides fluent invocation that collapses the invoke+null-check+unbox chain.
/// All methods are null-safe: calling methods on a null ref returns null/default.
/// </summary>
public readonly record struct Il2CppMethodRef(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public static implicit operator IntPtr(Il2CppMethodRef r) => r.Pointer;
    public static implicit operator Il2CppMethodRef(IntPtr p) => new(p);

    /// <summary>Invoke on an instance with optional arguments.</summary>
    /// <param name="instance">Il2CppObject* instance (IntPtr.Zero for static)</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Boxed return value as object ref</returns>
    public Il2CppObjectRef Invoke(IntPtr instance, IntPtr[] args = null)
        => IsNull ? default : Il2CppReflection.InvokeMethod(Pointer, instance, args);

    /// <summary>Invoke as a static method with optional arguments.</summary>
    /// <param name="args">Argument pointers</param>
    /// <returns>Boxed return value as object ref</returns>
    public Il2CppObjectRef InvokeStatic(IntPtr[] args = null)
        => IsNull ? default : Il2CppReflection.InvokeStaticMethod(Pointer, args);

    /// <summary>Invoke and unbox the return value.</summary>
    /// <typeparam name="T">Unmanaged return type</typeparam>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Unboxed value, or default if null</returns>
    public T InvokeGet<T>(IntPtr instance, IntPtr[] args = null) where T : unmanaged
        => IsNull ? default : Il2CppReflection.InvokeGet<T>(Pointer, instance, args);

    /// <summary>Invoke and return the result as a managed string.</summary>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Managed string, or null if method is null or invocation fails</returns>
    public string InvokeGetString(IntPtr instance, IntPtr[] args = null)
        => IsNull ? null : Il2CppReflection.InvokeGetString(Pointer, instance, args);

    /// <summary>Invoke with a single string argument.</summary>
    public Il2CppObjectRef InvokeWithString(IntPtr instance, string value)
        => IsNull ? default : Il2CppReflection.InvokeMethodWithString(Pointer, instance, value);

    /// <summary>Invoke with a single int argument.</summary>
    public Il2CppObjectRef InvokeWithInt(IntPtr instance, int value)
        => IsNull ? default : Il2CppReflection.InvokeMethodWithInt(Pointer, instance, value);

    /// <summary>Invoke with a single bool argument.</summary>
    public Il2CppObjectRef InvokeWithBool(IntPtr instance, bool value)
        => IsNull ? default : Il2CppReflection.InvokeMethodWithBool(Pointer, instance, value);

    /// <summary>Invoke with a single float argument.</summary>
    public Il2CppObjectRef InvokeWithFloat(IntPtr instance, float value)
        => IsNull ? default : Il2CppReflection.InvokeMethodWithFloat(Pointer, instance, value);

    public override string ToString()
        => IsNull ? "Il2CppMethodRef(null)" : $"Il2CppMethodRef(0x{Pointer:X})";
}
