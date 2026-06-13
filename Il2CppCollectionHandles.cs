using System;

namespace IL2CppSharp;

/// <summary>
/// High-level List&lt;T&gt; wrapper. Prefers IL2CPP property/indexer calls and
/// falls back to safe reference-element struct access when possible.
/// </summary>
public readonly record struct Il2CppListHandle(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public int Count
    {
        get
        {
            if (IsNull) return 0;

            var klass = IL2CPP.Class(Il2CppReflection.GetObjectClass(Pointer));
            var countMethod = klass.Method("get_Count");
            if (countMethod.TryResolve(out _))
                return countMethod.Invoke<int>(Pointer);

            return Il2CppCollectionHelper.GetListCount(Pointer);
        }
    }

    public Il2CppObjectRef GetItem(int index)
    {
        if (IsNull || index < 0) return default;

        var klass = IL2CPP.Class(Il2CppReflection.GetObjectClass(Pointer));
        var getItem = klass.Method("get_Item").Args<int>();
        if (getItem.TryResolve(out _))
            return getItem.Invoke(Pointer, index);

        return Il2CppCollectionHelper.GetListItemPtr(Pointer, index);
    }

    public bool TryGetItem(int index, out Il2CppObjectRef item)
    {
        item = GetItem(index);
        return !item.IsNull;
    }
}

/// <summary>
/// High-level Dictionary&lt;TKey, TValue&gt; wrapper for common indexer access.
/// </summary>
public readonly record struct Il2CppDictionaryHandle(IntPtr Pointer)
{
    public bool IsNull => Pointer == IntPtr.Zero;

    public int Count
    {
        get
        {
            if (IsNull) return 0;

            var klass = IL2CPP.Class(Il2CppReflection.GetObjectClass(Pointer));
            var countMethod = klass.Method("get_Count");
            if (countMethod.TryResolve(out _))
                return countMethod.Invoke<int>(Pointer);

            return Il2CppCollectionHelper.GetDictionaryCount(Pointer);
        }
    }

    public bool SetItem(object key, object value)
    {
        if (IsNull) return false;

        var klass = IL2CPP.Class(Il2CppReflection.GetObjectClass(Pointer));
        var method = key == null || value == null
            ? klass.Method("set_Item").AnyArgs()
            : klass.Method("set_Item").Args(key.GetType(), value.GetType());

        return method.TryInvoke(Pointer, out _, key, value);
    }

    public Il2CppObjectRef GetItem(object key)
    {
        if (IsNull) return default;

        var klass = IL2CPP.Class(Il2CppReflection.GetObjectClass(Pointer));
        var method = key == null
            ? klass.Method("get_Item").AnyArgs()
            : klass.Method("get_Item").Args(key.GetType());

        return method.Invoke(Pointer, key);
    }
}
