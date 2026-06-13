using System;
using System.Runtime.InteropServices;

namespace IL2CppSharp;

// Collection struct layouts matching .NET BCL as compiled by IL2CPP (x64).
// Verified against workspace/Il2CppApiCs/Il2CppGetter/Il2Cpp/Generics/

/// <summary>
/// IL2CPP List&lt;T&gt; internal layout.
/// Size: 0x28 (40 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct Il2CppList
{
    [FieldOffset(0x00)] public Il2CppObject obj;
    [FieldOffset(0x10)] public IntPtr items;      // Il2CppArray* (_items backing array)
    [FieldOffset(0x18)] public int size;           // _size (actual element count)
    [FieldOffset(0x1C)] public int version;        // _version
    [FieldOffset(0x20)] public IntPtr syncRoot;    // _syncRoot
}

/// <summary>
/// IL2CPP Dictionary&lt;TKey, TValue&gt; internal layout.
/// Size: 0x50 (80 bytes)
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public struct Il2CppDictionary
{
    [FieldOffset(0x00)] public Il2CppObject obj;
    [FieldOffset(0x10)] public IntPtr buckets;     // Il2CppArray* (_buckets)
    [FieldOffset(0x18)] public IntPtr entries;     // Il2CppArray* (_entries)
    [FieldOffset(0x20)] public int count;          // _count
    [FieldOffset(0x24)] public int freeList;       // _freeList
    [FieldOffset(0x28)] public int freeCount;      // _freeCount
    [FieldOffset(0x2C)] public int version;        // _version
    [FieldOffset(0x30)] public IntPtr comparer;    // IEqualityComparer<TKey>
    [FieldOffset(0x38)] public IntPtr keys;        // KeyCollection
    [FieldOffset(0x40)] public IntPtr values;      // ValueCollection
    [FieldOffset(0x48)] public IntPtr syncRoot;    // _syncRoot
}

/// <summary>
/// Helpers for reading IL2CPP collections via struct access.
/// </summary>
public static unsafe class Il2CppCollectionHelper
{
    /// <summary>
    /// Get the count of a List&lt;T&gt; by reading the struct directly.
    /// </summary>
    public static int GetListCount(IntPtr listPtr)
    {
        if (listPtr == IntPtr.Zero) return 0;
        return ((Il2CppList*)listPtr)->size;
    }

    /// <summary>
    /// Get an element pointer from a List&lt;T&gt; by index.
    /// Returns the element object pointer for reference-type lists.
    /// Value-type List&lt;T&gt; elements are intentionally not exposed here because their
    /// element size and layout must be handled by typed readers.
    /// </summary>
    public static IntPtr GetListItemPtr(IntPtr listPtr, int index)
    {
        if (listPtr == IntPtr.Zero) return IntPtr.Zero;
        var list = (Il2CppList*)listPtr;
        if ((uint)index >= (uint)list->size) return IntPtr.Zero;
        IntPtr items = list->items;
        if (items == IntPtr.Zero) return IntPtr.Zero;

        IntPtr arrayClass = Il2CppAPI.il2cpp_object_get_class(items);
        IntPtr elementClass = arrayClass == IntPtr.Zero
            ? IntPtr.Zero
            : Il2CppAPI.il2cpp_class_get_element_class(arrayClass);
        if (elementClass == IntPtr.Zero || Il2CppAPI.il2cpp_class_is_valuetype(elementClass))
            return IntPtr.Zero;

        int headerSize = Il2CppAPI.il2cpp_array_object_header_size();
        return *(IntPtr*)(items + headerSize + index * sizeof(IntPtr));
    }

    /// <summary>
    /// Get the count of a Dictionary&lt;TKey, TValue&gt; by reading the struct directly.
    /// Note: actual entry count = count - freeCount.
    /// </summary>
    public static int GetDictionaryCount(IntPtr dictPtr)
    {
        if (dictPtr == IntPtr.Zero) return 0;
        var dict = (Il2CppDictionary*)dictPtr;
        return dict->count - dict->freeCount;
    }
}
