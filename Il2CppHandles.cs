using System;

namespace IL2CppSharp;

/// <summary>
/// High-level class handle used by the fluent IL2CPP API.
/// </summary>
public sealed class Il2CppClassHandle
{
    private readonly bool _required;
    private readonly string _fullTypeName;
    private IntPtr _pointer;
    private bool _resolved;

    internal Il2CppClassHandle(string fullTypeName, bool required)
    {
        _fullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
        _required = required;
    }

    internal Il2CppClassHandle(IntPtr pointer, bool required)
    {
        _pointer = pointer;
        _resolved = true;
        _required = required;
    }

    public string FullTypeName => _fullTypeName;

    public IntPtr Pointer
    {
        get
        {
            if (_required) return ResolveOrThrow();
            TryResolve(out var pointer);
            return pointer;
        }
    }

    public bool IsNull => Pointer == IntPtr.Zero;

    public Il2CppClassHandle Require()
    {
        var handle = _pointer != IntPtr.Zero || string.IsNullOrEmpty(_fullTypeName)
            ? new Il2CppClassHandle(_pointer, required: true)
            : new Il2CppClassHandle(_fullTypeName, required: true);
        handle.ResolveOrThrow();
        return handle;
    }

    public bool TryResolve(out IntPtr pointer)
    {
        if (!_resolved)
        {
            _pointer = Il2CppReflection.FindClass(_fullTypeName);
            _resolved = true;
        }

        pointer = _pointer;
        return pointer != IntPtr.Zero;
    }

    public IntPtr ResolveOrThrow()
    {
        if (TryResolve(out var pointer)) return pointer;
        throw new TypeLoadException($"Required IL2CPP class not found: {_fullTypeName}");
    }

    public Il2CppMethodHandle Method(string methodName)
        => new(this, methodName, required: _required);

    public Il2CppMethodHandle RequireMethod(string methodName)
        => new Il2CppMethodHandle(this.Require(), methodName, required: true).Require();

    public Il2CppFieldRef Field(string fieldName)
        => TryResolve(out var klass) ? Il2CppReflection.FindField(klass, fieldName) : default;

    public Il2CppPropertyRef Property(string propertyName)
        => TryResolve(out var klass) ? Il2CppReflection.FindProperty(klass, propertyName) : default;

    public Il2CppObjectRef StaticField(string fieldName)
        => TryResolve(out var klass) ? Il2CppReflection.GetStaticFieldValue(klass, fieldName) : default;

    public T StaticField<T>(string fieldName) where T : unmanaged
        => TryResolve(out var klass) ? Il2CppReflection.GetStaticFieldValue<T>(klass, fieldName) : default;

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(_fullTypeName)) return $"Il2CppClassHandle({_fullTypeName})";
        return $"Il2CppClassHandle(0x{_pointer:X})";
    }
}

/// <summary>
/// High-level method handle used by the fluent IL2CPP API.
/// </summary>
public sealed class Il2CppMethodHandle
{
    private readonly Il2CppClassHandle _owner;
    private readonly string _methodName;
    private readonly bool _required;
    private readonly bool _anyParameterList;
    private readonly Il2CppTypeDescriptor[] _parameters;
    private IntPtr _pointer;
    private bool _resolved;

    internal Il2CppMethodHandle(Il2CppClassHandle owner, string methodName, bool required)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        _required = required;
        _parameters = [];
    }

    internal Il2CppMethodHandle(IntPtr methodInfo, bool required)
    {
        _pointer = methodInfo;
        _required = required;
        _resolved = true;
        _parameters = [];
    }

    private Il2CppMethodHandle(
        Il2CppClassHandle owner,
        string methodName,
        Il2CppTypeDescriptor[] parameters,
        bool anyParameterList,
        bool required)
    {
        _owner = owner;
        _methodName = methodName;
        _parameters = parameters ?? [];
        _anyParameterList = anyParameterList;
        _required = required;
    }

    public IntPtr Pointer
    {
        get
        {
            if (_required) return ResolveOrThrow();
            TryResolve(out var method);
            return method;
        }
    }

    public bool IsNull => Pointer == IntPtr.Zero;

    public Il2CppMethodHandle Args(params Type[] parameterTypes)
        => new(_owner, _methodName, Il2CppTypeDescriptor.FromTypes(parameterTypes), anyParameterList: false, required: _required);

    public Il2CppMethodHandle Args<T1>()
        => Args(typeof(T1));

    public Il2CppMethodHandle Args<T1, T2>()
        => Args(typeof(T1), typeof(T2));

    public Il2CppMethodHandle Args<T1, T2, T3>()
        => Args(typeof(T1), typeof(T2), typeof(T3));

    public Il2CppMethodHandle Args<T1, T2, T3, T4>()
        => Args(typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    public Il2CppMethodHandle AnyArgs()
        => new(_owner, _methodName, [], anyParameterList: true, required: _required);

    public Il2CppMethodHandle Require()
    {
        if (_owner == null)
        {
            var rawHandle = new Il2CppMethodHandle(_pointer, required: true);
            rawHandle.ResolveOrThrow();
            return rawHandle;
        }

        var handle = _pointer != IntPtr.Zero
            ? new Il2CppMethodHandle(_pointer, required: true)
            : new Il2CppMethodHandle(_owner.Require(), _methodName, _parameters, _anyParameterList, required: true);
        handle.ResolveOrThrow();
        return handle;
    }

    public bool TryResolve(out IntPtr method)
    {
        if (!_resolved)
        {
            if (_owner == null || !_owner.TryResolve(out var klass))
            {
                _pointer = IntPtr.Zero;
            }
            else
            {
                _pointer = Il2CppMethodResolver.FindMethod(klass, _methodName, _parameters, _anyParameterList);
            }
            _resolved = true;
        }

        method = _pointer;
        return method != IntPtr.Zero;
    }

    public IntPtr ResolveOrThrow()
    {
        if (TryResolve(out var method)) return method;
        throw new MissingMethodException($"Required IL2CPP method not found: {Describe()}");
    }

    public Il2CppObjectRef Invoke(IntPtr instance, params object[] args)
    {
        if (!TryInvoke(instance, out var result, args) && _required)
            throw new InvalidOperationException($"IL2CPP invocation failed: {Describe()}");
        return result;
    }

    public Il2CppObjectRef InvokeStatic(params object[] args)
    {
        if (!TryInvokeStatic(out var result, args) && _required)
            throw new InvalidOperationException($"IL2CPP static invocation failed: {Describe()}");
        return result;
    }

    public T Invoke<T>(IntPtr instance, params object[] args) where T : unmanaged
    {
        var result = Invoke(instance, args);
        return Il2CppResultMarshaller.ToUnmanaged<T>(result.Pointer);
    }

    public T InvokeStatic<T>(params object[] args) where T : unmanaged
    {
        var result = InvokeStatic(args);
        return Il2CppResultMarshaller.ToUnmanaged<T>(result.Pointer);
    }

    public string InvokeString(IntPtr instance, params object[] args)
    {
        var result = Invoke(instance, args);
        return result.IsNull ? null : Il2CppAPI.Il2CppStringToManaged(result.Pointer);
    }

    public string InvokeStaticString(params object[] args)
    {
        var result = InvokeStatic(args);
        return result.IsNull ? null : Il2CppAPI.Il2CppStringToManaged(result.Pointer);
    }

    public bool TryInvoke(IntPtr instance, out Il2CppObjectRef result, params object[] args)
    {
        result = default;
        bool throwOnFailure = _required;

        IntPtr method = throwOnFailure ? ResolveOrThrow() : Pointer;
        if (method == IntPtr.Zero) return false;

        IntPtr[] marshalled = Il2CppArgumentMarshaller.MarshalArguments(method, args, throwOnFailure);
        if (args != null && args.Length > 0 && marshalled == null) return false;

        result = Il2CppReflection.InvokeMethod(method, instance, marshalled);
        return true;
    }

    public bool TryInvokeStatic(out Il2CppObjectRef result, params object[] args)
    {
        result = default;
        bool throwOnFailure = _required;

        IntPtr method = throwOnFailure ? ResolveOrThrow() : Pointer;
        if (method == IntPtr.Zero) return false;

        IntPtr[] marshalled = Il2CppArgumentMarshaller.MarshalArguments(method, args, throwOnFailure);
        if (args != null && args.Length > 0 && marshalled == null) return false;

        result = Il2CppReflection.InvokeStaticMethod(method, marshalled);
        return true;
    }

    private string Describe()
        => _owner == null ? $"0x{_pointer:X}" : $"{_owner}.{_methodName}";

    public override string ToString()
        => _pointer != IntPtr.Zero ? $"Il2CppMethodHandle(0x{_pointer:X})" : $"Il2CppMethodHandle({Describe()})";
}

internal static class Il2CppResultMarshaller
{
    public static T ToUnmanaged<T>(IntPtr result) where T : unmanaged
    {
        if (typeof(T) == typeof(IntPtr))
        {
            object ptr = result;
            return (T)ptr;
        }

        return result == IntPtr.Zero ? default : Il2CppReflection.UnboxValue<T>(result);
    }
}
