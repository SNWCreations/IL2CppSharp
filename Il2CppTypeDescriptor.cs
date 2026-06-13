using System;
using System.Collections.Generic;

namespace IL2CppSharp;

/// <summary>
/// Describes an IL2CPP method parameter for high-level overload resolution.
/// </summary>
public sealed class Il2CppTypeDescriptor
{
    private Il2CppTypeDescriptor(
        Type managedType,
        Il2CppTypeEnum typeEnum,
        string fullTypeName = null,
        Il2CppTypeDescriptor elementType = null,
        bool isRawPointer = false)
    {
        ManagedType = managedType;
        TypeEnum = typeEnum;
        FullTypeName = fullTypeName;
        ElementType = elementType;
        IsRawPointer = isRawPointer;
    }

    public Type ManagedType { get; }

    public Il2CppTypeEnum TypeEnum { get; }

    public string FullTypeName { get; }

    public Il2CppTypeDescriptor ElementType { get; }

    public bool IsRawPointer { get; }

    internal string CacheKey
    {
        get
        {
            if (IsRawPointer) return "ptr";
            if (ElementType != null) return $"{TypeEnum}:{ElementType.CacheKey}";
            return $"{TypeEnum}:{FullTypeName ?? ManagedType?.FullName ?? string.Empty}";
        }
    }

    public static Il2CppTypeDescriptor From<T>()
        => FromType(typeof(T));

    public static Il2CppTypeDescriptor FromType(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (type == typeof(void)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_VOID, "System.Void");
        if (type == typeof(bool)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN, "System.Boolean");
        if (type == typeof(char)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_CHAR, "System.Char");
        if (type == typeof(sbyte)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_I1, "System.SByte");
        if (type == typeof(byte)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_U1, "System.Byte");
        if (type == typeof(short)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_I2, "System.Int16");
        if (type == typeof(ushort)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_U2, "System.UInt16");
        if (type == typeof(int)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_I4, "System.Int32");
        if (type == typeof(uint)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_U4, "System.UInt32");
        if (type == typeof(long)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_I8, "System.Int64");
        if (type == typeof(ulong)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_U8, "System.UInt64");
        if (type == typeof(float)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_R4, "System.Single");
        if (type == typeof(double)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_R8, "System.Double");
        if (type == typeof(string)) return new(type, Il2CppTypeEnum.IL2CPP_TYPE_STRING, "System.String");

        if (type == typeof(IntPtr) || type == typeof(Il2CppObjectRef))
            return new(type, Il2CppTypeEnum.IL2CPP_TYPE_OBJECT, isRawPointer: true);

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Only one-dimensional IL2CPP arrays are supported.");

            return new(
                type,
                Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY,
                elementType: FromType(type.GetElementType()));
        }

        if (type.IsEnum || type.IsValueType)
            return new(type, Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE, type.FullName);

        return new(type, Il2CppTypeEnum.IL2CPP_TYPE_CLASS, type.FullName);
    }

    internal static Il2CppTypeDescriptor[] FromTypes(Type[] types)
    {
        if (types == null || types.Length == 0) return [];

        var result = new Il2CppTypeDescriptor[types.Length];
        for (int i = 0; i < types.Length; i++)
            result[i] = FromType(types[i]);
        return result;
    }

    internal IntPtr ResolveClassPointer()
    {
        if (TypeEnum == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY)
            return IntPtr.Zero;

        string name = FullTypeName ?? ManagedType?.FullName;
        return string.IsNullOrEmpty(name) ? IntPtr.Zero : Il2CppReflection.FindClass(name);
    }

    public override string ToString()
        => CacheKey;
}

internal static class Il2CppMethodResolver
{
    private static readonly Dictionary<string, IntPtr> MethodCache = [];
    private static readonly object CacheLock = new();

    public static IntPtr FindMethod(
        IntPtr klass,
        string methodName,
        Il2CppTypeDescriptor[] parameters,
        bool anyParameterList)
    {
        if (klass == IntPtr.Zero || string.IsNullOrEmpty(methodName))
            return IntPtr.Zero;

        if (anyParameterList)
            return Il2CppReflection.FindMethod(klass, methodName, -1);

        parameters ??= [];
        string cacheKey = BuildCacheKey(klass, methodName, parameters);

        lock (CacheLock)
        {
            if (MethodCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        IntPtr iter = IntPtr.Zero;
        IntPtr method;
        while ((method = Il2CppAPI.il2cpp_class_get_methods(klass, ref iter)) != IntPtr.Zero)
        {
            string name = Il2CppMethodName(method);
            if (name != methodName) continue;

            int paramCount = Il2CppAPI.il2cpp_method_get_param_count(method);
            if (paramCount != parameters.Length) continue;

            bool match = true;
            for (int i = 0; i < paramCount; i++)
            {
                IntPtr paramType = Il2CppAPI.il2cpp_method_get_param(method, (uint)i);
                if (!MatchesParameter(paramType, parameters[i]))
                {
                    match = false;
                    break;
                }
            }

            if (!match) continue;

            lock (CacheLock) { MethodCache[cacheKey] = method; }
            return method;
        }

        return IntPtr.Zero;
    }

    private static string BuildCacheKey(
        IntPtr klass,
        string methodName,
        Il2CppTypeDescriptor[] parameters)
        => $"{klass.ToInt64():X}|{methodName}|{Describe(parameters)}";

    private static string Describe(Il2CppTypeDescriptor[] parameters)
    {
        if (parameters == null || parameters.Length == 0) return string.Empty;
        return string.Join(",", Array.ConvertAll(parameters, p => p.CacheKey));
    }

    private static string Il2CppMethodName(IntPtr method)
    {
        IntPtr namePtr = Il2CppAPI.il2cpp_method_get_name(method);
        return namePtr == IntPtr.Zero ? null : System.Runtime.InteropServices.Marshal.PtrToStringAnsi(namePtr);
    }

    private static bool MatchesParameter(IntPtr paramType, Il2CppTypeDescriptor expected)
    {
        if (paramType == IntPtr.Zero || expected == null) return false;
        if (expected.IsRawPointer) return true;

        var actual = (Il2CppTypeEnum)Il2CppAPI.il2cpp_type_get_type(paramType);
        if (expected.TypeEnum == Il2CppTypeEnum.IL2CPP_TYPE_STRING)
        {
            if (actual == Il2CppTypeEnum.IL2CPP_TYPE_STRING) return true;
            return MatchesClassName(Il2CppAPI.il2cpp_class_from_type(paramType), "System.String");
        }

        if (expected.TypeEnum == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY)
        {
            if (actual != Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY &&
                actual != Il2CppTypeEnum.IL2CPP_TYPE_ARRAY)
                return false;

            IntPtr arrayClass = Il2CppAPI.il2cpp_class_from_type(paramType);
            IntPtr elementClass = arrayClass == IntPtr.Zero
                ? IntPtr.Zero
                : Il2CppAPI.il2cpp_class_get_element_class(arrayClass);
            return MatchesClassDescriptor(elementClass, expected.ElementType);
        }

        if (IsValueTypeCompatible(expected.TypeEnum, actual) ||
            expected.TypeEnum == actual)
        {
            if (expected.FullTypeName == null) return true;
            IntPtr paramClass = Il2CppAPI.il2cpp_class_from_type(paramType);
            return MatchesClassName(paramClass, expected.FullTypeName);
        }

        return false;
    }

    private static bool IsValueTypeCompatible(Il2CppTypeEnum expected, Il2CppTypeEnum actual)
        => expected == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE &&
           (actual == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE ||
            actual == Il2CppTypeEnum.IL2CPP_TYPE_ENUM);

    private static bool MatchesClassDescriptor(IntPtr klass, Il2CppTypeDescriptor expected)
    {
        if (expected == null) return false;
        if (expected.IsRawPointer) return true;

        if (!string.IsNullOrEmpty(expected.FullTypeName))
            return MatchesClassName(klass, expected.FullTypeName);

        IntPtr expectedClass = expected.ResolveClassPointer();
        return expectedClass != IntPtr.Zero && klass == expectedClass;
    }

    private static bool MatchesClassName(IntPtr klass, string fullTypeName)
    {
        if (klass == IntPtr.Zero || string.IsNullOrEmpty(fullTypeName)) return false;

        string ns = Il2CppAPI.GetClassNamespace(klass) ?? string.Empty;
        string name = Il2CppAPI.GetClassName(klass);
        string actual = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        return string.Equals(actual, fullTypeName, StringComparison.Ordinal);
    }
}
