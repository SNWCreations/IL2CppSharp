using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IL2CppSharp;

/// <summary>
/// Unified IL2CPP reflection utilities for working with both AOT and HybridCLR hot-update types.
/// Provides type/method/field lookup with caching, method invocation, and type conversion.
/// </summary>
public static unsafe class Il2CppReflection
{
    #region Assembly Hints

    private static readonly List<string> _assemblyHints = [];

    /// <summary>
    /// Add an assembly name hint for type resolution (e.g., "AstralParty.Runtime").
    /// Used by class/type lookup helpers to search game-specific assemblies.
    /// </summary>
    public static void AddAssemblyHint(string assemblyName)
    {
        if (!string.IsNullOrEmpty(assemblyName) && !_assemblyHints.Contains(assemblyName))
            _assemblyHints.Add(assemblyName);
    }

    public static IReadOnlyList<string> AssemblyHints => _assemblyHints;

    #endregion

    #region Cache

    private static readonly Dictionary<string, IntPtr> _classCache = [];
    private static readonly Dictionary<(IntPtr, string, int), IntPtr> _methodCache = [];
    private static readonly object _cacheLock = new();
    private static bool _initialized;
    private static List<IntPtr> _loadedImages;

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the IL2CPP reflection system.
    /// Call this after HybridCLR has loaded hot-update assemblies.
    /// </summary>
    public static bool Initialize()
    {
        if (_initialized) return true;

        try
        {
            RefreshImageCache();
            _initialized = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Refresh the cached list of loaded IL2CPP images/assemblies.
    /// </summary>
    public static void RefreshImageCache()
    {
        lock (_cacheLock)
        {
            _loadedImages = [];
            _classCache.Clear();
            _methodCache.Clear();

            IntPtr domain = Il2CppAPI.il2cpp_domain_get();
            if (domain == IntPtr.Zero)
                return;

            uint assemblyCount = 0;
            IntPtr assembliesPtr = Il2CppAPI.il2cpp_domain_get_assemblies(domain, ref assemblyCount);

            for (uint i = 0; i < assemblyCount; i++)
            {
                IntPtr assembly = ((IntPtr*)assembliesPtr)[i];
                if (assembly == IntPtr.Zero) continue;

                IntPtr image = Il2CppAPI.il2cpp_assembly_get_image(assembly);
                if (image == IntPtr.Zero) continue;

                _loadedImages.Add(image);
            }
        }
    }

    #endregion

    #region Type Lookup

    /// <summary>
    /// Find an IL2CPP class by full type name (e.g., "Core.Scene.LoginSceneController").
    /// </summary>
    /// <param name="fullTypeName">Fully qualified type name (namespace.class)</param>
    /// <returns>Il2CppClass* pointer, or IntPtr.Zero if not found</returns>
    public static IntPtr FindClass(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
            return IntPtr.Zero;

        lock (_cacheLock)
        {
            if (_classCache.TryGetValue(fullTypeName, out var cached))
                return cached;
        }

        int lastDot = fullTypeName.LastIndexOf('.');
        string namespaceName = lastDot > 0 ? fullTypeName.Substring(0, lastDot) : "";
        string className = lastDot > 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;

        IntPtr result = FindClassInternal(namespaceName, className);

        if (result == IntPtr.Zero)
        {
            RefreshImageCache();
            result = FindClassInternal(namespaceName, className);
        }

        if (result != IntPtr.Zero)
        {
            lock (_cacheLock) { _classCache[fullTypeName] = result; }
        }

        return result;
    }

    private static IntPtr FindClassInternal(string namespaceName, string className)
    {
        if (_loadedImages == null)
            RefreshImageCache();

        foreach (var image in _loadedImages)
        {
            IntPtr klass = Il2CppAPI.il2cpp_class_from_name(image, namespaceName, className);
            if (klass != IntPtr.Zero)
                return klass;
        }

        // Fallback: enumerate all classes
        foreach (var image in _loadedImages)
        {
            uint classCount = Il2CppAPI.il2cpp_image_get_class_count(image);
            for (uint i = 0; i < classCount; i++)
            {
                IntPtr klass = Il2CppAPI.il2cpp_image_get_class(image, i);
                if (klass == IntPtr.Zero) continue;

                string klassName = Marshal.PtrToStringAnsi(Il2CppAPI.il2cpp_class_get_name(klass));
                string klassNamespace = Marshal.PtrToStringAnsi(Il2CppAPI.il2cpp_class_get_namespace(klass));

                if (klassName == className && klassNamespace == namespaceName)
                    return klass;
            }
        }

        return IntPtr.Zero;
    }
    #endregion

    #region Method Lookup

    /// <summary>
    /// Find a method in an IL2CPP class by name.
    /// </summary>
    /// <param name="klassPtr">Il2CppClass* pointer</param>
    /// <param name="methodName">Method name to search for</param>
    /// <param name="paramCount">Expected parameter count, or -1 to match any</param>
    /// <returns>MethodInfo* pointer, or IntPtr.Zero if not found</returns>
    public static IntPtr FindMethod(IntPtr klassPtr, string methodName, int paramCount = -1)
    {
        if (klassPtr == IntPtr.Zero || string.IsNullOrEmpty(methodName))
            return IntPtr.Zero;

        var cacheKey = (klassPtr, methodName, paramCount);
        lock (_cacheLock)
        {
            if (_methodCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        IntPtr iter = IntPtr.Zero;
        IntPtr method;

        while ((method = Il2CppAPI.il2cpp_class_get_methods(klassPtr, ref iter)) != IntPtr.Zero)
        {
            string name = Marshal.PtrToStringAnsi(Il2CppAPI.il2cpp_method_get_name(method));
            if (name != methodName) continue;

            if (paramCount >= 0)
            {
                int actualParamCount = Il2CppAPI.il2cpp_method_get_param_count(method);
                if (actualParamCount != paramCount) continue;
            }

            lock (_cacheLock) { _methodCache[cacheKey] = method; }
            return method;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Find a method by full type name and method name.
    /// </summary>
    /// <param name="fullTypeName">Fully qualified type name</param>
    /// <param name="methodName">Method name to search for</param>
    /// <param name="paramCount">Expected parameter count, or -1 to match any</param>
    /// <returns>MethodInfo* pointer, or IntPtr.Zero if not found</returns>
    public static IntPtr FindMethod(string fullTypeName, string methodName, int paramCount = -1)
    {
        IntPtr klass = FindClass(fullTypeName);
        if (klass == IntPtr.Zero)
            return IntPtr.Zero;
        return FindMethod(klass, methodName, paramCount);
    }

    #endregion

    #region Method Invocation

    /// <summary>
    /// Create an IL2CPP string from a managed string using UTF-16 (no ANSI truncation).
    /// </summary>
    /// <param name="str">Managed string</param>
    /// <returns>Il2CppString* pointer, or IntPtr.Zero if str is null</returns>
    public static IntPtr CreateIl2CppString(string str)
    {
        if (str == null) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_string_new_utf16(str, str.Length);
    }

    /// <summary>
    /// Invoke a static method with no arguments.
    /// </summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <returns>Boxed return value (Il2CppObject*), or IntPtr.Zero on failure/void return</returns>
    public static IntPtr InvokeStaticMethod(IntPtr methodInfo)
    {
        return InvokeStaticMethod(methodInfo, null);
    }

    private static IntPtr InvokeRaw(IntPtr methodInfo, IntPtr instance, void** args)
    {
        if (methodInfo == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr exception = IntPtr.Zero;
        IntPtr result = Il2CppAPI.il2cpp_runtime_invoke(methodInfo, instance, args, ref exception);
        return exception == IntPtr.Zero ? result : IntPtr.Zero;
    }

    /// <summary>
    /// Resolve args for il2cpp_runtime_invoke: for value type params,
    /// if the caller passed a boxed object, unbox it to get the data pointer.
    /// il2cpp_runtime_invoke expects raw value pointers for value types, but
    /// BoxValue returns boxed Il2CppObject* — passing that directly causes
    /// the runtime to read the object header as the value (silent corruption).
    /// </summary>
    private static IntPtr[] ResolveInvokeArgs(IntPtr methodInfo, IntPtr[] args)
    {
        if (args == null || args.Length == 0) return args;

        int paramCount = Il2CppAPI.il2cpp_method_get_param_count(methodInfo);
        if (paramCount != args.Length) return args;

        IntPtr[] resolved = null;

        for (int i = 0; i < paramCount; i++)
        {
            if (args[i] == IntPtr.Zero) continue;

            IntPtr paramType = Il2CppAPI.il2cpp_method_get_param(methodInfo, (uint)i);
            if (paramType == IntPtr.Zero) continue;

            IntPtr paramClass = Il2CppAPI.il2cpp_class_from_type(paramType);
            if (paramClass == IntPtr.Zero || !Il2CppAPI.il2cpp_class_is_valuetype(paramClass))
                continue;

            // Value type param: check if arg is a boxed object of the matching class
            IntPtr objClass = Il2CppAPI.il2cpp_object_get_class(args[i]);
            if (objClass == paramClass)
            {
                if (resolved == null)
                {
                    resolved = new IntPtr[args.Length];
                    Array.Copy(args, resolved, args.Length);
                }
                resolved[i] = Il2CppAPI.il2cpp_object_unbox(args[i]);
            }
        }

        return resolved ?? args;
    }

    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="args">Array of argument pointers (each pointing to the arg value)</param>
    /// <returns>Boxed return value (Il2CppObject*), or IntPtr.Zero on failure/void return</returns>
    public static IntPtr InvokeStaticMethod(IntPtr methodInfo, IntPtr[] args)
    {
        if (args == null || args.Length == 0)
            return InvokeRaw(methodInfo, IntPtr.Zero, null);

        args = ResolveInvokeArgs(methodInfo, args);
        fixed (IntPtr* argsPtr = args)
        {
            return InvokeRaw(methodInfo, IntPtr.Zero, (void**)argsPtr);
        }
    }

    /// <summary>
    /// Invoke an instance or static method with arguments.
    /// </summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance pointer (IntPtr.Zero for static)</param>
    /// <param name="args">Array of argument pointers</param>
    /// <returns>Boxed return value (Il2CppObject*), or IntPtr.Zero on failure/void return</returns>
    public static IntPtr InvokeMethod(IntPtr methodInfo, IntPtr instance, IntPtr[] args)
    {
        if (args == null || args.Length == 0)
            return InvokeRaw(methodInfo, instance, null);

        args = ResolveInvokeArgs(methodInfo, args);
        fixed (IntPtr* argsPtr = args)
        {
            return InvokeRaw(methodInfo, instance, (void**)argsPtr);
        }
    }

    /// <summary>
    /// Find and invoke a static method by type name and method name.
    /// </summary>
    /// <param name="fullTypeName">Fully qualified type name</param>
    /// <param name="methodName">Method name</param>
    /// <param name="args">Argument pointers</param>
    /// <returns>Boxed return value (Il2CppObject*), or IntPtr.Zero on failure</returns>
    public static IntPtr InvokeStaticMethod(string fullTypeName, string methodName, params IntPtr[] args)
    {
        IntPtr methodInfo = FindMethod(fullTypeName, methodName, args?.Length ?? 0);
        if (methodInfo == IntPtr.Zero)
            return IntPtr.Zero;
        return InvokeStaticMethod(methodInfo, args);
    }

    /// <summary>
    /// Invoke a static method with a single string argument.
    /// </summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="value">String argument value</param>
    /// <returns>Boxed return value (Il2CppObject*), or IntPtr.Zero on failure</returns>
    public static IntPtr InvokeStaticMethodWithString(IntPtr methodInfo, string value)
    {
        IntPtr il2cppString = CreateIl2CppString(value ?? "");
        void* argPtr = (void*)il2cppString;
        return InvokeRaw(methodInfo, IntPtr.Zero, &argPtr);
    }

    /// <summary>
    /// Get the Il2CppClass* of an object instance.
    /// </summary>
    /// <param name="obj">Il2CppObject* pointer</param>
    /// <returns>Il2CppClass* pointer, or IntPtr.Zero if obj is null</returns>
    public static IntPtr GetObjectClass(IntPtr obj)
    {
        if (obj == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_object_get_class(obj);
    }

    /// <summary>Invoke an instance method with a bool argument.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="value">Bool argument</param>
    /// <returns>Boxed return value (Il2CppObject*)</returns>
    public static IntPtr InvokeMethodWithBool(IntPtr methodInfo, IntPtr instance, bool value)
    {
        byte boolValue = value ? (byte)1 : (byte)0;
        void* argPtr = &boolValue;
        return InvokeRaw(methodInfo, instance, &argPtr);
    }

    /// <summary>Invoke an instance method with an int argument.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="value">Int argument</param>
    /// <returns>Boxed return value (Il2CppObject*)</returns>
    public static IntPtr InvokeMethodWithInt(IntPtr methodInfo, IntPtr instance, int value)
    {
        void* argPtr = &value;
        return InvokeRaw(methodInfo, instance, &argPtr);
    }

    /// <summary>Invoke an instance method with a float argument.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="value">Float argument</param>
    /// <returns>Boxed return value (Il2CppObject*)</returns>
    public static IntPtr InvokeMethodWithFloat(IntPtr methodInfo, IntPtr instance, float value)
    {
        void* argPtr = &value;
        return InvokeRaw(methodInfo, instance, &argPtr);
    }

    /// <summary>Invoke an instance method with a string argument.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="value">String argument</param>
    /// <returns>Boxed return value (Il2CppObject*)</returns>
    public static IntPtr InvokeMethodWithString(IntPtr methodInfo, IntPtr instance, string value)
    {
        IntPtr il2cppString = CreateIl2CppString(value ?? "");
        void* argPtr = (void*)il2cppString;
        return InvokeRaw(methodInfo, instance, &argPtr);
    }

    /// <summary>Invoke an instance method with two float arguments.</summary>
    public static IntPtr InvokeMethodWith2Floats(IntPtr methodInfo, IntPtr instance, float value1, float value2)
    {
        void*[] argPtrs = new void*[2];
        float v1 = value1, v2 = value2;
        argPtrs[0] = &v1;
        argPtrs[1] = &v2;
        fixed (void** args = argPtrs)
        {
            return InvokeRaw(methodInfo, instance, args);
        }
    }

    /// <summary>Invoke an instance method with a string and float argument.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="strValue">String argument</param>
    /// <param name="floatValue">Float argument</param>
    /// <returns>Boxed return value (Il2CppObject*)</returns>
    public static IntPtr InvokeMethodWithStringAndFloat(IntPtr methodInfo, IntPtr instance, string strValue, float floatValue)
    {
        IntPtr il2cppString = CreateIl2CppString(strValue ?? "");
        void*[] argPtrs = new void*[2];
        argPtrs[0] = (void*)il2cppString;
        float floatVal = floatValue;
        argPtrs[1] = &floatVal;
        fixed (void** args = argPtrs)
        {
            return InvokeRaw(methodInfo, instance, args);
        }
    }

    /// <summary>Invoke an instance method and return the result as int.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Unboxed int return value, or 0 on failure</returns>
    public static int InvokeMethodGetInt(IntPtr methodInfo, IntPtr instance)
    {
        return InvokeGet<int>(methodInfo, instance);
    }

    /// <summary>Invoke an instance method and return the result as bool.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Unboxed bool return value, or false on failure</returns>
    public static bool InvokeMethodGetBool(IntPtr methodInfo, IntPtr instance)
    {
        return InvokeGet<byte>(methodInfo, instance) != 0;
    }

    /// <summary>Invoke an instance method with an int param and return the result as string.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <param name="param">Int argument</param>
    /// <returns>Managed string, or null on failure</returns>
    public static string InvokeMethodGetString(IntPtr methodInfo, IntPtr instance, int param)
    {
        IntPtr result = InvokeMethodWithInt(methodInfo, instance, param);
        return result == IntPtr.Zero ? null : Il2CppAPI.Il2CppStringToManaged(result);
    }

    /// <summary>Invoke an instance method with no params and return the result as string.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Managed string, or null on failure</returns>
    public static string InvokeMethodGetStringNoParam(IntPtr methodInfo, IntPtr instance)
    {
        return InvokeGetString(methodInfo, instance);
    }

    /// <summary>Invoke an instance method and return the result as float.</summary>
    /// <param name="methodInfo">MethodInfo* pointer</param>
    /// <param name="instance">Il2CppObject* instance</param>
    /// <returns>Unboxed float return value, or 0f on failure</returns>
    public static float InvokeMethodGetFloat(IntPtr methodInfo, IntPtr instance)
    {
        return InvokeGet<float>(methodInfo, instance);
    }

    #endregion

    #region IL2CPP Type Inspection

    private const Il2CppTypeEnum IL2CPP_TYPE_VOID = Il2CppTypeEnum.IL2CPP_TYPE_VOID;
    private const Il2CppTypeEnum IL2CPP_TYPE_VALUETYPE = Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE;

    /// <summary>
    /// Method signature info for invoker generation.
    /// </summary>
    public struct MethodSignatureInfo
    {
        public int ParamCount;
        public Il2CppTypeEnum[] ParamTypeEnums;
        public int[] ParamValueSizes; // >0 for value types, 0 for ref types
        public Il2CppTypeEnum ReturnTypeEnum;
        public int ReturnValueSize; // >0 for value types, 0 for ref/void
        public bool IsInstance;
        public bool IsVoidReturn;
        public bool HasLargeStructReturn; // return value type > 8 bytes
    }

    /// <summary>
    /// Inspect a MethodInfo to extract signature information for invoker generation.
    /// </summary>
    public static MethodSignatureInfo GetMethodSignatureInfo(IntPtr methodInfoPtr)
    {
        var info = new MethodSignatureInfo();

        int paramCount = Il2CppAPI.il2cpp_method_get_param_count(methodInfoPtr);
        info.ParamCount = paramCount;
        info.ParamTypeEnums = new Il2CppTypeEnum[paramCount];
        info.ParamValueSizes = new int[paramCount];

        for (int i = 0; i < paramCount; i++)
        {
            IntPtr paramType = Il2CppAPI.il2cpp_method_get_param(methodInfoPtr, (uint)i);
            int typeEnum = Il2CppAPI.il2cpp_type_get_type(paramType);
            info.ParamTypeEnums[i] = (Il2CppTypeEnum)typeEnum;

            if (info.ParamTypeEnums[i] == IL2CPP_TYPE_VALUETYPE)
            {
                IntPtr klass = Il2CppAPI.il2cpp_class_from_type(paramType);
                uint align = 0;
                int size = Il2CppAPI.il2cpp_class_value_size(klass, ref align);
                info.ParamValueSizes[i] = size;
            }
        }

        IntPtr retType = Il2CppAPI.il2cpp_method_get_return_type(methodInfoPtr);
        info.ReturnTypeEnum = (Il2CppTypeEnum)Il2CppAPI.il2cpp_type_get_type(retType);
        info.IsVoidReturn = info.ReturnTypeEnum == IL2CPP_TYPE_VOID;

        if (info.ReturnTypeEnum == IL2CPP_TYPE_VALUETYPE)
        {
            IntPtr retKlass = Il2CppAPI.il2cpp_class_from_type(retType);
            uint align = 0;
            int size = Il2CppAPI.il2cpp_class_value_size(retKlass, ref align);
            info.ReturnValueSize = size;
            info.HasLargeStructReturn = size > 8;
        }

        // Check instance flag via method flags
        uint iflags = 0;
        uint flags = Il2CppAPI.il2cpp_method_get_flags(methodInfoPtr, ref iflags);
        const uint METHOD_ATTRIBUTE_STATIC = 0x0010;
        info.IsInstance = (flags & METHOD_ATTRIBUTE_STATIC) == 0;

        return info;
    }

    /// <summary>
    /// Compute a signature hash for invoker caching.
    /// </summary>
    public static int ComputeSignatureHash(in MethodSignatureInfo sig)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + sig.ParamCount;
            hash = hash * 31 + (int)sig.ReturnTypeEnum;
            hash = hash * 31 + sig.ReturnValueSize;
            hash = hash * 31 + (sig.IsInstance ? 1 : 0);
            for (int i = 0; i < sig.ParamCount; i++)
            {
                hash = hash * 31 + (int)sig.ParamTypeEnums[i];
                hash = hash * 31 + sig.ParamValueSizes[i];
            }
            return hash;
        }
    }

    #endregion

    #region Field and Array Utilities

    /// <summary>
    /// Find a field on an IL2CPP class by name.
    /// </summary>
    /// <param name="klass">Il2CppClass* pointer</param>
    /// <param name="fieldName">Field name</param>
    /// <returns>FieldInfo* pointer, or IntPtr.Zero if not found</returns>
    public static IntPtr FindField(IntPtr klass, string fieldName)
    {
        if (klass == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_class_get_field_from_name(klass, fieldName);
    }

    /// <summary>
    /// Read a pointer-sized field value from an IL2CPP object.
    /// </summary>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <param name="field">FieldInfo* pointer</param>
    /// <returns>Field value as IntPtr (for reference type fields)</returns>
    public static IntPtr GetFieldValue(IntPtr obj, IntPtr field)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return IntPtr.Zero;
        IntPtr value = IntPtr.Zero;
        Il2CppAPI.il2cpp_field_get_value(obj, field, &value);
        return value;
    }

    /// <summary>
    /// Read a field value into an arbitrary buffer.
    /// </summary>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <param name="field">FieldInfo* pointer</param>
    /// <param name="outValue">Pointer to output buffer</param>
    public static void GetFieldValueRaw(IntPtr obj, IntPtr field, void* outValue)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return;
        Il2CppAPI.il2cpp_field_get_value(obj, field, outValue);
    }

    /// <summary>
    /// Set a bool field on an IL2CPP object.
    /// </summary>
    /// <param name="obj">Il2CppObject* instance</param>
    /// <param name="field">FieldInfo* pointer</param>
    /// <param name="value">Bool value to set</param>
    public static void SetFieldValueBool(IntPtr obj, IntPtr field, bool value)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return;
        byte boolValue = value ? (byte)1 : (byte)0;
        Il2CppAPI.il2cpp_field_set_value(obj, field, &boolValue);
    }

    public static byte[] ReadByteArray(IntPtr il2cppArray)
    {
        if (il2cppArray == IntPtr.Zero) return null;
        try
        {
            uint length = Il2CppAPI.il2cpp_array_length(il2cppArray);
            if (length == 0) return [];

            byte[] result = new byte[length];
            IntPtr dataPtr = il2cppArray + Il2CppAPI.il2cpp_array_object_header_size();
            Marshal.Copy(dataPtr, result, 0, (int)length);
            return result;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Unboxing and Array Access

    /// <summary>
    /// Unbox a boxed IL2CPP value type object to a managed unmanaged type.
    /// </summary>
    public static T UnboxValue<T>(IntPtr boxedObj) where T : unmanaged
    {
        if (boxedObj == IntPtr.Zero) return default;
        IntPtr valuePtr = Il2CppAPI.il2cpp_object_unbox(boxedObj);
        if (valuePtr == IntPtr.Zero) return default;
        return *(T*)valuePtr;
    }

    /// <summary>
    /// Read an IL2CPP array into a managed array.
    /// Uses il2cpp_array_object_header_size() for runtime-verified data offset.
    /// </summary>
    public static T[] ReadArray<T>(IntPtr il2cppArray) where T : unmanaged
    {
        if (il2cppArray == IntPtr.Zero) return null;
        uint length = Il2CppAPI.il2cpp_array_length(il2cppArray);
        if (length == 0) return [];
        int headerSize = Il2CppAPI.il2cpp_array_object_header_size();
        T* dataPtr = (T*)(il2cppArray + headerSize);
        T[] result = new T[length];
        for (uint i = 0; i < length; i++)
            result[i] = dataPtr[i];
        return result;
    }

    /// <summary>
    /// Box a value type into an IL2CPP object.
    /// </summary>
    public static IntPtr BoxValue<T>(IntPtr klass, T value) where T : unmanaged
    {
        if (klass == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&value));
    }

    #endregion

    #region Return Value Helpers

    /// <summary>
    /// Invoke a method and unbox the return value.
    /// </summary>
    public static T InvokeGet<T>(IntPtr methodInfo, IntPtr instance, IntPtr[] args = null) where T : unmanaged
    {
        IntPtr result = InvokeMethod(methodInfo, instance, args);
        if (result == IntPtr.Zero) return default;
        IntPtr valuePtr = Il2CppAPI.il2cpp_object_unbox(result);
        if (valuePtr == IntPtr.Zero) return default;
        return *(T*)valuePtr;
    }

    /// <summary>
    /// Invoke a method and return the result as a managed string.
    /// </summary>
    public static string InvokeGetString(IntPtr methodInfo, IntPtr instance, IntPtr[] args = null)
    {
        IntPtr result = InvokeMethod(methodInfo, instance, args);
        if (result == IntPtr.Zero) return null;
        return Il2CppAPI.Il2CppStringToManaged(result);
    }

    #endregion

    #region Property Accessors

    /// <summary>
    /// Find a property on an IL2CPP class by name.
    /// </summary>
    public static IntPtr FindProperty(IntPtr klass, string propertyName)
    {
        if (klass == IntPtr.Zero || string.IsNullOrEmpty(propertyName)) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_class_get_property_from_name(klass, propertyName);
    }

    /// <summary>
    /// Get the getter MethodInfo* for a property.
    /// </summary>
    public static IntPtr GetPropertyGetter(IntPtr property)
    {
        if (property == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_property_get_get_method(property);
    }

    /// <summary>
    /// Get the setter MethodInfo* for a property.
    /// </summary>
    public static IntPtr GetPropertySetter(IntPtr property)
    {
        if (property == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_property_get_set_method(property);
    }

    /// <summary>
    /// Read a value-type property from an instance.
    /// </summary>
    public static T GetPropertyValue<T>(IntPtr instance, IntPtr klass, string propertyName) where T : unmanaged
    {
        IntPtr prop = FindProperty(klass, propertyName);
        if (prop == IntPtr.Zero) return default;
        IntPtr getter = GetPropertyGetter(prop);
        if (getter == IntPtr.Zero) return default;
        return InvokeGet<T>(getter, instance);
    }

    /// <summary>
    /// Read a string property from an instance.
    /// </summary>
    public static string GetPropertyString(IntPtr instance, IntPtr klass, string propertyName)
    {
        IntPtr prop = FindProperty(klass, propertyName);
        if (prop == IntPtr.Zero) return null;
        IntPtr getter = GetPropertyGetter(prop);
        if (getter == IntPtr.Zero) return null;
        return InvokeGetString(getter, instance);
    }

    #endregion

    #region Static Field Access

    /// <summary>
    /// Get a static field value as a pointer (for reference types).
    /// </summary>
    public static IntPtr GetStaticFieldValue(IntPtr klass, string fieldName)
    {
        IntPtr field = FindField(klass, fieldName);
        if (field == IntPtr.Zero) return IntPtr.Zero;
        IntPtr value = IntPtr.Zero;
        Il2CppAPI.il2cpp_field_static_get_value(field, &value);
        return value;
    }

    /// <summary>
    /// Get a static field value as an unmanaged type.
    /// </summary>
    public static T GetStaticFieldValue<T>(IntPtr klass, string fieldName) where T : unmanaged
    {
        IntPtr field = FindField(klass, fieldName);
        if (field == IntPtr.Zero) return default;
        T value = default;
        Il2CppAPI.il2cpp_field_static_get_value(field, &value);
        return value;
    }

    /// <summary>
    /// Set a static field value.
    /// </summary>
    public static void SetStaticFieldValue<T>(IntPtr klass, string fieldName, T value) where T : unmanaged
    {
        IntPtr field = FindField(klass, fieldName);
        if (field == IntPtr.Zero) return;
        Il2CppAPI.il2cpp_field_static_set_value(field, &value);
    }

    #endregion

    #region GC Handle Support

    /// <summary>
    /// Pin an IL2CPP object to prevent GC from moving/collecting it.
    /// </summary>
    /// <param name="obj">Il2CppObject* to pin</param>
    /// <returns>GC handle (pass to UnpinObject to release), or 0 if obj is null</returns>
    public static uint PinObject(IntPtr obj)
    {
        if (obj == IntPtr.Zero) return 0;
        return Il2CppAPI.il2cpp_gchandle_new(obj, true);
    }

    /// <summary>
    /// Free a GC handle previously created by PinObject.
    /// </summary>
    /// <param name="gcHandle">GC handle returned by PinObject</param>
    public static void UnpinObject(uint gcHandle)
    {
        if (gcHandle == 0) return;
        Il2CppAPI.il2cpp_gchandle_free(gcHandle);
    }

    /// <summary>
    /// Get the object pointer from a GC handle.
    /// </summary>
    /// <param name="gcHandle">GC handle</param>
    /// <returns>Il2CppObject* pointer, or IntPtr.Zero if handle is invalid</returns>
    public static IntPtr GetGCHandleTarget(uint gcHandle)
    {
        if (gcHandle == 0) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_gchandle_get_target(gcHandle);
    }

    #endregion

    #region Generic Type Resolution

    /// <summary>
    /// Find an inflated generic class (e.g., List&lt;int&gt;, Dictionary&lt;string, int&gt;).
    /// Uses il2cpp_class_get_type + Il2CppGenericInst to build the inflated type.
    /// </summary>
    /// <param name="openTypeName">Open generic type name (e.g., "System.Collections.Generic.List`1")</param>
    /// <param name="typeArgClasses">Il2CppClass* pointers for each type argument</param>
    /// <returns>Il2CppClass* of the inflated generic type, or IntPtr.Zero on failure</returns>
    public static IntPtr FindGenericClass(string openTypeName, params IntPtr[] typeArgClasses)
    {
        if (string.IsNullOrEmpty(openTypeName) || typeArgClasses == null || typeArgClasses.Length == 0)
            return IntPtr.Zero;

        IntPtr openClass = FindClass(openTypeName);
        if (openClass == IntPtr.Zero)
            return IntPtr.Zero;

        // Get Il2CppType* for each type argument
        IntPtr[] typeArgTypes = new IntPtr[typeArgClasses.Length];
        for (int i = 0; i < typeArgClasses.Length; i++)
        {
            if (typeArgClasses[i] == IntPtr.Zero) return IntPtr.Zero;
            typeArgTypes[i] = Il2CppAPI.il2cpp_class_get_type(typeArgClasses[i]);
            if (typeArgTypes[i] == IntPtr.Zero) return IntPtr.Zero;
        }

        // Use il2cpp_class_get_type on the open class to get its Il2CppType
        IntPtr openType = Il2CppAPI.il2cpp_class_get_type(openClass);
        if (openType == IntPtr.Zero) return IntPtr.Zero;

        // Wrapper-based generic inflation is intentionally not part of the core package.
        var il2cppType = *(Il2CppType*)openType;
        return il2cppType.TypeEnum == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST
            ? openClass
            : IntPtr.Zero;
    }

    #endregion

    #region Method Lookup by Parameter Types

    /// <summary>
    /// Find a method by name and parameter type enums (for overload disambiguation).
    /// </summary>
    /// <param name="klassPtr">Il2CppClass* pointer</param>
    /// <param name="methodName">Method name</param>
    /// <param name="paramTypes">Expected parameter Il2CppTypeEnum values</param>
    /// <returns>MethodInfo* pointer, or IntPtr.Zero if not found</returns>
    public static IntPtr FindMethod(IntPtr klassPtr, string methodName, params Il2CppTypeEnum[] paramTypes)
    {
        if (klassPtr == IntPtr.Zero || string.IsNullOrEmpty(methodName) || paramTypes == null)
            return IntPtr.Zero;

        IntPtr iter = IntPtr.Zero;
        IntPtr method;

        while ((method = Il2CppAPI.il2cpp_class_get_methods(klassPtr, ref iter)) != IntPtr.Zero)
        {
            string name = Marshal.PtrToStringAnsi(Il2CppAPI.il2cpp_method_get_name(method));
            if (name != methodName) continue;

            int paramCount = Il2CppAPI.il2cpp_method_get_param_count(method);
            if (paramCount != paramTypes.Length) continue;

            bool match = true;
            for (int i = 0; i < paramCount; i++)
            {
                IntPtr paramType = Il2CppAPI.il2cpp_method_get_param(method, (uint)i);
                int typeEnum = Il2CppAPI.il2cpp_type_get_type(paramType);
                if ((Il2CppTypeEnum)typeEnum != paramTypes[i])
                {
                    match = false;
                    break;
                }
            }

            if (match) return method;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Find a method by name and parameter class pointers.
    /// Useful for disambiguating overloads where multiple params share the same Il2CppTypeEnum
    /// (e.g., two different delegate types are both IL2CPP_TYPE_CLASS).
    /// </summary>
    public static IntPtr FindMethod(IntPtr klassPtr, string methodName, params IntPtr[] paramClasses)
    {
        if (klassPtr == IntPtr.Zero || string.IsNullOrEmpty(methodName) || paramClasses == null)
            return IntPtr.Zero;

        IntPtr iter = IntPtr.Zero;
        IntPtr method;

        while ((method = Il2CppAPI.il2cpp_class_get_methods(klassPtr, ref iter)) != IntPtr.Zero)
        {
            string name = Marshal.PtrToStringAnsi(Il2CppAPI.il2cpp_method_get_name(method));
            if (name != methodName) continue;

            int paramCount = Il2CppAPI.il2cpp_method_get_param_count(method);
            if (paramCount != paramClasses.Length) continue;

            bool match = true;
            for (int i = 0; i < paramCount; i++)
            {
                IntPtr paramType = Il2CppAPI.il2cpp_method_get_param(method, (uint)i);
                IntPtr paramClass = Il2CppAPI.il2cpp_class_from_type(paramType);
                if (paramClass != paramClasses[i])
                {
                    match = false;
                    break;
                }
            }

            if (match) return method;
        }

        return IntPtr.Zero;
    }

    #endregion

    #region Class Hierarchy Traversal

    /// <summary>
    /// Find a method by walking the class hierarchy (current class + all parents).
    /// </summary>
    /// <param name="klass">Il2CppClass* pointer to start searching from</param>
    /// <param name="methodName">Method name</param>
    /// <param name="paramCount">Expected parameter count, or -1 to match any</param>
    /// <returns>MethodInfo* pointer, or IntPtr.Zero if not found in hierarchy</returns>
    public static IntPtr FindMethodInHierarchy(IntPtr klass, string methodName, int paramCount = -1)
    {
        while (klass != IntPtr.Zero)
        {
            IntPtr method = FindMethod(klass, methodName, paramCount);
            if (method != IntPtr.Zero) return method;
            klass = Il2CppAPI.il2cpp_class_get_parent(klass);
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Find a method by walking the class hierarchy with parameter type matching.
    /// </summary>
    /// <param name="klass">Il2CppClass* pointer to start searching from</param>
    /// <param name="methodName">Method name</param>
    /// <param name="paramTypes">Expected parameter Il2CppTypeEnum values</param>
    /// <returns>MethodInfo* pointer, or IntPtr.Zero if not found in hierarchy</returns>
    public static IntPtr FindMethodInHierarchy(IntPtr klass, string methodName, params Il2CppTypeEnum[] paramTypes)
    {
        while (klass != IntPtr.Zero)
        {
            IntPtr method = FindMethod(klass, methodName, paramTypes);
            if (method != IntPtr.Zero) return method;
            klass = Il2CppAPI.il2cpp_class_get_parent(klass);
        }
        return IntPtr.Zero;
    }

    #endregion

    #region Enum Helpers

    /// <summary>
    /// Get the integer value from a boxed IL2CPP enum object.
    /// </summary>
    /// <param name="enumObj">Boxed Il2CppObject* of enum type</param>
    /// <returns>Underlying int value, or 0 if null</returns>
    public static int GetEnumValue(IntPtr enumObj)
    {
        if (enumObj == IntPtr.Zero) return 0;
        IntPtr valuePtr = Il2CppAPI.il2cpp_object_unbox(enumObj);
        if (valuePtr == IntPtr.Zero) return 0;
        return *(int*)valuePtr;
    }

    /// <summary>
    /// Create a boxed IL2CPP enum from an integer value.
    /// </summary>
    /// <param name="enumClass">Il2CppClass* of the enum type</param>
    /// <param name="value">Integer value to box</param>
    /// <returns>Boxed Il2CppObject* of the enum, or IntPtr.Zero if enumClass is null</returns>
    public static IntPtr CreateEnum(IntPtr enumClass, int value)
    {
        if (enumClass == IntPtr.Zero) return IntPtr.Zero;
        return Il2CppAPI.il2cpp_value_box(enumClass, (IntPtr)(&value));
    }

    #endregion
}
