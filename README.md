# IL2CppSharp

English | [简体中文](README.zh-CN.md)

[![CI](https://github.com/SNWCreations/IL2CppSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/SNWCreations/IL2CppSharp/actions/workflows/ci.yml)

A lightweight C# library for interacting with the IL2CPP runtime in Unity games. It provides a fluent high-level API for daily plugin code and keeps the direct `Il2CppReflection` / `Il2CppAPI` layer available for low-level runtime work.

## Features

- **Fluent High-Level API** — `IL2CPP.Class("Namespace.Type").Method("Name").Args<T1, T2>().Invoke(...)` handles lookup, overload signatures, argument marshalling, arrays, boxing, and result conversion
- **P/Invoke API** — 60+ centralized `il2cpp_*` native imports covering classes, methods, fields, properties, arrays, types, strings, GC, and runtime initialization
- **Typed Native Structs** — `Il2CppObject`, `Il2CppArray`, `Il2CppMethodInfo`, `Il2CppType`, `Il2CppFieldInfo`, `Il2CppGenericInst`, `Il2CppGenericClass`, and more, with verified x64 layouts matching Unity 2021.3 IL2CPP source
- **Collection Helpers** — High-level `IL2CPP.List(ptr)` and `IL2CPP.Dictionary(ptr)` wrappers for common Count/indexer access, plus guarded low-level struct helpers
- **Reflection & Caching** — Find classes, methods, fields, and properties by name with automatic caching. Supports both AOT and HybridCLR hot-update types
- **Type-safe Invocation** — Generic high-level `Invoke<T>`, `InvokeString`, plus low-level `InvokeGet<T>`, `InvokeGetString`, `UnboxValue<T>`, `BoxValue<T>`
- **Property & Static Field Access** — One-call helpers like `GetPropertyValue<T>`, `GetStaticFieldValue<T>` that eliminate the find-method-then-invoke boilerplate
- **GC Handle Support** — Pin/unpin IL2CPP objects to prevent garbage collection of objects held by raw pointers
- **Unicode String Support** — `il2cpp_string_new_utf16` for correct CJK/Unicode string creation (no ANSI truncation)
- **Class Hierarchy Traversal** — `FindMethodInHierarchy` walks parent classes to locate inherited methods
- **Overload Resolution** — `FindMethod` with `Il2CppTypeEnum[]` parameter types for disambiguating overloaded methods
- **Enum Helpers** — `GetEnumValue` / `CreateEnum` for boxing/unboxing IL2CPP enum types

## Requirements

- .NET 6.0+
- x64 Windows
- A Unity IL2CPP process that exports the native `il2cpp_*` API

> **Note:** Native struct layouts (`Il2CppStructs.cs`, `Il2CppCollections.cs`) are x64-only. The P/Invoke API and reflection layer use `IntPtr` and work on both architectures, but struct field offsets assume 8-byte pointers. x86 support would require a parallel set of struct definitions with different offsets.

## Standalone Repository Setup

This repository is designed to build outside the original monorepo.

The core package has no generated-wrapper assembly requirement for raw/fluent pointer usage and does not require `Il2Cppmscorlib.dll`.

1. Restore and build with:

```bash
dotnet build -c Release /p:GeneratePackageOnBuild=false
```

Wrapper type lookup, generated object wrapping, and runtime type injection helpers are intentionally outside the public core package. Projects that need generated wrappers should reference their own game interop assemblies and Il2CppInterop APIs directly.

## Quick Start

### Initialization

```csharp
// Call after HybridCLR has loaded hot-update assemblies
IL2CPP.Initialize();
IL2CPP.AddAssemblyHint("YourGame.Runtime");
```

### Fluent High-Level API

```csharp
// Static method with automatic string + float argument marshalling
IL2CPP.Class("UI.SystemTipsWindow")
    .Method("ShowTips")
    .Args<string, float>()
    .Invoke(systemTipsPtr, "Saved", 3.0f);

// Static AssetBundle call with managed byte[] and uint
IntPtr bundle = IL2CPP.Class("UnityEngine.AssetBundle")
    .Method("LoadFromMemory")
    .Args<byte[], uint>()
    .InvokeStatic(bundleBytes, 0u);

// Initialization paths can fail fast with clear exceptions
var showTips = IL2CPP.RequireClass("UI.SystemTipsWindow")
    .Method("ShowTips")
    .Args<string, float>()
    .Require();
```

`Try...` and normal fluent calls are silent and return null/default on lookup or marshalling failure. `Require...` uses standard .NET exceptions: `TypeLoadException` for missing classes, `MissingMethodException` for missing methods, `ArgumentException` for invalid arguments, and `InvalidOperationException` for failed invocation/allocation.

### Arrays and Collections

```csharp
IntPtr il2cppBytes = IL2CPP.Array(bundleBytes);
byte[] managedBytes = IL2CPP.ReadByteArray(il2cppBytes);

var items = IL2CPP.List(packageItemsPtr);
for (int i = 0; i < items.Count; i++)
{
    IntPtr item = items.GetItem(i);
}

IL2CPP.Dictionary(textureDictPtr).SetItem("Login_atlas0", texture.Pointer);
```

### Low-Level / Legacy API

The `Il2CppReflection` and `Il2CppAPI` APIs remain supported for direct runtime work. They are intentionally not marked obsolete, but new plugin code should prefer the fluent `IL2CPP` API when it avoids manual `IntPtr[]` setup.

#### Finding Types and Methods

```csharp
// Find a class
IntPtr klass = Il2CppReflection.FindClass("Game.Player.PlayerController");

// Find a method (by name + param count)
IntPtr method = Il2CppReflection.FindMethod(klass, "TakeDamage", 2);

// Find an overloaded method (by parameter types)
IntPtr method = Il2CppReflection.FindMethod(klass, "SetValue",
    Il2CppTypeEnum.IL2CPP_TYPE_STRING, Il2CppTypeEnum.IL2CPP_TYPE_I4);

// Find a method in class hierarchy (walks parent classes)
IntPtr method = Il2CppReflection.FindMethodInHierarchy(klass, "ToString", 0);
```

#### Invoking Methods

```csharp
// Generic invoke with unboxed return
int hp = Il2CppReflection.InvokeGet<int>(getHpMethod, playerInstance);
float speed = Il2CppReflection.InvokeGet<float>(getSpeedMethod, playerInstance);
string name = Il2CppReflection.InvokeGetString(getNameMethod, playerInstance);

// Static method invocation
Il2CppReflection.InvokeStaticMethod("Game.Manager.GameManager", "ResetState");
```

#### Properties and Fields

```csharp
// Read a property directly
int level = Il2CppReflection.GetPropertyValue<int>(instance, klass, "Level");
string title = Il2CppReflection.GetPropertyString(instance, klass, "Title");

// Static field access
IntPtr singleton = Il2CppReflection.GetStaticFieldValue(managerClass, "Instance");
int maxHp = Il2CppReflection.GetStaticFieldValue<int>(configClass, "MaxHP");
```

#### Struct Access

```csharp
// Unbox a value type
int value = Il2CppReflection.UnboxValue<int>(boxedObj);

// Read an IL2CPP array
byte[] data = Il2CppReflection.ReadArray<byte>(il2cppArray);
int[] ids = Il2CppReflection.ReadArray<int>(il2cppArray);

// Box a value
IntPtr boxed = Il2CppReflection.BoxValue<int>(intClass, 42);

// Direct collection struct access (faster than method calls)
int count = Il2CppCollectionHelper.GetListCount(listPtr);
int dictSize = Il2CppCollectionHelper.GetDictionaryCount(dictPtr);
```

### Native Struct Inspection

```csharp
// Read MethodInfo fields directly
unsafe {
    var mi = *(Il2CppMethodInfo*)methodInfoPtr;
    Console.WriteLine($"Method: {Marshal.PtrToStringAnsi(mi.name)}");
    Console.WriteLine($"Params: {mi.parameters_count}");
    Console.WriteLine($"Token: 0x{mi.token:X}");
}

// Use offset constants for pointer arithmetic
IntPtr methodPtr = *(IntPtr*)(methodInfoPtr + MethodInfoOffsets.MethodPointer);
```

### GC Pinning

```csharp
// Pin an object to prevent GC collection
uint handle = Il2CppReflection.PinObject(importantObj);

// ... use the raw pointer safely ...

// Release when done
Il2CppReflection.UnpinObject(handle);
```

## License

IL2CppSharp (C) 2026 SNWCreations, IL2CppSharp contributors. Licensed under Apache 2.0 License. See [LICENSE](LICENSE).
