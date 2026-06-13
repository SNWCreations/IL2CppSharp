using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace IL2CppSharp;

internal static unsafe class Il2CppArgumentMarshaller
{
    public static IntPtr[] MarshalArguments(IntPtr methodInfo, object[] args, bool throwOnFailure)
    {
        args ??= [];

        int paramCount = methodInfo == IntPtr.Zero
            ? args.Length
            : Il2CppAPI.il2cpp_method_get_param_count(methodInfo);

        if (paramCount != args.Length)
        {
            string message = $"Argument count mismatch: method expects {paramCount}, got {args.Length}.";
            return Fail<IntPtr[]>(message, throwOnFailure);
        }

        if (args.Length == 0) return null;

        var result = new IntPtr[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            IntPtr paramType = methodInfo == IntPtr.Zero
                ? IntPtr.Zero
                : Il2CppAPI.il2cpp_method_get_param(methodInfo, (uint)i);
            result[i] = MarshalArgument(args[i], paramType, throwOnFailure);
        }

        return result;
    }

    private static IntPtr MarshalArgument(object value, IntPtr paramType, bool throwOnFailure)
    {
        if (value == null) return IntPtr.Zero;

        if (value is IntPtr ptr) return ptr;
        if (value is Il2CppObjectRef objRef) return objRef.Pointer;
        if (value is string str) return Il2CppReflection.CreateIl2CppString(str);

        IntPtr paramClass = paramType == IntPtr.Zero
            ? IntPtr.Zero
            : Il2CppAPI.il2cpp_class_from_type(paramType);

        if (value is Array array)
        {
            try
            {
                IntPtr elementClass = paramClass == IntPtr.Zero
                    ? IntPtr.Zero
                    : Il2CppAPI.il2cpp_class_get_element_class(paramClass);
                return Il2CppArrayMarshaller.CreateFromArray(array, elementClass);
            }
            catch (Exception ex)
            {
                return Fail<IntPtr>($"Failed to marshal array argument: {ex.Message}", throwOnFailure);
            }
        }

        Type valueType = value.GetType();
        if (valueType.IsEnum || valueType.IsPrimitive)
        {
            IntPtr boxClass = paramClass != IntPtr.Zero
                ? paramClass
                : Il2CppTypeDescriptor.FromType(valueType).ResolveClassPointer();
            if (boxClass == IntPtr.Zero)
                return Fail<IntPtr>($"Could not resolve IL2CPP class for value type {valueType.FullName}.", throwOnFailure);

            return Il2CppValueMarshaller.Box(boxClass, value);
        }

        return Fail<IntPtr>($"Unsupported IL2CPP argument type: {valueType.FullName}.", throwOnFailure);
    }

    private static T Fail<T>(string message, bool throwOnFailure)
    {
        if (throwOnFailure) throw new ArgumentException(message);
        return default;
    }
}

internal static unsafe class Il2CppArrayMarshaller
{
    public static IntPtr Create<T>(T[] values) where T : unmanaged
    {
        if (values == null) return IntPtr.Zero;

        IntPtr elementClass = Il2CppTypeDescriptor.FromType(typeof(T)).ResolveClassPointer();
        if (elementClass == IntPtr.Zero)
            throw new TypeLoadException($"Could not resolve IL2CPP array element type: {typeof(T).FullName}");

        IntPtr array = Il2CppAPI.CreateArray(elementClass, values.Length);
        if (array == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to allocate IL2CPP array: {typeof(T).FullName}[{values.Length}]");

        if (values.Length == 0) return array;

        IntPtr dataPtr = Il2CppAPI.GetArrayDataPointer(array);
        fixed (T* src = values)
        {
            long byteCount = (long)sizeof(T) * values.Length;
            Buffer.MemoryCopy(src, (void*)dataPtr, byteCount, byteCount);
        }

        return array;
    }

    public static IntPtr CreateStringArray(string[] values)
    {
        if (values == null) return IntPtr.Zero;

        IntPtr stringClass = Il2CppReflection.FindClass("System.String");
        if (stringClass == IntPtr.Zero)
            throw new TypeLoadException("Could not resolve IL2CPP System.String class.");

        IntPtr array = Il2CppAPI.CreateArray(stringClass, values.Length);
        if (array == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to allocate IL2CPP string array: {values.Length}");

        IntPtr dataPtr = Il2CppAPI.GetArrayDataPointer(array);
        for (int i = 0; i < values.Length; i++)
        {
            IntPtr item = Il2CppReflection.CreateIl2CppString(values[i]);
            *(IntPtr*)(dataPtr + i * IntPtr.Size) = item;
        }

        return array;
    }

    public static IntPtr CreateFromArray(Array values, IntPtr preferredElementClass)
    {
        if (values == null) return IntPtr.Zero;
        if (values.Rank != 1)
            throw new NotSupportedException("Only one-dimensional IL2CPP arrays are supported.");

        Type elementType = values.GetType().GetElementType();
        if (elementType == typeof(string))
            return CreateStringArray((string[])values);

        if (preferredElementClass != IntPtr.Zero)
            return CreateWithElementClass(values, elementType, preferredElementClass);

        MethodInfo createMethod = typeof(Il2CppArrayMarshaller)
            .GetMethod(nameof(Create), BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(elementType);

        if (createMethod == null)
            throw new NotSupportedException($"Unsupported IL2CPP array element type: {elementType.FullName}");

        try
        {
            return (IntPtr)createMethod.Invoke(null, new object[] { values });
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static IntPtr CreateWithElementClass(Array values, Type elementType, IntPtr elementClass)
    {
        IntPtr array = Il2CppAPI.CreateArray(elementClass, values.Length);
        if (array == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to allocate IL2CPP array: {elementType.FullName}[{values.Length}]");

        if (values.Length == 0) return array;

        MethodInfo copyMethod = typeof(Il2CppArrayMarshaller)
            .GetMethod(nameof(CopyArrayData), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(elementType);

        if (copyMethod == null)
            throw new NotSupportedException($"Unsupported IL2CPP array element type: {elementType.FullName}");

        try
        {
            copyMethod.Invoke(null, new object[] { values, Il2CppAPI.GetArrayDataPointer(array) });
            return array;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static void CopyArrayData<T>(T[] values, IntPtr destination) where T : unmanaged
    {
        fixed (T* src = values)
        {
            long byteCount = (long)sizeof(T) * values.Length;
            Buffer.MemoryCopy(src, (void*)destination, byteCount, byteCount);
        }
    }
}

internal static unsafe class Il2CppValueMarshaller
{
    public static IntPtr Box(IntPtr klass, object value)
    {
        if (klass == IntPtr.Zero || value == null) return IntPtr.Zero;

        Type type = value.GetType();
        if (type.IsEnum)
            return BoxEnum(klass, value, Enum.GetUnderlyingType(type));

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
            {
                byte v = (bool)value ? (byte)1 : (byte)0;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Char:
            {
                char v = (char)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.SByte:
            {
                sbyte v = (sbyte)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Byte:
            {
                byte v = (byte)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Int16:
            {
                short v = (short)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.UInt16:
            {
                ushort v = (ushort)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Int32:
            {
                int v = (int)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.UInt32:
            {
                uint v = (uint)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Int64:
            {
                long v = (long)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.UInt64:
            {
                ulong v = (ulong)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Single:
            {
                float v = (float)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            case TypeCode.Double:
            {
                double v = (double)value;
                return Il2CppAPI.il2cpp_value_box(klass, (IntPtr)(&v));
            }
            default:
                throw new NotSupportedException($"Unsupported value type for IL2CPP boxing: {type.FullName}");
        }
    }

    private static IntPtr BoxEnum(IntPtr klass, object value, Type underlyingType)
    {
        object converted = Convert.ChangeType(value, underlyingType);
        return Box(klass, converted);
    }
}
