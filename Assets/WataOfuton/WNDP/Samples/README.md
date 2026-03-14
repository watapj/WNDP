# VRCW Non-Destructive Pipeline Samples

## 構成

- `MaterialKeyword/`
  - material-local keyword 切り替え sample
- `MeshEdit/`
  - 非破壊 mesh 編集 sample
- `SerializedPropertyOverride/`
  - `SerializedObject` ベースの scene property 上書き sample
- `ProxyCollider/`
  - collider 用 generated child object sample
- `PrefabProxy/`
  - prefab proxy 生成 sample
- `MeshCombine/`
  - 一時 mesh 結合 sample
- `HierarchyMeshCombine/`
  - Empty root 階層向けの子孫 mesh 結合 sample
- `Editor/MaterialKeyword/`
  - material sample 用 editor pass
- `Editor/MeshEdit/`
  - mesh sample 用 editor pass
- `Editor/SerializedPropertyOverride/`
  - serialized property sample 用 editor pass
- `Editor/ProxyCollider/`
  - proxy collider sample 用 editor pass
- `Editor/PrefabProxy/`
  - prefab proxy sample 用 editor pass
- `Editor/MeshCombine/`
  - mesh combine sample 用 editor pass
- `Editor/HierarchyMeshCombine/`
  - hierarchy mesh combine sample 用 editor pass

## Material Keyword Sample

- Runtime marker: `MaterialKeyword/GlobalKeywordColorSampleMarker.cs`
- Editor pass: `Editor/MaterialKeyword/GlobalKeywordColorSamplePass.cs`
- Shader: `MaterialKeyword/NewUnlitShader.shader`
- Sample material: `MaterialKeyword/WataOfuton_WNDPSample.mat`
- Shader name: `WataOfuton/WNDP/Samples/GlobalKeywordColor`
- Material-local keyword: `WATA_WNDP_SAMPLE_COLOR_SWAP`

### 使い方

1. `MaterialKeyword/WataOfuton_WNDPSample.mat` か、`WataOfuton/WNDP/Samples/GlobalKeywordColor` を使う material を cube などの visible object に割り当てる
2. 同じ GameObject に `GlobalKeywordColorSampleMarker` を付ける
3. world build を実行するか Play を開始する

### 期待結果

- keyword が無効のときは `_BaseColor` が使われる
- keyword が有効のときは `_KeywordColor` が使われる
- source scene と source material asset は Build / Play 終了後も変化しない

## Mesh Edit Sample

- Runtime marker: `MeshEdit/MeshScaleSampleMarker.cs`
- Editor pass: `Editor/MeshEdit/MeshScaleSamplePass.cs`
- 対象 component: `MeshFilter`
- 任意の補助 component: `MeshCollider`

### 使い方

1. cube などの mesh object を sample scene に置く
2. 同じ GameObject に `MeshScaleSampleMarker` を付ける
3. 必要なら `Vertex Scale` を調整する
   - 既定値 `(1.0, 1.75, 1.0)` は bounds 中心を基準に上方向へ引き伸ばす
4. world build を実行するか Play を開始する

### 期待結果

- object の形状は build scene または isolated play session でのみ変化する
- 同じ GameObject の `MeshCollider` が元 mesh を参照していた場合は、編集後 clone に差し替わる
- source mesh asset と source scene は変化しない

## Serialized Property Override Sample

- Runtime marker: `SerializedPropertyOverride/SerializedPropertyOverrideSampleMarker.cs`
- Editor pass: `Editor/SerializedPropertyOverride/SerializedPropertyOverrideSamplePass.cs`
- 対象 component: 任意の serialized `Component`
- 対応 property type:
  - `bool`
  - `int / enum`
  - `float`
  - `string`
  - `Vector3`
  - `Color`

### 使い方

1. `Transform` など、分かりやすい serialized field を持つ component を GameObject に付ける
2. 同じ GameObject に `SerializedPropertyOverrideSampleMarker` を付ける
3. `Quick Select Component` で同じ GameObject 上の component を選ぶ
4. `Quick Select Property` で対応 field を選ぶ
   - もしくは `Property Path` を手入力する
   - 例: `m_LocalScale`
5. value kind と値を設定する
6. world build を実行するか Play を開始する

### 期待結果

- 対象 component の値は現在の Build / Play session 中だけ変わる
- source scene の serialized value は終了後も元のまま
- cloned asset を作らずに scene object を非破壊編集する例になっている

## Proxy Collider Sample

- Runtime marker: `ProxyCollider/ProxyColliderSampleMarker.cs`
- Editor pass: `Editor/ProxyCollider/ProxyColliderSamplePass.cs`
- 対象 object: `MeshFilter` または `Renderer` を持つ GameObject
- 生成 object: `BoxCollider` または `SphereCollider` を持つ child GameObject

### 使い方

1. cube など visible object を scene に置く
2. その object に `ProxyColliderSampleMarker` を付ける
3. `Box` か `Sphere` を選ぶ
4. 必要なら `Padding` を調整する
5. 必要なら `Disable Existing Colliders` を有効にする
6. world build を実行するか Play を開始する

### 期待結果

- `Proxy Collider (Generated)` のような child object が session 中だけ生成される
- 既存 collider は session 中だけ無効化できる
- source scene の collider 構成は終了後に元へ戻る

## Prefab Proxy Sample

- Runtime marker: `PrefabProxy/PrefabProxySampleMarker.cs`
- Editor pass: `Editor/PrefabProxy/PrefabProxySamplePass.cs`
- 対象 object: marker を持つ GameObject
- 必要 asset: 任意の prefab asset

### 使い方

1. cube など visible object を scene に置く
2. proxy として使いたい prefab asset を作るか選ぶ
3. source object に `PrefabProxySampleMarker` を付ける
4. prefab asset を設定する
5. proxy を source object の子にするかを選ぶ
6. `Source Handling` を設定する
   - `Keep`
   - `Disable`
   - `Destroy`
7. `Parent Under Marker` を有効にする場合は `Source Handling` を `Keep` にする
8. world build を実行するか Play を開始する

### 期待結果

- `Prefab Proxy (Generated)` のような prefab instance が session 中だけ生成される
- source object は設定に応じて、その session 中だけ保持 / 無効化 / 破棄される
- source scene と prefab asset は変化しない

## Mesh Combine Sample

- Runtime marker: `MeshCombine/MeshCombineSampleMarker.cs`
- Editor pass: `Editor/MeshCombine/MeshCombineSamplePass.cs`
- 対象 root: 2 つ以上の `MeshFilter + MeshRenderer` 子を持つ parent object

### 使い方

1. `MeshRenderer` を持つ子 mesh object を 2 つ以上ぶら下げた parent object を作る
2. parent に `MeshCombineSampleMarker` を付ける
3. 必要なら `Disable Source Renderers` を有効にする
4. 必要なら `Add Mesh Collider` を有効にする
5. world build を実行するか Play を開始する

### 期待結果

- `Combined Mesh (Generated)` のような child object が session 中だけ生成される
- generated object に combined mesh と collected material list が設定される
- `Disable Source Renderers` 有効時は source renderer が session 中だけ非表示になる
- source mesh と source scene は変化しない

## Hierarchy Mesh Combine Sample

- Runtime marker: `HierarchyMeshCombine/HierarchyMeshCombineSampleMarker.cs`
- Editor pass: `Editor/HierarchyMeshCombine/HierarchyMeshCombineSamplePass.cs`
- 対象 root: marker を持つ GameObject
- 想定構成: 自身に mesh が無い Empty parent でもよい

### 使い方

1. Empty parent object を作る
2. その子や孫に mesh object を配置する
3. Empty parent に `HierarchyMeshCombineSampleMarker` を付ける
4. 必要なら `Destroy Source Objects` を有効にする
5. `Destroy Source Objects` を無効にする場合は、必要に応じて `Disable Source Renderers` を使う
6. 必要なら `Add Mesh Collider` を有効にする
7. world build を実行するか Play を開始する

### 期待結果

- marker root 自身には `MeshFilter` / `MeshRenderer` が無くてもよい
- 結合対象は子孫 mesh のみで、marker root 自身の mesh は無視される
- session 中だけ marker root に combined `MeshFilter` / `MeshRenderer` が設定される
- `Destroy Source Objects` 有効時は source descendant object が session scene からのみ除去される
- source hierarchy と source mesh は終了後に元へ戻る