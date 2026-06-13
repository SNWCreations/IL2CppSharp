using System;

namespace IL2CppSharp;

/// <summary>
/// Zero-allocation wrapper around an Il2CppClass* pointer.
/// Provides null-safe lookups for methods, fields, properties, and metadata.
/// </summary>
public readonly record struct Il2CppClassRef(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public static implicit operator IntPtr(Il2CppClassRef r) => r.Pointer;
    public static implicit operator Il2CppClassRef(IntPtr p) => new(p);

    /// <summary>Find a class by fully qualified type name.</summary>
    /// <param name="fullTypeName">Namespace.ClassName</param>
    /// <returns>Class ref, or null ref if not found</returns>
    public static Il2CppClassRef Find(string fullTypeName)
        => Il2CppReflection.FindClass(fullTypeName);

    /// <summary>
    /// Try multiple type names, return the first that resolves.
    /// </summary>
    public static Il2CppClassRef FindAny(params string[] typeNames)
    {
        if (typeNames == null) return default;
        foreach (var name in typeNames)
        {
            IntPtr klass = Il2CppReflection.FindClass(name);
            if (klass != IntPtr.Zero) return klass;
        }
        return default;
    }

    // --- Method lookup ---

    /// <summary>Find a method by name and parameter count.</summary>
    /// <param name="name">Method name</param>
    /// <param name="paramCount">Expected parameter count, or -1 for any</param>
    /// <returns>Method ref, or null ref if not found</returns>
    public Il2CppMethodRef FindMethod(string name, int paramCount = -1)
        => IsNull ? default : Il2CppReflection.FindMethod(Pointer, name, paramCount);

    /// <summary>Find a method by name and parameter types (overload disambiguation).</summary>
    public Il2CppMethodRef FindMethod(string name, params Il2CppTypeEnum[] paramTypes)
        => IsNull ? default : Il2CppReflection.FindMethod(Pointer, name, paramTypes);

    /// <summary>Find a method by walking the class hierarchy.</summary>
    public Il2CppMethodRef FindMethodInHierarchy(string name, int paramCount = -1)
        => IsNull ? default : Il2CppReflection.FindMethodInHierarchy(Pointer, name, paramCount);

    /// <summary>
    /// Try multiple method names, return the first that resolves.
    /// Addresses the multi-name fallback pattern.
    /// </summary>
    /// <param name="names">Method names to try in order</param>
    /// <param name="paramCount">Expected parameter count, or -1 for any</param>
    /// <returns>First matching method ref, or null ref</returns>
    public Il2CppMethodRef FindMethodAny(string[] names, int paramCount = -1)
    {
        if (IsNull || names == null) return default;
        foreach (var name in names)
        {
            IntPtr m = Il2CppReflection.FindMethod(Pointer, name, paramCount);
            if (m != IntPtr.Zero) return m;
        }
        return default;
    }

    // --- Field/Property lookup ---

    /// <summary>Find a field by name.</summary>
    /// <returns>Field ref, or null ref if not found</returns>
    public Il2CppFieldRef FindField(string name)
        => IsNull ? default : Il2CppReflection.FindField(Pointer, name);

    /// <summary>Find a property by name.</summary>
    /// <returns>Property ref, or null ref if not found</returns>
    public Il2CppPropertyRef FindProperty(string name)
        => IsNull ? default : Il2CppReflection.FindProperty(Pointer, name);

    // --- Static field shortcuts ---

    /// <summary>Get a static field value (reference type).</summary>
    public IntPtr GetStaticFieldValue(string fieldName)
        => IsNull ? IntPtr.Zero : Il2CppReflection.GetStaticFieldValue(Pointer, fieldName);

    /// <summary>Get a static field value (value type).</summary>
    public T GetStaticFieldValue<T>(string fieldName) where T : unmanaged
        => IsNull ? default : Il2CppReflection.GetStaticFieldValue<T>(Pointer, fieldName);

    /// <summary>Set a static field value.</summary>
    public void SetStaticFieldValue<T>(string fieldName, T value) where T : unmanaged
    { if (!IsNull) Il2CppReflection.SetStaticFieldValue(Pointer, fieldName, value); }

    // --- Property shortcuts ---

    /// <summary>Read a value-type property from an instance.</summary>
    public T GetPropertyValue<T>(IntPtr instance, string propertyName) where T : unmanaged
        => IsNull ? default : Il2CppReflection.GetPropertyValue<T>(instance, Pointer, propertyName);

    /// <summary>Read a string property from an instance.</summary>
    public string GetPropertyString(IntPtr instance, string propertyName)
        => IsNull ? null : Il2CppReflection.GetPropertyString(instance, Pointer, propertyName);

    // --- Object creation ---

    /// <summary>Allocate and construct an instance (calls .ctor).</summary>
    /// <param name="ctorParamCount">Constructor parameter count</param>
    /// <param name="ctorArgs">Constructor argument pointers</param>
    /// <returns>Constructed object ref, or null ref if class is null</returns>
    public Il2CppObjectRef MakeInitializedInstance(int ctorParamCount = 0, IntPtr[] ctorArgs = null)
    {
        if (IsNull) return default;
        IntPtr obj = Il2CppAPI.CreateObject(Pointer);
        if (obj == IntPtr.Zero) return default;
        var ctor = Il2CppReflection.FindMethod(Pointer, ".ctor", ctorParamCount);
        if (ctor != IntPtr.Zero)
            Il2CppReflection.InvokeMethod(ctor, obj, ctorArgs);
        return new Il2CppObjectRef(obj);
    }

    /// <summary>Allocate an instance without calling .ctor.</summary>
    /// <returns>Uninitialized object wrapper, or null wrapper if class is null</returns>
    public Il2CppUninitializedObject MakeUninitializedInstance()
    {
        if (IsNull) return new Il2CppUninitializedObject(IntPtr.Zero);
        IntPtr obj = Il2CppAPI.CreateObject(Pointer);
        return new Il2CppUninitializedObject(obj);
    }

    // --- Static call shortcuts ---

    /// <summary>Find and invoke a static method by name.</summary>
    /// <param name="methodName">Method name</param>
    /// <param name="paramCount">Parameter count</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Boxed return value as object ref</returns>
    public Il2CppObjectRef CallStatic(string methodName, int paramCount = 0, IntPtr[] args = null)
    {
        if (IsNull) return default;
        var method = Il2CppReflection.FindMethod(Pointer, methodName, paramCount);
        if (method == IntPtr.Zero) return default;
        return Il2CppReflection.InvokeStaticMethod(method, args);
    }

    /// <summary>Find and invoke a static method, unbox the return value.</summary>
    public T CallStaticGet<T>(string methodName, int paramCount = 0, IntPtr[] args = null) where T : unmanaged
    {
        if (IsNull) return default;
        var method = Il2CppReflection.FindMethod(Pointer, methodName, paramCount);
        if (method == IntPtr.Zero) return default;
        return Il2CppReflection.InvokeGet<T>(method, IntPtr.Zero, args);
    }

    /// <summary>Find and invoke a static method, return the result as string.</summary>
    public string CallStaticGetString(string methodName, int paramCount = 0, IntPtr[] args = null)
    {
        if (IsNull) return null;
        var method = Il2CppReflection.FindMethod(Pointer, methodName, paramCount);
        if (method == IntPtr.Zero) return null;
        return Il2CppReflection.InvokeGetString(method, IntPtr.Zero, args);
    }

    // --- Boxing ---

    /// <summary>Box a value type using this class.</summary>
    public unsafe IntPtr Box<T>(T value) where T : unmanaged
        => IsNull ? IntPtr.Zero : Il2CppReflection.BoxValue(Pointer, value);

    // --- Metadata ---

    /// <summary>Class name (without namespace).</summary>
    public string Name => IsNull ? null : Il2CppAPI.GetClassName(Pointer);

    /// <summary>Class namespace.</summary>
    public string Namespace => IsNull ? null : Il2CppAPI.GetClassNamespace(Pointer);

    /// <summary>Parent class.</summary>
    public Il2CppClassRef Parent => IsNull ? default : Il2CppAPI.GetParentClass(Pointer);

    public override string ToString()
        => IsNull ? "Il2CppClassRef(null)" : $"Il2CppClassRef({Namespace}.{Name})";
}
