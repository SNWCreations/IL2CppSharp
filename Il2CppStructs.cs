using System;
using System.Runtime.InteropServices;

namespace IL2CppSharp;

// All struct layouts verified against Unity 2021.3 IL2CPP source (x64).
// See: workspace/il2cpp_unity/libil2cpp/il2cpp-object-internals.h,
//      il2cpp-class-internals.h, il2cpp-runtime-metadata.h

/// <summary>
/// IL2CPP object header. Base of all IL2CPP managed objects.
/// Size: 0x10 (16 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct Il2CppObject
{
    [FieldOffset(0x00)] public IntPtr klass;    // Il2CppClass* (also vtable)
    [FieldOffset(0x08)] public IntPtr monitor;  // MonitorData*

    /// <summary>Offset from object pointer to the first user data field.</summary>
    public const int DataOffset = 0x10;
}

/// <summary>
/// IL2CPP string header. Chars follow inline at offset 0x14 (UTF-16).
/// Fixed header size: 0x14 (20 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct Il2CppString
{
    [FieldOffset(0x00)] public Il2CppObject obj;
    [FieldOffset(0x10)] public int length;

    /// <summary>Offset from string pointer to the first char (UTF-16).</summary>
    public const int CharsOffset = 0x14;
}

/// <summary>
/// IL2CPP array bounds for multi-dimensional arrays.
/// Size: 0x10 (16 bytes, includes padding)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct Il2CppArrayBounds
{
    [FieldOffset(0x00)] public ulong length;       // il2cpp_array_size_t
    [FieldOffset(0x08)] public int lower_bound;    // il2cpp_array_lower_bound_t
}

/// <summary>
/// IL2CPP array header. Element data follows at offset 0x20.
/// Size: 0x20 (32 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct Il2CppArray
{
    [FieldOffset(0x00)] public Il2CppObject obj;
    [FieldOffset(0x10)] public IntPtr bounds;       // Il2CppArrayBounds* (NULL for szarrays)
    [FieldOffset(0x18)] public ulong max_length;    // il2cpp_array_size_t

    /// <summary>Offset from array pointer to the first element.</summary>
    public const int DataOffset = 0x20;
}

/// <summary>
/// IL2CPP MethodInfo struct (standard, without HybridCLR extension).
/// Size: 0x58 (88 bytes, padded from 0x54)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x58)]
public struct Il2CppMethodInfo
{
    [FieldOffset(0x00)] public IntPtr methodPointer;          // Il2CppMethodPointer
    [FieldOffset(0x08)] public IntPtr virtualMethodPointer;   // Il2CppMethodPointer
    [FieldOffset(0x10)] public IntPtr invoker_method;         // InvokerMethod
    [FieldOffset(0x18)] public IntPtr name;                   // const char*
    [FieldOffset(0x20)] public IntPtr klass;                  // Il2CppClass*
    [FieldOffset(0x28)] public IntPtr return_type;            // const Il2CppType*
    [FieldOffset(0x30)] public IntPtr parameters;             // const Il2CppType**
    [FieldOffset(0x38)] public IntPtr methodMetadataHandle;   // union: rgctx_data / methodMetadataHandle
    [FieldOffset(0x40)] public IntPtr genericContainerHandle; // union: genericMethod / genericContainerHandle
    [FieldOffset(0x48)] public uint token;
    [FieldOffset(0x4C)] public ushort flags;
    [FieldOffset(0x4E)] public ushort iflags;
    [FieldOffset(0x50)] public ushort slot;
    [FieldOffset(0x52)] public byte parameters_count;
    [FieldOffset(0x53)] public byte bitfield;
    // bitfield: is_generic:1, is_inflated:1, wrapper_type:1,
    //           has_full_generic_sharing_signature:1, indirect_call_via_invokers:1
}

/// <summary>
/// Offset constants for MethodInfo, usable for raw pointer arithmetic.
/// </summary>
public static class MethodInfoOffsets
{
    public const int MethodPointer = 0x00;
    public const int VirtualMethodPointer = 0x08;
    public const int InvokerMethod = 0x10;
    public const int Name = 0x18;
    public const int Klass = 0x20;
    public const int ReturnType = 0x28;
    public const int Parameters = 0x30;
    public const int Token = 0x48;
    public const int Flags = 0x4C;
    public const int Slot = 0x50;
    public const int ParametersCount = 0x52;
    public const int Bitfield = 0x53;
    public const int StandardSize = 0x58;

    // HybridCLR extends MethodInfo with additional fields after 0x54
    public const int InterpData = 0x58;
    public const int HybridCLRSize = 0x80;
}

/// <summary>
/// IL2CPP type descriptor. Contains a data union (8 bytes) + bitfield (4 bytes).
/// Size: 0x10 (16 bytes, padded from 0x0C)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct Il2CppType
{
    [FieldOffset(0x00)] public IntPtr data;       // union: klassIndex, typeHandle, type, array, generic_class, etc.
    [FieldOffset(0x08)] public uint bitfield;     // attrs:16, type:8, num_mods:5, byref:1, pinned:1, valuetype:1

    public ushort Attrs => (ushort)(bitfield & 0xFFFF);
    public Il2CppTypeEnum TypeEnum => (Il2CppTypeEnum)((bitfield >> 16) & 0xFF);
    public byte NumMods => (byte)((bitfield >> 24) & 0x1F);
    public bool ByRef => ((bitfield >> 29) & 1) != 0;
    public bool Pinned => ((bitfield >> 30) & 1) != 0;
    public bool ValueType => ((bitfield >> 31) & 1) != 0;
}

/// <summary>
/// IL2CPP field info.
/// Size: 0x20 (32 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct Il2CppFieldInfo
{
    [FieldOffset(0x00)] public IntPtr name;     // const char*
    [FieldOffset(0x08)] public IntPtr type;     // const Il2CppType*
    [FieldOffset(0x10)] public IntPtr parent;   // Il2CppClass*
    [FieldOffset(0x18)] public int offset;      // -1 = thread static
    [FieldOffset(0x1C)] public uint token;
}

/// <summary>
/// IL2CPP generic context (class + method instantiation).
/// Size: 0x10 (16 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct Il2CppGenericContext
{
    [FieldOffset(0x00)] public IntPtr class_inst;   // const Il2CppGenericInst*
    [FieldOffset(0x08)] public IntPtr method_inst;  // const Il2CppGenericInst*
}

/// <summary>
/// IL2CPP generic instantiation (type argument list).
/// Size: 0x10 (16 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct Il2CppGenericInst
{
    [FieldOffset(0x00)] public uint type_argc;
    // 4 bytes padding
    [FieldOffset(0x08)] public IntPtr type_argv;    // const Il2CppType**
}

/// <summary>
/// IL2CPP generic class (inflated generic type).
/// Size: 0x20 (32 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct Il2CppGenericClass
{
    [FieldOffset(0x00)] public IntPtr type;                 // const Il2CppType*
    [FieldOffset(0x08)] public Il2CppGenericContext context; // inline struct
    [FieldOffset(0x18)] public IntPtr cached_class;         // Il2CppClass*
}
