# IL2CppSharp

[English](README.md) | 简体中文

[![CI](https://github.com/SNWCreations/IL2CppSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/SNWCreations/IL2CppSharp/actions/workflows/ci.yml)

一个轻量级的 C# 库，用于与 Unity 游戏的 IL2CPP 运行时交互。它提供适合日常插件代码的 Fluent 高层 API，同时保留 `Il2CppReflection` / `Il2CppAPI` 低层入口用于直接运行时操作。

## 功能特性

- **Fluent 高层 API** -- `IL2CPP.Class("Namespace.Type").Method("Name").Args<T1, T2>().Invoke(...)` 统一处理查找、重载签名、参数封送、数组、装箱和返回值转换
- **P/Invoke API** -- 60+ 集中管理的 `il2cpp_*` 原生导入，覆盖类、方法、字段、属性、数组、类型、字符串、GC 和运行时初始化
- **类型化原生结构体** -- `Il2CppObject`、`Il2CppArray`、`Il2CppMethodInfo`、`Il2CppType`、`Il2CppFieldInfo`、`Il2CppGenericInst`、`Il2CppGenericClass` 等，x64 布局已对照 Unity 2021.3 IL2CPP 源码验证
- **集合辅助** -- 高层 `IL2CPP.List(ptr)` 和 `IL2CPP.Dictionary(ptr)` 包装常见 Count/indexer 访问，并保留带边界限制的低层结构体辅助
- **反射与缓存** -- 按名称查找类、方法、字段和属性，自动缓存。同时支持 AOT 和 HybridCLR 热更新类型
- **类型安全调用** -- 高层泛型 `Invoke<T>`、`InvokeString`，以及低层 `InvokeGet<T>`、`InvokeGetString`、`UnboxValue<T>`、`BoxValue<T>`
- **属性与静态字段访问** -- `GetPropertyValue<T>`、`GetStaticFieldValue<T>` 等一步到位的辅助方法，消除查找方法再调用的样板代码
- **GC Handle 支持** -- 固定/释放 IL2CPP 对象，防止原生指针持有的对象被垃圾回收
- **Unicode 字符串支持** -- `il2cpp_string_new_utf16` 正确创建中日韩/Unicode 字符串（无 ANSI 截断）
- **类层次遍历** -- `FindMethodInHierarchy` 沿父类链查找继承的方法
- **重载解析** -- `FindMethod` 支持 `Il2CppTypeEnum[]` 参数类型匹配，精确区分重载方法
- **枚举辅助** -- `GetEnumValue` / `CreateEnum` 用于 IL2CPP 枚举类型的装箱/拆箱

## 环境要求

- .NET 6.0+
- x64 Windows
- 导出原生 `il2cpp_*` API 的 Unity IL2CPP 进程

> **注意：** 原生结构体布局（`Il2CppStructs.cs`、`Il2CppCollections.cs`）仅支持 x64。P/Invoke API 和反射层使用 `IntPtr`，在两种架构上均可工作，但结构体字段偏移假定指针为 8 字节。x86 支持需要一套独立的结构体定义。

## 独立仓库构建

核心包用于 raw/fluent 指针 API，不需要生成的 wrapper 程序集，也不需要 `Il2Cppmscorlib.dll`。

```bash
dotnet build -c Release /p:GeneratePackageOnBuild=false
```

wrapper 类型查找、托管 wrapper 对象转换和运行时类型注入辅助不属于公开核心包。需要生成 wrapper 的项目应自行引用对应游戏生成的 interop 程序集和 Il2CppInterop API。

## 快速上手

### 初始化

```csharp
// 在 HybridCLR 加载热更新程序集后调用
IL2CPP.Initialize();
IL2CPP.AddAssemblyHint("YourGame.Runtime");
```

### Fluent 高层 API

```csharp
// 自动封送 string + float 参数
IL2CPP.Class("UI.SystemTipsWindow")
    .Method("ShowTips")
    .Args<string, float>()
    .Invoke(systemTipsPtr, "Saved", 3.0f);

// 静态 AssetBundle 调用，直接传托管 byte[] 和 uint
IntPtr bundle = IL2CPP.Class("UnityEngine.AssetBundle")
    .Method("LoadFromMemory")
    .Args<byte[], uint>()
    .InvokeStatic(bundleBytes, 0u);

// 初始化路径可以使用 Require 失败即抛出
var showTips = IL2CPP.RequireClass("UI.SystemTipsWindow")
    .Method("ShowTips")
    .Args<string, float>()
    .Require();
```

普通 Fluent 调用和 `Try...` 路径保持静默，失败时返回 null/default。`Require...` 路径使用标准 .NET 异常：类缺失抛 `TypeLoadException`，方法缺失抛 `MissingMethodException`，参数无效抛 `ArgumentException`，调用或分配失败抛 `InvalidOperationException`。

### 数组和集合

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

`Il2CppReflection` 和 `Il2CppAPI` 仍保持支持，用于直接运行时操作。它们不会被标记为 obsolete；新插件代码在能避免手动 `IntPtr[]` 装参时应优先使用 `IL2CPP` Fluent API。

#### 查找类型和方法

```csharp
// 查找类
IntPtr klass = Il2CppReflection.FindClass("Game.Player.PlayerController");

// 按名称 + 参数数量查找方法
IntPtr method = Il2CppReflection.FindMethod(klass, "TakeDamage", 2);

// 按参数类型查找重载方法
IntPtr method = Il2CppReflection.FindMethod(klass, "SetValue",
    Il2CppTypeEnum.IL2CPP_TYPE_STRING, Il2CppTypeEnum.IL2CPP_TYPE_I4);

// 沿类层次查找方法（遍历父类）
IntPtr method = Il2CppReflection.FindMethodInHierarchy(klass, "ToString", 0);
```

#### 调用方法

```csharp
// 泛型调用并拆箱返回值
int hp = Il2CppReflection.InvokeGet<int>(getHpMethod, playerInstance);
float speed = Il2CppReflection.InvokeGet<float>(getSpeedMethod, playerInstance);
string name = Il2CppReflection.InvokeGetString(getNameMethod, playerInstance);

// 静态方法调用
Il2CppReflection.InvokeStaticMethod("Game.Manager.GameManager", "ResetState");
```

#### 属性和字段

```csharp
// 直接读取属性
int level = Il2CppReflection.GetPropertyValue<int>(instance, klass, "Level");
string title = Il2CppReflection.GetPropertyString(instance, klass, "Title");

// 静态字段访问
IntPtr singleton = Il2CppReflection.GetStaticFieldValue(managerClass, "Instance");
int maxHp = Il2CppReflection.GetStaticFieldValue<int>(configClass, "MaxHP");
```

#### 结构体访问

```csharp
// 拆箱值类型
int value = Il2CppReflection.UnboxValue<int>(boxedObj);

// 读取 IL2CPP 数组
byte[] data = Il2CppReflection.ReadArray<byte>(il2cppArray);
int[] ids = Il2CppReflection.ReadArray<int>(il2cppArray);

// 装箱值类型
IntPtr boxed = Il2CppReflection.BoxValue<int>(intClass, 42);

// 直接集合结构体访问（比方法调用更快）
int count = Il2CppCollectionHelper.GetListCount(listPtr);
int dictSize = Il2CppCollectionHelper.GetDictionaryCount(dictPtr);
```

### 原生结构体检查

```csharp
// 直接读取 MethodInfo 字段
unsafe {
    var mi = *(Il2CppMethodInfo*)methodInfoPtr;
    Console.WriteLine($"Method: {Marshal.PtrToStringAnsi(mi.name)}");
    Console.WriteLine($"Params: {mi.parameters_count}");
    Console.WriteLine($"Token: 0x{mi.token:X}");
}

// 使用偏移常量进行指针运算
IntPtr methodPtr = *(IntPtr*)(methodInfoPtr + MethodInfoOffsets.MethodPointer);
```

### GC 固定

```csharp
// 固定对象防止 GC 回收
uint handle = Il2CppReflection.PinObject(importantObj);

// ... 安全使用原生指针 ...

// 使用完毕后释放
Il2CppReflection.UnpinObject(handle);
```

## 许可证

IL2CppSharp (C) 2026 SNWCreations, IL2CppSharp contributors. Licensed under Apache 2.0 License. See [LICENSE](LICENSE).

## 发布

包发布由 `Publish Package` GitHub Actions workflow 通过 NuGet trusted publishing 处理。它会先发布到 `https://int.nugettest.org/v3/index.json`。只有显式启用 `publish_to_nuget` 后，才会继续发布到 NuGet.org。

需要为 owner `SNWCreations`、repository `IL2CppSharp`、workflow file `publish.yml` 以及 environments `nuget-test` 和 `nuget` 配置 trusted publishing policies。将 repository variable 或 secret `NUGET_USER` 设置为这些 policies 使用的 NuGet profile name。
