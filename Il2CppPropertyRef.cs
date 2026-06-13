using System;

namespace IL2CppSharp;

/// <summary>
/// Zero-allocation wrapper around a PropertyInfo* pointer.
/// Provides access to getter/setter methods and shorthand value access.
/// All methods are null-safe.
/// </summary>
public readonly record struct Il2CppPropertyRef(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public static implicit operator IntPtr(Il2CppPropertyRef r) => r.Pointer;
    public static implicit operator Il2CppPropertyRef(IntPtr p) => new(p);

    /// <summary>Get the getter MethodInfo* for this property.</summary>
    public Il2CppMethodRef Getter
        => IsNull ? default : Il2CppAPI.il2cpp_property_get_get_method(Pointer);

    /// <summary>Get the setter MethodInfo* for this property.</summary>
    public Il2CppMethodRef Setter
        => IsNull ? default : Il2CppAPI.il2cpp_property_get_set_method(Pointer);

    /// <summary>Read a value-type property from an instance.</summary>
    /// <typeparam name="T">Unmanaged value type</typeparam>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Property value, or default if null</returns>
    public T GetValue<T>(IntPtr instance) where T : unmanaged
        => Getter.InvokeGet<T>(instance);

    /// <summary>Read a string property from an instance.</summary>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Managed string, or null if null</returns>
    public string GetString(IntPtr instance)
        => Getter.InvokeGetString(instance);

    /// <summary>Read a reference-type property from an instance.</summary>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Property value as object ref</returns>
    public Il2CppObjectRef GetObject(IntPtr instance)
        => Getter.Invoke(instance);

    public override string ToString()
        => IsNull ? "Il2CppPropertyRef(null)" : $"Il2CppPropertyRef(0x{Pointer:X})";
}
