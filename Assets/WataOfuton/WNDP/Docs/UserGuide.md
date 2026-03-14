# VRCW Non-Destructive Pipeline User Guide

この文書は、VRCW Non-Destructive Pipeline を使って marker / pass を自作する人向けのガイドです。

関連ドキュメント:

- 入口: [README.md](../../../../README.md)
- サンプル一覧: [Samples/README.md](../Samples/README.md)

## 基本方針

- Source Scene と source asset は直接変更しない
- Runtime 側には marker を置き、実処理は Editor C# の pass に書く
- Pass 作者が主に触る API は `WorldMarkerPass<TMarker>` とする
- `WorldPassContext` は lower-level API であり、必要なときだけ直接使う
- 高水準 helper を増やしすぎず、まずはプリミティブな API を確実に使う

## 最小構成

WNDP で marker / pass を自作するときは、基本的に次の 2 つを作ります。

1. Runtime 側の marker component
2. Editor 側の `WorldMarkerPass<TMarker>`

### 例: marker

```csharp
using UnityEngine;
using WataOfuton.Tool.WNDP;

public sealed class ExampleScaleMarker : WorldPassMarker
{
    [SerializeField] private Vector3 _scale = new(1.0f, 2.0f, 1.0f);

    public Vector3 Scale => _scale;
}
```

### 例: pass

```csharp
using WataOfuton.Tool.WNDP;
using WataOfuton.Tool.WNDP.Editor;

[WorldBuildPass(WorldBuildPhase.Transforming, 100, Targets = WorldBuildPassTargets.BuildAndPlay)]
public sealed class ExampleScalePass : WorldMarkerPass<ExampleScaleMarker>
{
    public override string DisplayName => "Example Scale";

    protected override void ValidateMarker(
        WorldPassContext context,
        ExampleScaleMarker marker,
        IWorldValidationSink sink)
    {
        if (marker.GetComponent<Transform>() == null)
        {
            sink.Error("ExampleScaleMarker requires a Transform.", marker);
        }
    }

    protected override void ExecuteMarker(WorldPassContext context, ExampleScaleMarker marker)
    {
        marker.transform.localScale = marker.Scale;
    }
}
```

## まず覚えるべき API

`WorldMarkerPass<TMarker>` から使える主な helper は次です。

- Asset clone
  - `CloneForSession(...)`
- Scene object / prefab
  - `CreateObject(...)`
  - `CloneObject(...)`
  - `CloneComponentHost(...)`
  - `InstantiatePrefab(...)`
  - `GetOrCreateGeneratedRoot(...)`
- Component / transform
  - `EnsureComponent(...)`
  - `DestroyComponent(...)`
  - `ResetLocalTransform(...)`
  - `CopyTransform(...)`
- Lifecycle
  - `RegisterCleanup(...)`
  - `DestroyMarker(...)`
  - `DestroyObject(...)`
  - `SetObjectActive(...)`
- Session 判定
  - `IsBuild(...)`
  - `IsPlay(...)`
  - `IsPlatform(...)`
  - `TargetPlatform(...)`
  - `SessionScene(...)`
  - `CurrentSessionKind(...)`
  - `CurrentSessionId(...)`

## API 詳細

### 1. Asset clone 系

#### `CloneForSession(context, source, owner, nameHint = null)`

asset-like object を session 用に複製します。

主な用途:

- `Material` を Build / Play 時だけ差し替えたい
- `Mesh` を複製して頂点編集したい
- `AnimationClip` や `ScriptableObject` を一時加工したい

使いどころ:

- source asset を直接触ってはいけないとき
- Build と Play で同じ書き味にしたいとき

注意点:

- `GameObject` と `Component` には使わない
- scene object / prefab は `WorldTransientObjects` 系 helper を使う
- `owner` は clone の発生元を report に残すための文脈情報

例:

```csharp
var clonedMesh = CloneForSession(context, sourceMesh, marker, "Scaled");
```

### 2. Scene object / prefab 系

#### `CreateObject(context, name, parent = null)`

session scene 内に空の `GameObject` を作ります。

主な用途:

- generated object の土台を作る
- proxy collider 用の child object を作る
- 複数の生成物をぶら下げる root を作る

例:

```csharp
var proxy = CreateObject(context, "Proxy Collider", marker.transform);
```

#### `CloneObject(context, source, parent = null, nameOverride = null)`

`GameObject` を session scene 内で複製します。

主な用途:

- source object を残したまま clone 側だけ加工したい
- 既存 hierarchy を一時的に複製したい

注意点:

- source object はそのまま残る
- source を隠すか消すかは `SetObjectActive(...)` や `DestroyObject(...)` で明示する

#### `CloneComponentHost(context, sourceComponent, parent = null, nameOverride = null)`

指定した `Component` が付いている host `GameObject` を複製します。

主な用途:

- 特定 component を持つ object を clone 側で編集したい
- component 単体ではなく host object ごと複製したい

#### `InstantiatePrefab(context, prefabAsset, parent = null, nameOverride = null)`

prefab asset を session scene 内に instance 化します。

主な用途:

- Build / Play 時だけ proxy prefab を差し込みたい
- source object を軽量 prefab に差し替えたい

例:

```csharp
var instance = InstantiatePrefab(context, marker.PrefabAsset, marker.transform, "Runtime Proxy");
```

#### `GetOrCreateGeneratedRoot(context, name, parent = null)`

generated object をまとめる root を取得します。無ければ作成します。

主な用途:

- generated object を毎回同じ場所にまとめたい
- 複数の child object を作る pass で生成先を安定化したい

挙動:

- `parent` あり: その直下で同名 child を探す
- `parent` なし: session scene の root から同名 object を探す
- 新規作成時も再利用時も transform は既定値に揃える

例:

```csharp
var generatedRoot = GetOrCreateGeneratedRoot(context, "Generated", marker.transform);
```

### 3. Component / transform 系

#### `EnsureComponent<T>(context, target)`

`GameObject` に `T` が付いていればそれを返し、無ければ追加して返します。

主な用途:

- `MeshFilter` / `MeshRenderer` / `MeshCollider` を安全に確保したい
- generated object に必要 component を確実に持たせたい

例:

```csharp
var meshFilter = EnsureComponent<MeshFilter>(context, generatedRoot);
```

#### `DestroyComponent(context, component)`

指定した component を session scene 上で削除します。

主な用途:

- generated root を再利用するときに古い component を消したい
- 不要になった一時 component だけを消したい

注意点:

- source scene ではなく session scene 上の object に対して使う

#### `ResetLocalTransform(context, target)`

local transform を既定値に戻します。

既定値:

- localPosition = `Vector3.zero`
- localRotation = `Quaternion.identity`
- localScale = `Vector3.one`

主な用途:

- generated child を親の直下に素直に置きたい
- prefab / generated root の local 値を揃えたい

#### `CopyTransform(context, source, destination)`

`Transform` の値を `source` から `destination` へコピーします。

挙動:

- 親が同じなら local 値をコピー
- 親が異なるなら world 位置・回転と localScale をコピー

主な用途:

- proxy object を source object と同じ位置へ置きたい
- generated object に source の transform を引き継ぎたい

### 4. Lifecycle / cleanup 系

#### `RegisterCleanup(context, action)`

pass 完了後に cleanup を登録します。

主な用途:

- 一時的な global state を戻したい
- helper では表現しきれない特殊 cleanup を足したい

注意点:

- source scene の復元処理ではなく、session 中に必要な後始末へ使う

#### `DestroyMarker(context, marker)`

marker component を session scene 上で `DestroyImmediate` します。

主な用途:

- Build / Play 対象に marker を残したくない
- `DestroyAfterProcessing` を手動で早めに適用したい

#### `DestroyObject(context, target)`

`GameObject` を session scene 上で削除します。

主な用途:

- generated object を再生成前に消したい
- source object を session 中だけ破棄したい

#### `SetObjectActive(context, target, active)`

`GameObject` の active state を切り替えます。

主な用途:

- source renderer / source object を一時的に無効化したい
- Build / Play 時だけ表示・非表示を切り替えたい

### 5. Session 判定系

#### `IsBuild(context)`

現在の session が Build かどうかを返します。

#### `IsPlay(context)`

現在の session が Play かどうかを返します。

#### `IsPlatform(context, mask)`

現在の target platform が指定 mask を含むかを返します。

注意点:

- 完全一致ではなく flags の包含判定

#### `TargetPlatform(context)`

現在の target platform mask を返します。

#### `SessionScene(context)`

現在 pass が処理している session scene を返します。

#### `CurrentSessionKind(context)`

現在の session 種別を返します。値は `Build` または `Play` です。

#### `CurrentSessionId(context)`

現在の session ID を返します。

主な用途:

- report や generated object 名に session 単位の識別を入れたいとき

## asset と object の使い分け

asset-like object:

- `Material`
- `Mesh`
- `AnimationClip`
- `ScriptableObject` など

これらは `CloneForSession(...)` を使います。

```csharp
var clonedMesh = CloneForSession(context, sourceMesh, marker, "Scaled");
```

Scene object / prefab:

- `GameObject`
- `Component`
- Prefab instance

これらは object helper を使います。

```csharp
var root = GetOrCreateGeneratedRoot(context, "Generated", marker.transform);
var box = EnsureComponent<BoxCollider>(context, root);
var instance = InstantiatePrefab(context, marker.PrefabAsset, marker.transform, "Runtime Proxy");
SetObjectActive(context, marker.gameObject, false);
```

## `WorldPassContext` を直接使う場面

通常は `WorldMarkerPass<TMarker>` の helper で十分です。`WorldPassContext` を直接使うのは次のような場面に限るのが自然です。

- `ExecutionReport` に診断情報を追加したい
- `TransientAssets` / `TransientObjects` の lower-level API を使いたい
- helper では表現しづらい特殊なケースを扱いたい

sample / builtin pass と同じ書き味に揃えたい場合は、まず helper で書けないかを確認してください。

## やってはいけないこと

- source scene を直接編集する
- source asset を直接書き換える
- pass の中で `AssetDatabase.SaveAssets` を呼ぶ
- `GameObject` / `Component` に `CloneForSession(...)` を使う
- lower-level API をむやみに辿って複雑な pass を組む

## おすすめ sample

最初に読むなら次の sample がおすすめです。

1. `Samples/Editor/MaterialKeyword/GlobalKeywordColorSamplePass.cs`
2. `Samples/Editor/MeshEdit/MeshScaleSamplePass.cs`
3. `Samples/Editor/SerializedPropertyOverride/SerializedPropertyOverrideSamplePass.cs`
4. `Samples/Editor/PrefabProxy/PrefabProxySamplePass.cs`
5. `Samples/Editor/HierarchyMeshCombine/HierarchyMeshCombineSamplePass.cs`
