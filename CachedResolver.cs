using System;

namespace IL2CppSharp;

/// <summary>
/// Lazy-resolve-once class resolver. Replaces hand-rolled static bool + IntPtr caching patterns.
/// Thread-safe: resolution happens at most once.
/// </summary>
public sealed class CachedClass(string fullTypeName)
{
    private readonly string _typeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
    private Il2CppClassRef _cached;
    private volatile bool _resolved;


    /// <summary>The resolved class ref. Resolves lazily on first access.</summary>
    public Il2CppClassRef Value
    {
        get
        {
            if (!_resolved)
            {
                _cached = Il2CppReflection.FindClass(_typeName);
                _resolved = true;
            }
            return _cached;
        }
    }

    /// <summary>Whether resolution has been attempted.</summary>
    public bool IsResolved => _resolved;

    /// <summary>Reset the cache, forcing re-resolution on next access.</summary>
    public void Invalidate()
    {
        _resolved = false;
        _cached = default;
    }
}

/// <summary>
/// Lazy-resolve-once method resolver. Depends on a CachedClass for the owning type.
/// </summary>
/// <param name="owner">Owning class resolver</param>
/// <param name="methodName">Method name to find</param>
/// <param name="paramCount">Expected parameter count, or -1 for any</param>
/// <param name="useParent">If true, search on the parent class instead</param>
public sealed class CachedMethod(CachedClass owner, string methodName, int paramCount = -1, bool useParent = false)
{
    private readonly CachedClass _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    private readonly string _methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    private readonly int _paramCount = paramCount;
    private readonly bool _useParent = useParent;
    private Il2CppMethodRef _cached;
    private volatile bool _resolved;


    /// <summary>The resolved method ref. Resolves lazily on first access.</summary>
    public Il2CppMethodRef Value
    {
        get
        {
            if (!_resolved)
            {
                var klass = _useParent ? _owner.Value.Parent : _owner.Value;
                _cached = klass.FindMethod(_methodName, _paramCount);
                _resolved = true;
            }
            return _cached;
        }
    }

    public bool IsResolved => _resolved;

    public void Invalidate()
    {
        _resolved = false;
        _cached = default;
    }
}

/// <summary>
/// Lazy-resolve-once field resolver.
/// </summary>
public sealed class CachedField(CachedClass owner, string fieldName)
{
    private readonly CachedClass _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    private readonly string _fieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
    private Il2CppFieldRef _cached;
    private volatile bool _resolved;


    /// <summary>The resolved field ref. Resolves lazily on first access.</summary>
    public Il2CppFieldRef Value
    {
        get
        {
            if (!_resolved)
            {
                _cached = _owner.Value.FindField(_fieldName);
                _resolved = true;
            }
            return _cached;
        }
    }

    public bool IsResolved => _resolved;

    public void Invalidate()
    {
        _resolved = false;
        _cached = default;
    }
}

/// <summary>
/// Lazy-resolve-once property resolver.
/// </summary>
public sealed class CachedProperty(CachedClass owner, string propertyName)
{
    private readonly CachedClass _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    private readonly string _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    private Il2CppPropertyRef _cached;
    private volatile bool _resolved;


    /// <summary>The resolved property ref. Resolves lazily on first access.</summary>
    public Il2CppPropertyRef Value
    {
        get
        {
            if (!_resolved)
            {
                _cached = _owner.Value.FindProperty(_propertyName);
                _resolved = true;
            }
            return _cached;
        }
    }

    public bool IsResolved => _resolved;

    public void Invalidate()
    {
        _resolved = false;
        _cached = default;
    }
}
