using System;

namespace IL2CppSharp;

/// <summary>
/// High-level entry point for fluent IL2CPP access.
/// </summary>
public static class IL2CPP
{
    /// <summary>
    /// Initialize the reflection cache.
    /// </summary>
    public static bool Initialize()
        => Il2CppReflection.Initialize();

    public static void AddAssemblyHint(string assemblyName)
        => Il2CppReflection.AddAssemblyHint(assemblyName);

    public static Il2CppClassHandle Class(string fullTypeName)
        => new(fullTypeName, required: false);

    public static Il2CppClassHandle Class(IntPtr classPointer)
        => new(classPointer, required: false);

    public static Il2CppClassHandle RequireClass(string fullTypeName)
        => new Il2CppClassHandle(fullTypeName, required: true).Require();

    public static bool TryClass(string fullTypeName, out Il2CppClassHandle klass)
    {
        klass = Class(fullTypeName);
        return klass.TryResolve(out _);
    }

    public static Il2CppMethodHandle Method(IntPtr methodInfo)
        => new(methodInfo, required: false);

    public static Il2CppMethodHandle RequireMethod(IntPtr methodInfo)
        => new Il2CppMethodHandle(methodInfo, required: true).Require();

    public static Il2CppObjectRef Object(IntPtr pointer)
        => new(pointer);

    public static Il2CppListHandle List(IntPtr pointer)
        => new(pointer);

    public static Il2CppDictionaryHandle Dictionary(IntPtr pointer)
        => new(pointer);

    public static IntPtr String(string value)
        => Il2CppReflection.CreateIl2CppString(value);

    public static IntPtr Array<T>(T[] values) where T : unmanaged
        => Il2CppArrayMarshaller.Create(values);

    public static IntPtr Array(string[] values)
        => Il2CppArrayMarshaller.CreateStringArray(values);

    public static T[] ReadArray<T>(IntPtr il2cppArray) where T : unmanaged
        => Il2CppReflection.ReadArray<T>(il2cppArray);

    public static byte[] ReadByteArray(IntPtr il2cppArray)
        => Il2CppReflection.ReadByteArray(il2cppArray);
}
