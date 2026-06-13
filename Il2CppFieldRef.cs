using System;

namespace IL2CppSharp;

/// <summary>
/// Zero-allocation wrapper around a FieldInfo* pointer.
/// Provides typed field access for both instance and static fields.
/// All methods are null-safe.
/// </summary>
public readonly unsafe record struct Il2CppFieldRef(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public static implicit operator IntPtr(Il2CppFieldRef r) => r.Pointer;
    public static implicit operator Il2CppFieldRef(IntPtr p) => new(p);

    /// <summary>Read a reference-type field from an object instance.</summary>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <returns>Field value as object ref</returns>
    public Il2CppObjectRef GetValue(IntPtr obj)
        => (IsNull || obj == IntPtr.Zero) ? default : Il2CppReflection.GetFieldValue(obj, Pointer);

    /// <summary>Read a value-type field from an object instance.</summary>
    /// <typeparam name="T">Unmanaged value type</typeparam>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <returns>Field value, or default if null</returns>
    public T GetValue<T>(IntPtr obj) where T : unmanaged
    {
        if (IsNull || obj == IntPtr.Zero) return default;
        T value = default;
        Il2CppAPI.il2cpp_field_get_value(obj, Pointer, &value);
        return value;
    }

    /// <summary>Set a pointer-sized field on an object instance.</summary>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <param name="value">Value to set</param>
    public void SetValue(IntPtr obj, IntPtr value)
    {
        if (IsNull || obj == IntPtr.Zero) return;
        Il2CppAPI.SetFieldValue(obj, Pointer, value);
    }

    /// <summary>Set a value-type field on an object instance.</summary>
    public void SetValue<T>(IntPtr obj, T value) where T : unmanaged
    {
        if (IsNull || obj == IntPtr.Zero) return;
        int offset = Il2CppAPI.il2cpp_field_get_offset(Pointer);
        *(T*)((byte*)obj + offset) = value;
    }

    /// <summary>Read a static field value (reference type).</summary>
    /// <returns>Static field value as IntPtr</returns>
    public IntPtr GetStaticValue()
    {
        if (IsNull) return IntPtr.Zero;
        IntPtr value = IntPtr.Zero;
        Il2CppAPI.il2cpp_field_static_get_value(Pointer, &value);
        return value;
    }

    /// <summary>Read a static field value (value type).</summary>
    public T GetStaticValue<T>() where T : unmanaged
    {
        if (IsNull) return default;
        T value = default;
        Il2CppAPI.il2cpp_field_static_get_value(Pointer, &value);
        return value;
    }

    /// <summary>Set a static field value.</summary>
    public void SetStaticValue<T>(T value) where T : unmanaged
    {
        if (IsNull) return;
        Il2CppAPI.il2cpp_field_static_set_value(Pointer, &value);
    }

    /// <summary>Get the field offset within the object.</summary>
    public int Offset => IsNull ? 0 : Il2CppAPI.il2cpp_field_get_offset(Pointer);

    public override string ToString()
        => IsNull ? "Il2CppFieldRef(null)" : $"Il2CppFieldRef(0x{Pointer:X})";
}
