using System;
using System.Runtime.InteropServices;

namespace IL2CppSharp;

/// <summary>
/// Low-level P/Invoke declarations and thin wrappers for the IL2CPP C API (GameAssembly.dll).
/// All il2cpp_* native imports are centralized here.
/// </summary>
public static unsafe class Il2CppAPI
{
    #region Domain & Assembly

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_domain_get();

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_domain_get_assemblies(IntPtr domain, ref uint size);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_assembly_get_image(IntPtr assembly);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_image_get_name(IntPtr image);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_image_get_class(IntPtr image, uint index);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_image_get_class_count(IntPtr image);

    #endregion

    #region Class

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_from_name(IntPtr image,
        [MarshalAs(UnmanagedType.LPStr)] string namespaze,
        [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_name(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_namespace(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_parent(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_from_type(IntPtr type);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_class_is_valuetype(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_class_value_size(IntPtr klass, ref uint align);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_element_class(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_class_is_enum(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_class_is_generic(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_class_is_inflated(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_class_instance_size(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_interfaces(IntPtr klass, ref IntPtr iter);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_class_get_type_token(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_method_from_name(IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name, int argsCount);

    #endregion

    #region Object

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_object_new(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_object_get_class(IntPtr obj);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_object_unbox(IntPtr obj);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_value_box(IntPtr klass, IntPtr data);

    #endregion

    #region Method

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_methods(IntPtr klass, ref IntPtr iter);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr il2cpp_method_get_name(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    public static extern int il2cpp_method_get_param_count(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_method_get_param(IntPtr method, uint index);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_method_get_return_type(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_method_get_flags(IntPtr method, ref uint iflags);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_runtime_invoke(IntPtr method, IntPtr obj, void** args, ref IntPtr exc);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_method_is_generic(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_method_is_inflated(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_method_is_instance(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_method_get_token(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_method_get_class(IntPtr method);

    #endregion

    #region Field

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_field_from_name(IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_fields(IntPtr klass, ref IntPtr iter);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_field_get_name(IntPtr field);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_field_get_value(IntPtr obj, IntPtr field, void* value);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_field_set_value(IntPtr obj, IntPtr field, void* value);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_field_get_offset(IntPtr field);

    /// <summary>
    /// Get the byte offset of a field within an IL2CPP object.
    /// </summary>
    public static int GetFieldOffset(IntPtr field)
    {
        if (field == IntPtr.Zero) return -1;
        return il2cpp_field_get_offset(field);
    }

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_field_get_type(IntPtr field);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_field_static_get_value(IntPtr field, void* value);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_field_static_set_value(IntPtr field, void* value);

    #endregion

    #region Property

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_properties(IntPtr klass, ref IntPtr iter);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_property_from_name(IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_property_get_get_method(IntPtr prop);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_property_get_set_method(IntPtr prop);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_property_get_name(IntPtr prop);

    #endregion

    #region String

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_string_new([MarshalAs(UnmanagedType.LPStr)] string str);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    internal static extern IntPtr il2cpp_string_new_utf16([MarshalAs(UnmanagedType.LPWStr)] string str, int length);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_string_chars(IntPtr str);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_string_length(IntPtr str);

    #endregion

    #region Array

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_array_length(IntPtr array);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_array_new(IntPtr elementTypeKlass, ulong length);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_array_element_size(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_array_object_header_size();

    #endregion

    #region Type

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int il2cpp_type_get_type(IntPtr type);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_class_get_type(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_type_get_object(IntPtr type);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_type_get_name(IntPtr type);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool il2cpp_type_is_byref(IntPtr type);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_type_get_attrs(IntPtr type);

    #endregion

    #region Runtime

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_runtime_class_init(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_runtime_object_init(IntPtr obj);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_add_internal_call([MarshalAs(UnmanagedType.LPStr)] string name, IntPtr method);

    #endregion

    #region GC

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint il2cpp_gchandle_new(IntPtr obj, bool pinned);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void il2cpp_gchandle_free(uint gchandle);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr il2cpp_gchandle_get_target(uint gchandle);

    #endregion

    #region Public Helpers

    /// <summary>
    /// Allocate a new IL2CPP object of the given class.
    /// </summary>
    public static IntPtr CreateObject(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return IntPtr.Zero;
        return il2cpp_object_new(klass);
    }

    /// <summary>
    /// Allocate a new IL2CPP array of the given element type and length.
    /// </summary>
    public static IntPtr CreateArray(IntPtr elementTypeKlass, int length)
    {
        if (elementTypeKlass == IntPtr.Zero) return IntPtr.Zero;
        return il2cpp_array_new(elementTypeKlass, (ulong)length);
    }

    /// <summary>
    /// Get the Il2CppType* for a class pointer.
    /// </summary>
    public static IntPtr GetClassType(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return IntPtr.Zero;
        return il2cpp_class_get_type(klass);
    }

    /// <summary>
    /// Get the boxed System.Type object for an Il2CppType* pointer.
    /// </summary>
    public static IntPtr GetTypeObject(IntPtr type)
    {
        if (type == IntPtr.Zero) return IntPtr.Zero;
        return il2cpp_type_get_object(type);
    }

    /// <summary>
    /// Get the pointer to the first element of an IL2CPP array.
    /// </summary>
    public static IntPtr GetArrayDataPointer(IntPtr array)
    {
        if (array == IntPtr.Zero) return IntPtr.Zero;
        return array + il2cpp_array_object_header_size();
    }

    /// <summary>
    /// Get the length of an IL2CPP array.
    /// </summary>
    public static uint GetArrayLength(IntPtr array)
    {
        if (array == IntPtr.Zero) return 0;
        return il2cpp_array_length(array);
    }

    /// <summary>
    /// Get the parent class of an IL2CPP class.
    /// </summary>
    public static IntPtr GetParentClass(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return IntPtr.Zero;
        return il2cpp_class_get_parent(klass);
    }

    /// <summary>
    /// Get the name of an IL2CPP class.
    /// </summary>
    public static string GetClassName(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return null;
        IntPtr namePtr = il2cpp_class_get_name(klass);
        if (namePtr == IntPtr.Zero) return null;
        return Marshal.PtrToStringAnsi(namePtr);
    }

    /// <summary>
    /// Get the namespace of an IL2CPP class.
    /// </summary>
    public static string GetClassNamespace(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return null;
        IntPtr nsPtr = il2cpp_class_get_namespace(klass);
        if (nsPtr == IntPtr.Zero) return null;
        return Marshal.PtrToStringAnsi(nsPtr);
    }

    /// <summary>
    /// Convert an IL2CPP string pointer to a managed string using raw char pointer access.
    /// </summary>
    public static string Il2CppStringToManaged(IntPtr il2cppString)
    {
        if (il2cppString == IntPtr.Zero) return null;
        int length = il2cpp_string_length(il2cppString);
        if (length == 0) return string.Empty;
        IntPtr chars = il2cpp_string_chars(il2cppString);
        if (chars == IntPtr.Zero) return null;
        return Marshal.PtrToStringUni(chars, length);
    }

    /// <summary>
    /// Set a pointer-sized field value on an IL2CPP object.
    /// Uses direct memory write via field offset for reliability.
    /// </summary>
    public static void SetFieldValue(IntPtr obj, IntPtr field, IntPtr value)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return;
        int offset = il2cpp_field_get_offset(field);
        *(IntPtr*)((byte*)obj + offset) = value;
    }

    /// <summary>
    /// Read an int field from an IL2CPP object.
    /// </summary>
    public static int ReadFieldInt(IntPtr obj, IntPtr field)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return 0;
        int value = 0;
        il2cpp_field_get_value(obj, field, &value);
        return value;
    }

    /// <summary>
    /// Write an int field on an IL2CPP object.
    /// Uses direct memory write via field offset for reliability.
    /// </summary>
    public static void WriteFieldInt(IntPtr obj, IntPtr field, int value)
    {
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return;
        int offset = il2cpp_field_get_offset(field);
        *(int*)((byte*)obj + offset) = value;
    }

    /// <summary>
    /// Read an unmanaged struct field from an IL2CPP object.
    /// </summary>
    public static T ReadFieldStruct<T>(IntPtr obj, IntPtr field) where T : unmanaged
    {
        T value = default;
        if (obj == IntPtr.Zero || field == IntPtr.Zero) return value;
        il2cpp_field_get_value(obj, field, &value);
        return value;
    }

    /// <summary>
    /// Read the method pointer from a MethodInfo*.
    /// </summary>
    public static IntPtr GetMethodPointer(IntPtr methodInfoPtr)
    {
        if (methodInfoPtr == IntPtr.Zero) return IntPtr.Zero;
        return *(IntPtr*)methodInfoPtr;
    }

    /// <summary>
    /// Resolve an internal call by name, returning the native function pointer.
    /// </summary>
    public static IntPtr ResolveICall(string name) => il2cpp_resolve_icall(name);

    /// <summary>
    /// Register or overwrite an internal call entry in the icall map.
    /// </summary>
    public static void AddInternalCall(string name, IntPtr method) => il2cpp_add_internal_call(name, method);

    public static int GetClassInstanceSize(IntPtr klass)
    {
        if (klass == IntPtr.Zero) return 0;
        return il2cpp_class_instance_size(klass);
    }

    public static uint GCHandleNew(IntPtr obj, bool pinned) => il2cpp_gchandle_new(obj, pinned);

    public static void GCHandleFree(uint handle) => il2cpp_gchandle_free(handle);

    #endregion
}
