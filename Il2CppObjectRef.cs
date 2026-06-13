using System;

namespace IL2CppSharp;

/// <summary>
/// Zero-allocation wrapper around an Il2CppObject* pointer.
/// All methods are null-safe: calling methods on a null ref returns null/default.
/// </summary>
public readonly record struct Il2CppObjectRef(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public static implicit operator IntPtr(Il2CppObjectRef r) => r.Pointer;
    public static implicit operator Il2CppObjectRef(IntPtr p) => new(p);

    /// <summary>Get the Il2CppClass* of this object.</summary>
    /// <returns>Class ref, or null ref if this is null</returns>
    public Il2CppClassRef GetClass()
        => IsNull ? default : Il2CppReflection.GetObjectClass(Pointer);

    /// <summary>Unbox a boxed value type object.</summary>
    /// <typeparam name="T">Unmanaged value type</typeparam>
    /// <returns>Unboxed value, or default if null</returns>
    public T Unbox<T>() where T : unmanaged
        => IsNull ? default : Il2CppReflection.UnboxValue<T>(Pointer);

    /// <summary>Read a reference-type field from this object.</summary>
    /// <param name="field">FieldInfo* ref</param>
    /// <returns>Field value as object ref</returns>
    public Il2CppObjectRef GetFieldValue(Il2CppFieldRef field)
        => (IsNull || field.IsNull) ? default : Il2CppReflection.GetFieldValue(Pointer, field);

    /// <summary>Read a value-type field from this object.</summary>
    public unsafe T GetFieldValue<T>(Il2CppFieldRef field) where T : unmanaged
    {
        if (IsNull || field.IsNull) return default;
        T value = default;
        Il2CppAPI.il2cpp_field_get_value(Pointer, field, &value);
        return value;
    }

    /// <summary>
    /// Find a method on this object's class and invoke it.
    /// Null-safe: returns null ref if any step fails.
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <param name="paramCount">Expected parameter count</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Boxed return value as object ref</returns>
    public Il2CppObjectRef Call(string methodName, int paramCount = 0, IntPtr[] args = null)
    {
        if (IsNull) return default;
        var klass = GetClass();
        if (klass.IsNull) return default;
        var method = klass.FindMethod(methodName, paramCount);
        if (method.IsNull) return default;
        return method.Invoke(Pointer, args);
    }

    /// <summary>Find and invoke a method, unbox the return value.</summary>
    public T CallGet<T>(string methodName) where T : unmanaged
    {
        if (IsNull) return default;
        var klass = GetClass();
        if (klass.IsNull) return default;
        var method = klass.FindMethod(methodName, 0);
        if (method.IsNull) return default;
        return method.InvokeGet<T>(Pointer);
    }

    /// <summary>Find and invoke a method, return the result as string.</summary>
    public string CallGetString(string methodName)
    {
        if (IsNull) return null;
        var klass = GetClass();
        if (klass.IsNull) return null;
        var method = klass.FindMethod(methodName, 0);
        if (method.IsNull) return null;
        return method.InvokeGetString(Pointer);
    }

    /// <summary>Convert this IL2CPP string object to a managed string.</summary>
    /// <returns>Managed string, or null if this is null</returns>
    public string AsString()
        => IsNull ? null : Il2CppAPI.Il2CppStringToManaged(Pointer);

    public override string ToString()
        => IsNull ? "Il2CppObjectRef(null)" : $"Il2CppObjectRef(0x{Pointer:X})";
}
