# VRCW Non-Destructive Pipeline 設計仕様書

## 1. 文書の位置付け

本書は正本仕様書である。  
対象は、VRChat World 向けの非破壊 Build / Play 前処理基盤 `VRCW Non-Destructive Pipeline` とする。

正本ドキュメント:

- `Docs/`

---

## 2. 現在の実装状況

現時点で、以下を実装済みとする。

- `Build Session`
  - `IVRCSDKBuildRequestedCallback`
  - `IProcessSceneWithReport`
  - phase ベースの Pass 実行
- `Play Session`
  - `IProcessSceneWithReport` による Play Mode Scene 処理
  - Marker が存在する場合の自動 Play 前処理
  - Unity 標準の Scene 隔離を利用した非破壊 Play
- Pass authoring 基盤
  - `IWorldBuildPass`
  - `WorldBuildPassAttribute`
  - `WorldBuildPassTargets`
  - `WorldMarkerPass<TMarker>`
  - `WorldPassContext`
  - `WorldTransientAssets`
  - `WorldTransientObjects`
- Built-in Pass
  - `PlatformTogglePass`
  - `ReferenceSwapPass`
  - `BuildOnlyObjectPass`
  - `StripFromBuildPass`
- sample / test asset
  - Material keyword sample
  - Mesh edit sample
  - Serialized property override sample
  - Proxy collider sample
  - Prefab proxy sample
  - Mesh combine sample
  - Hierarchy mesh combine sample

未実装または今後の拡張対象は以下。

- `WorldTransientAssets` の対応対象拡張
- Scene object / prefab 操作 API の拡張
- report 以外の EditMode / integration test の自動化
- diff 可視化、validate-only、UX 整備

---

## 3. 目的

本基盤の目的は以下。

- NDMF のように、任意の非破壊処理を後付けで挿し込みやすくする
- Udon ではない通常の Editor C# で処理を記述できるようにする
- Build / Play の終了後に元 Scene / Prefab / Asset へ変更を残さない
- VRChat で許可されない任意 C# コンポーネントを最終 Build / Play 対象へ残さない

---

## 4. 非対象

本フェーズで対象外とするものは以下。

- multi-scene world 対応
- avatar 向け NDMF との完全互換
- Source Scene を直接加工して Undo や差し戻しで戻す方式
- Runtime 中に Pass を動的実行する仕組み
- report を毎回 file として自動生成する運用

---

## 5. 設計原則

### 5.1 Pass First

拡張の実行単位は `Editor C# で書かれた Pass` とする。  
Scene 上のコンポーネントは、Pass への入力データとして扱う。

### 5.2 Source Scene Immutable

非破壊性は Undo に依存しない。  
「元に戻す」のではなく、「最初から Source Scene を直接加工しない」ことで保証する。

### 5.3 Phase Required

Pass の順序制御には NDMF 準拠の phase を必須とする。

- `FirstChance`
- `PlatformInit`
- `Resolving`
- `Generating`
- `Transforming`
- `Optimizing`
- `PlatformFinish`

### 5.4 Build / Play Unified Model

Build と Play は同一の `IProcessSceneWithReport.OnProcessScene` エントリポイントを共有する。  
Pass 実行モデル、phase、marker モデル、report 形式、TransientAssets はすべて共通である。

### 5.5 Marker As Input

Marker は処理本体ではなく設定入力である。  
Marker 自体に複雑な実装詳細を背負わせず、処理は Pass と Context 側へ寄せる。

### 5.6 Fail Closed

非破壊性を安全に保証できない場合、Build / Play Session は中止する。

---

## 6. 背景と前提

### 6.1 プロジェクト前提

- Unity: `2022.3.22f1`
- VRChat Worlds SDK: `3.10.2`
- 対象: VRChat World
- world 構成: single-scene 前提

### 6.2 ローカル確認済み事項

- VRChat world build 前に `VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene)` が呼ばれる
- `com.vrchat.worlds` 内で `IProcessSceneWithReport` による build scene 前処理が使われている

このため、Build 側は

- `IVRCSDKBuildRequestedCallback`
- `IProcessSceneWithReport`

の組み合わせで入口と実処理を分離できる。

---

## 7. 全体アーキテクチャ

| コンポーネント                    | 役割                                                       | 配置    |
| --------------------------------- | ---------------------------------------------------------- | ------- |
| `WataWorldBuildRequestedCallback` | VRChat Build 入口                                          | Editor  |
| `WorldBuildSceneProcessor`        | `IProcessSceneWithReport` による Build / Play Scene 前処理 | Editor  |
| `WorldBuildOrchestrator`          | Build / Play 共通の Pass 実行統括                          | Editor  |
| `WorldPassRegistry`               | Pass の自動収集と順序付け                                  | Editor  |
| `IWorldBuildPass`                 | Pass の最小契約                                            | Editor  |
| `WorldMarkerPass<TMarker>`        | Marker 駆動 Pass の基底クラス                              | Editor  |
| `WorldBuildContext`               | orchestration 用の内部文脈                                 | Editor  |
| `WorldPassContext`                | Pass 作者向けの公開文脈                                    | Editor  |
| `WorldTransientAssets`            | Session 専用 asset clone 生成                              | Editor  |
| `WorldTransientObjects`           | Session 専用 Scene object / prefab 操作                    | Editor  |
| `WorldBuildArtifactStore`         | report の保存先管理                                        | Editor  |
| `WorldPassMarker`                 | Scene 上入力の基底コンポーネント                           | Runtime |
| `WorldBuildExecutionReport`       | 実行結果の記録                                             | Editor  |

---

## 8. 実行モデル

### 8.1 Build Session

1. VRChat SDK の build が開始される
2. `WataWorldBuildRequestedCallback` が preflight を行う
3. `WorldBuildSceneProcessor` が build scene を受け取る
4. `WorldBuildOrchestrator` が `WorldBuildContext` を構築する
5. phase 順に Pass を実行する
6. 最終 safety net として marker を strip する
7. report を Editor session に cache する

### 8.2 Play Session

1. ユーザーが通常の `Play` ボタンを押す
2. Unity が Play Mode 進入時に Scene のインメモリコピーを作成する
3. Unity が `IProcessSceneWithReport.OnProcessScene(scene, null)` を呼ぶ
4. `WorldBuildSceneProcessor` が `BuildReport == null` かつ Play Mode 進入中を検出する
5. Scene 内に `WorldPassMarker` が存在すれば Play session として処理する
6. `WorldBuildOrchestrator.ProcessScene` が Pass を実行し、Marker を strip する
7. Play Mode 中は加工済み Scene で動作する
8. Play 終了 → Unity が自動的に元の Scene 状態を復元する

### 8.3 非破壊保証の意味

Build では Unity build pipeline が渡す build scene を加工する。  
Play では Unity が Play Mode 用に作成する Scene のインメモリコピーを加工する。  
どちらも Source Scene を直接触らないため、終了時に元編集状態へ変更を残さない。

---

## 9. Pass モデル

### 9.1 最小インターフェース

```csharp
public interface IWorldBuildPass
{
    string DisplayName { get; }
    WorldBuildPhase Phase { get; }
    int Order { get; }
    WorldBuildPassTargets Targets { get; }

    bool AppliesTo(WorldBuildContext context);
    void Validate(WorldBuildContext context, IWorldValidationSink sink);
    void Execute(WorldBuildContext context);
}
```

### 9.2 属性

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class WorldBuildPassAttribute : Attribute
{
    public WorldBuildPassAttribute(WorldBuildPhase phase, int order = 0)
    {
        Phase = phase;
        Order = order;
        Targets = WorldBuildPassTargets.Build;
    }

    public WorldBuildPhase Phase { get; }
    public int Order { get; }
    public WorldBuildPassTargets Targets { get; set; }
}
```

### 9.3 Session 対象

`WorldBuildPassTargets` は以下を持つ。

- `Build`
- `Play`
- `BuildAndPlay`

Pass は自分がどの Session で動くかを属性で宣言する。

### 9.4 実行順

順序は以下で安定化する。

1. `Phase`
2. `Order`
3. Pass 型のフルネーム

### 9.5 発見方法

`WorldPassRegistry` は `TypeCache` を使って `IWorldBuildPass` 実装を自動収集し、`WorldBuildPassAttribute` を持つ型だけを対象にする。

---

## 10. Marker モデル

### 10.1 基底クラス

```csharp
public abstract class WorldPassMarker : MonoBehaviour
{
    [SerializeField] private bool _enabled = true;
    [SerializeField] private WorldBuildPlatformMask _platformMask = WorldBuildPlatformMask.All;
    [SerializeField] private bool _destroyAfterProcessing = true;

    public bool Enabled => _enabled;
    public WorldBuildPlatformMask PlatformMask => _platformMask;
    public bool DestroyAfterProcessing => _destroyAfterProcessing;
}
```

### 10.2 役割

Marker は以下だけを担う。

- Pass への入力データ
- platform 適用条件
- 処理後に自分を消すかどうかの定型指定

Marker が直接 `WorldBuildContext` や保存先 path を触る設計にはしない。

### 10.3 処理後の破棄

`DestroyAfterProcessing` が有効な marker は、Pass 実行後に `DestroyImmediate` されてよい。  
これは VRChat で許可されない任意 C# コンポーネントを Build / Play 対象へ残さないための要件である。

ただし、この破棄は build scene または play temp scene に対してのみ行う。  
Source Scene 上の marker は常に保持される。

### 10.4 実装済み Marker 例

- `PlatformToggleMarker`
- `ReferenceSwapMarker`
- `BuildOnlyObjectMarker`
- `StripFromBuildMarker`
- `GlobalKeywordColorSampleMarker`
- `MeshScaleSampleMarker`
- `SerializedPropertyOverrideSampleMarker`
- `ProxyColliderSampleMarker`
- `PrefabProxySampleMarker`
- `MeshCombineSampleMarker`
- `HierarchyMeshCombineSampleMarker`

---

## 11. Pass authoring API

### 11.1 `WorldBuildContext`

`WorldBuildContext` は orchestration 用の内部文脈であり、以下を保持する。

- `SessionId`
- `RequestedBuildType`
- `SessionKind`
- `BuildTarget`
- `TargetPlatformMask`
- `SourceScenePath`
- `Scene`
- `ExecutionReport`
- `TransientAssets`
- `TransientObjects`
- `PassContext`

Pass 作者が日常的に直接使う対象は `WorldBuildContext` ではなく `WorldPassContext` とする。

### 11.2 `WorldPassContext`

`WorldPassContext` は Pass 作者向けの facade であり、以下を公開する。
ただし位置づけとしては lower-level API とし、通常の Pass authoring ではこれを直接主対象にしない。

- `SessionId`
- `SessionKind`
- `RequestedBuildType`
- `BuildTarget`
- `TargetPlatformMask`
- `SourceScenePath`
- `Scene`
- `ExecutionReport`
- `TransientAssets`
- `TransientObjects`
- `GetMarkers<TMarker>()`
- `RegisterCleanup(Action)`
- `DestroyMarkerAfterProcessing(WorldPassMarker)`

### 11.3 `WorldMarkerPass<TMarker>`

Marker 駆動 Pass の標準基底クラスとして `WorldMarkerPass<TMarker>` を提供する。  
外部作者を含む Pass 作者に対しては、これを primary / canonical な authoring surface として案内する。  
Pass 作者は主に以下の hook を実装する。

- `ValidateMarker(...)`
- `ExecuteMarker(...)`
- `ShouldDestroyMarkerAfterExecute(...)`
- `EnumerateMarkers(...)`

加えて、Pass authoring で頻出する操作は `WorldMarkerPass<TMarker>` の保護メソッドとして短縮公開する。

- `CloneForSession(...)`
- `CreateObject(...)`
- `CloneObject(...)`
- `CloneComponentHost(...)`
- `InstantiatePrefab(...)`
- `GetOrCreateGeneratedRoot(...)`
- `EnsureComponent(...)`
- `DestroyComponent(...)`
- `ResetLocalTransform(...)`
- `CopyTransform(...)`
- `RegisterCleanup(...)`
- `DestroyMarker(...)`
- `DestroyObject(...)`
- `SetObjectActive(...)`
- `IsBuild(...)`
- `IsPlay(...)`
- `IsPlatform(...)`
- `TargetPlatform(...)`
- `SessionScene(...)`
- `CurrentSessionKind(...)`
- `CurrentSessionId(...)`

これにより、marker 列挙、platform 判定、処理後破棄を共通化しつつ、Pass 作者が `WorldPassContext` の下位 service を毎回たどらずに書ける。  
`WorldPassContext` の直接参照自体は許容するが、sample / builtin / docs では原則として `WorldMarkerPass<TMarker>` の helper を優先する。

---

## 12. Transient Asset / Object モデル

### 12.1 方針

`WorldTransientAssets` は「この session 専用の clone を得る」ための汎用 API とする。  
Pass 作者に temp asset の保存先や `AssetDatabase` 詳細を直接意識させない。

### 12.2 現在の公開 API

```csharp
public T CloneForSession<T>(T source, Object owner, string nameHint = null)
    where T : UnityEngine.Object
```

`owner` は clone 発生元の ownership / diagnostics 文脈を表す引数とする。  
現時点では clone の寿命や解放タイミングは制御しないが、report 上の発生元追跡には使う。

### 12.3 現在の挙動

Build / Play 共通で `Object.Instantiate` によるインメモリ clone を返す。  
Scene object から参照されることで Play Mode 中も生存する。

### 12.4 現在の制約

`CloneForSession<T>()` は現在、`GameObject` と `Component` を直接は扱わない。  
Scene object / prefab の複製は別 API として将来追加する。

また、`owner` は現在の実装では `GC` 保持や clone 解放タイミングの制御には使わない。  
一方で、`ExecutionReport.transientClones` には `source / clone / owner` の識別情報を記録する。
どの pass が clone を生成したかまでは、現時点では記録しない。

### 12.5 将来拡張方針
将来的な拡張は、個別型ごとの helper を先に増やすのではなく、まずプリミティブな API を安定化した上で検討する。  

- asset-like な UnityEngine.Object への対応拡張
- owner を用いた diagnostics / lifecycle tracking の強化
- sample / built-in pass で重複が確認できた操作のみ helper 化する
- path や AssetDatabase を pass 作者が直接扱わずに済む API を優先する

### 12.6 `WorldTransientObjects`

`WorldTransientObjects` は、現在の session Scene 上の `GameObject` / prefab instance を扱う高水準 API とする。  
Pass 作者に `SceneManager.MoveGameObjectToScene`, `PrefabUtility.InstantiatePrefab`, `DestroyImmediate` などの詳細を直接意識させない。

現在の公開 API:

```csharp
public GameObject Create(string name, Transform parent = null);
public GameObject Clone(GameObject source, Transform parent = null, string nameOverride = null);
public T CloneComponentHost<T>(T sourceComponent, Transform parent = null, string nameOverride = null)
    where T : Component;
public GameObject InstantiatePrefab(GameObject prefabAsset, Transform parent = null, string nameOverride = null);
public GameObject GetOrCreateGeneratedRoot(string name, Transform parent = null);
public T EnsureComponent<T>(GameObject target) where T : Component;
public void DestroyComponent(Component target);
public void ResetLocalTransform(Transform target);
public void CopyTransform(Transform source, Transform destination);
public void Destroy(GameObject target);
public void SetActive(GameObject target, bool active);
```

現在の方針:

- `WorldTransientObjects` は session Scene に閉じた `GameObject` 操作を担う
- `WorldTransientAssets` は asset 的な `UnityEngine.Object` の clone を担う
- `GameObject` / `Component` を `CloneForSession(...)` に渡す設計にはしない
- 通常の Pass authoring では `WorldMarkerPass<TMarker>` の helper 経由で使う
- API は便利 helper を無制限に増やすのではなく、`Create / Clone / InstantiatePrefab / Destroy / SetActive` のようなプリミティブを確実に揃える方針を優先する

候補 API メモ:

- `ReplaceObject(...)`
  - source object と generated object の置換を 1 操作で扱う候補
- `ReplaceWithPrefab(...)`
  - prefab instance 化と source object の disable / destroy をまとめる候補
- `CreateHiddenRoot(...)`
  - generated object 群の親となる session 専用 root を生成する候補
- `EnsureComponent<T>(...)`
  - generated object への component 付与を簡略化する候補
- `MoveToParent(...)`
  - session 上の object を再配置する候補
- `SetParent(...)`
  - parent の付け替えをプリミティブに扱う候補

- `SetLayer(...)`
  - generated object の layer を明示的に切り替える候補
- `SetStatic(...)`
  - generated object の static 状態を明示的に切り替える候補
- `EnsureChild(...)`
  - 名前付き child object の取得または作成を簡略化する候補

ただし、これらは現時点では候補であり、直ちに公開 API とするものではない。  
まずはプリミティブな操作を安定化し、その上で複数の sample / built-in pass で繰り返し現れるパターンだけを helper 化する。

また、`GetComponent<T>()` / `TryGetComponent<T>()` 相当の wrapper は現時点では優先度を低く置く。  
これらは Unity 標準 API が十分に明確であり、framework 側で追加しても意味の圧縮が小さいためである。

この分離により、asset 編集と scene object 編集を別レイヤとして扱い、外部作者にとっての API の見通しを保つ。

---

## 13. Artifact / Report 方針

### 13.1 出力先

永続的な report file が必要な場合のみ、以下へ export する。

- `Assets/WNDP Reports/`

### 13.2 サブディレクトリ

- report は自動保存しない

### 13.3 レポート

既定の出力先:

- Build / Play ともに直近 report は Editor session 内に保持する
- 必要時のみ `Assets/WNDP Reports/` 配下へ export する
- Editor session 内で保持するのは Build 用 1 件、Play 用 1 件の直近 report のみとする

report には pass 実行結果、diagnostics に加えて、`TransientAssets.CloneForSession(...)` によって生成された clone の
`source / clone / owner` 情報を `transientClones` として含める。
pass 名や pass 型までは現時点では含めない。

### 13.4 許容条件

生成物の出力先が `Assets/` 配下であること自体は許容する。  
ただし、生成物が意図せず最終 world 依存へ混入しないことを条件とする。

### 13.5 Git 運用

report は常時ファイルとして保持せず、必要時のみ export する。  
`Assets/WNDP Reports/` はユーザーが明示 export した成果物置き場であり、git 管理するかどうかは利用側の運用で決める。

---

### 13.6 report 実装状況
現状の実装では、report は Build / Play のたびに file へ自動生成しない。  
代わりに ExecutionReport は Editor session 内に cache し、必要になったタイミングで明示 export する。  

現状の挙動:

- package 配下や tool 配下へ report を都度自動生成しない
- 直近の Build report / Play report は session cache として保持する
- report file が必要な場合のみ、メニュー操作で Assets/ 配下へ export する
- export 先の既定フォルダは Assets/WNDP Reports/ とする
- export file 名には sessionId を含め、同名 file が存在する場合は連番で重複回避する
- sessionKind が未知の値だった場合は warning を出して Build として扱う

配布方針:

- 配布先が Packages/ 配下であっても report を package 配下へ自動生成しない
- report が必要な場合のみ、明示 export で Assets/ 配下へ書き出す
- この方針は Unity の Build Report Inspector のような on-demand export を意図したものとする

現状の export 入口:

- Window/WataOfuton/VRCW Non-Destructive Pipeline/Export Last Build Report
- Window/WataOfuton/VRCW Non-Destructive Pipeline/Export Last Play Report

## 14. ディレクトリ構成

```text
Package Root/
  Docs/
    WNDP_Spec.md
    UserGuide.md
  Runtime/
    WorldPassMarker.cs
    WorldBuildPlatformMask.cs
    Markers/
      PlatformToggleMarker.cs
      ReferenceSwapMarker.cs
      BuildOnlyObjectMarker.cs
      StripFromBuildMarker.cs
  Editor/
    Build/
      WataWorldBuildRequestedCallback.cs
      WorldBuildSceneProcessor.cs
    Core/
      IWorldValidationSink.cs
      WorldBuildArtifactStore.cs
      WorldBuildContext.cs
      WorldBuildExecutionReport.cs
      WorldBuildOrchestrator.cs
      WorldBuildPhase.cs
      WorldBuildPlatformUtility.cs
      WorldBuildSessionState.cs
      WorldPassContext.cs
      WorldPassRegistry.cs
      WorldSessionKind.cs
      WorldTransientAssets.cs
      WorldTransientObjects.cs
      WorldValidationSink.cs
    Passes/
      IWorldBuildPass.cs
      WorldBuildPassAttribute.cs
      WorldBuildPassBase.cs
      WorldBuildPassTargets.cs
      WorldMarkerPass.cs
      Builtin/
        PlatformTogglePass.cs
        ReferenceSwapPass.cs
        BuildOnlyObjectPass.cs
        StripFromBuildPass.cs
  Samples/
    MaterialKeyword/
      GlobalKeywordColorSampleMarker.cs
      NewUnlitShader.shader
    PrefabProxy/
      PrefabProxySampleMarker.cs
    MeshEdit/
      MeshScaleSampleMarker.cs
    Editor/
      MaterialKeyword/
        GlobalKeywordColorSamplePass.cs
      PrefabProxy/
        PrefabProxySamplePass.cs
      MeshEdit/
        MeshScaleSamplePass.cs
      ...
  Tests/
    Editor/
      WataOfuton.Tool.WNDP.Tests.Editor.asmdef
      WorldBuildArtifactStoreTests.cs
```

---

## 15. 実装済み機能

### 15.1 Build

- `IVRCSDKBuildRequestedCallback` 入口
- `IProcessSceneWithReport` による build scene 処理
- preflight validation
- phase 実行
- validation sink
- report 出力

### 15.2 Play

- `IProcessSceneWithReport` による Play Mode 自動検出
- Marker 存在時の Play 前処理 Pass 実行
- Unity 標準の Scene 隔離による非破壊保証
- report 出力

### 15.3 Built-in Pass

- `PlatformTogglePass`
- `ReferenceSwapPass`
- `BuildOnlyObjectPass`
- `StripFromBuildPass`

### 15.4 Sample

- Material keyword sample
  - session clone した material を操作する
- Mesh edit sample
  - session clone した mesh の頂点を編集する
- Serialized property override sample
  - `SerializedObject` / `SerializedProperty` を通して Scene object の値を一時的に上書きする
- Proxy collider sample
  - Build / Play 時だけ簡易 collider 用の child object を生成する
- Prefab proxy sample
  - Build / Play 時だけ prefab proxy を instance 化し、source object の扱いを切り替える
- Mesh combine sample
  - 子 mesh 群を一時的に結合し、最適化系 Pass の書き方を示す
- Hierarchy mesh combine sample
  - Empty root に marker を付け、子孫 mesh だけをまとめて root 自身へ `MeshRenderer` 等を付与する階層指向 sample

これにより、本基盤が material 専用ではなく、非破壊な asset clone を前提に多様な処理へ拡張できることを確認済みとする。

---

## 16. 今後の実装予定

優先度が高いものから順に以下。

### 16.1 Transient API の拡張

- asset-like object の対応型拡張
- scene object / prefab 操作のうち、まだプリミティブとして不足している API の追加
- Pass 作者が path や AssetDatabase を直接扱わずに済む API の整備
- 高水準 helper を先に増やしすぎるのではなく、プリミティブな API を確実に揃える
- helper は sample / built-in pass の重複パターンが確認できたものから追加する

### 16.2 自動テスト

- Pass Registry discovery / ordering
- Build 非破壊性
- Play Session 復元
- Built-in Pass の回帰テスト

### 16.3 UX / 補助機能

- validate-only 実行
- 実行 diff 可視化
- report の見やすさ改善
- sample / fixture の拡充

### 16.4 内部品質改善

以下は現時点で実害は小さいが、規模拡大時に検討すべき項目。

- **`WorldPassRegistry.GetPasses()` のキャッシュ**: 現在は呼び出し毎に `Activator.CreateInstance` で全 Pass をインスタンス化している。Pass 数が増えた場合、Phase 実行前に 1 度だけ生成しキャッシュする方式を検討する
- **`WorldBuildDiagnostic.context` の型変更**: 現在は `string`（`Object.name`）を保持している。`UnityEngine.Object` 参照を別途持てば、Console ログからのクリック → Inspector ジャンプが可能になる。ただし JSON シリアライズとの両立が必要
- **`WorldPassContext` の位置づけ明確化**: 現状ほぼ `WorldBuildContext` の proxy だが、Pass 向け API を意図的に狭窄する目的で存在する。Pass 数・API が増えた場合に Context 分離の恩恵が明確になるため、現設計を維持する

---

### 16.5 名称

公開名は現時点では未確定とする。  
公開名は `VRCW Non-Destructive Pipeline`、内部識別子は `WNDP` とする。

現時点の検討メモ:

- `VRC` / `World` を名前に残す方向は有力
- `ND` は技術略号として許容範囲
- 略称そのものには必ずしもこだわらない
- `Pipeline` は、Build / Play 入口から phase / pass / cleanup / report までを順序付きで扱う基盤、という意味で妥当
- `VRCW` は `VRChat World` の略とし、README 冒頭でのみ補足する

名称は README / package 名 / namespace 名を含めて配布前に再整理する。

## 17. 安全ルール

以下を基盤ルールとして禁止する。

- `EditorSceneManager.SaveScene` による Source Scene 保存
- `EditorSceneManager.SaveOpenScenes`
- `AssetDatabase.SaveAssets`
- `PrefabUtility.SaveAsPrefabAsset`
- 明示 export 以外の report 自動生成

許可するもの:

- build scene または Play Mode の Scene コピー上の `GameObject` / `Component` 変更
- 明示 export による `Assets/WNDP Reports/` 配下への report 出力
- 通常の Editor C# ロジック

---

## 18. テスト方針

### 18.1 自動テスト

今後、以下を EditMode / integration test で担保する。

- Pass が自動収集される
- `Phase -> Order -> FullName` で順序が安定する
- Build 後に Source Scene に差分が残らない
- Play 後に Source Scene に差分が残らない
- report export が正しい場所へ生成される

2026-03-13 時点では、report 周りについて以下の EditMode test を実装済み:

- `StoreLastReport -> TryGetLastReport` の round-trip
- Build / Play の直近 report cache が分離されること
- `ExportLastReport` が file を生成すること
- 同名 export 時に連番で重複回避すること
- 未知の `sessionKind` が warning を出して Build 扱いになること

### 18.2 手動確認

最低限、以下を継続確認する。

- PC Build で sample が反映される
- Play Session で sample が反映される
- Play 終了後に Source Scene の marker / asset が元のままである
- Marker / 補助コンポーネントが最終 Build / Play 対象に残らない

---

## 19. 想定リスク

### 19.1 VRChat SDK / Unity 更新

Build callback や Play 周辺挙動は将来変わる可能性がある。  
session state は idempotent に保つ。

### 19.2 Configurable Enter Play Mode

Play Session は `IProcessSceneWithReport.OnProcessScene` を入口としており、Unity の Configurable Enter Play Mode で `Reload Scene` が無効化されている場合は `OnProcessScene` が呼ばれず Play 前処理がスキップされる。  
VRChat SDK 自体が Scene reload を前提としているため、VRChat ワールド開発で `Reload Scene` を無効化する実用シナリオは想定しない。

### 19.3 Play Mode での TransientAssets 生存

`OnProcessScene` で生成したインメモリ clone は、Scene object から参照されていれば Play Mode 中も生存する。  
Scene object から参照されない standalone asset（例: ScriptableObject）は GC 対象になりうるため、将来の Pass 拡張時に注意が必要である。

---

## 20. 実装判断

本フェーズの判断は以下とする。

- Build / Play の両 Session を扱う
- single-scene のみを対象とする
- Pass ベースを中核 API とする
- Marker は入力データとする
- phase 分割を必須とする
- 通常の Editor C# を前提とする
- Source Scene 不変を原則とする
- report export 先の既定値は `Assets/WNDP Reports/` とする
- 正本ドキュメントは `Docs/` とする

---

## 21. 参考

### 21.1 公式資料

- VRChat Creators: Build Pipeline Callbacks and Interfaces
- Unity Scripting API: `IProcessSceneWithReport`
- Unity Scripting API: `PostProcessSceneAttribute`

### 21.2 ローカル参照

- `Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/VRCSdkControlPanelWorldBuilderV3.cs`
- `Packages/com.vrchat.worlds/Editor/Udon/UdonEditorManager.cs`
- `Packages/com.vrchat.base/Editor/VRCSDK/Dependencies/VRChat/BuildPipeline/Samples/VRCSDKBuildRequestedCallbackSample.cs`

---

以上を現行 `WNDP` の設計仕様とする。
