# VRCW Non-Destructive Pipeline

VRCW Non-Destructive Pipeline は、VRChat World 向けの非破壊 Build / Play 処理基盤です。  
Build 前や Play 開始前に Editor C# の処理を差し込みつつ、元の Scene や Asset を直接変更しないことを主目的にしています。

## できること

- VRChat World の Build 前に Editor C# の処理を差し込む
- Play 開始時にも同じ Pass モデルで一時処理を適用する
- marker component と pass による拡張しやすい authoring model を提供する
- Material / Mesh / Scene object / Prefab を session 単位で非破壊に扱う
- 実行結果を execution report として保持し、必要時のみ export する

## 導入

前提:

- Unity 2022.3
- VRChat Worlds SDK が導入済みであること

WNDP は普通の Unity Package Manager から Git URL で導入できます。  

### Unity Package Manager から追加する

1. Unity で `Window/Package Manager` を開く
2. 左上の `+` を押す
3. `Add package from git URL...` を選ぶ
4. 次の URL を入力する

```text
https://github.com/watapj/WNDP.git?path=Assets/WataOfuton/WNDP#main
```

### `manifest.json` に直接書く場合

```json
{
  "dependencies": {
    "com.wataofuton.wndp": "https://github.com/watapj/WNDP.git?path=Assets/WataOfuton/WNDP#main"
  }
}
```

## ドキュメント

- marker / pass を自作する人向け: [Docs/UserGuide.md](Assets/WataOfuton/WNDP/Docs/UserGuide.md)
- サンプル一覧: [Samples/README.md](Assets/WataOfuton/WNDP/Samples/README.md)

## 補足

- このプロジェクトは、[NDMF](https://github.com/bdunderscore/ndmf) の Non-Destructive Framework 的な考え方に着想を得ています。  
- ただし対象はアバターではなく VRChat World であり、独立した実装です。  
- 現状は experimental 段階です。  
- コアの Build / Play session、pass 基盤、samples、report export は揃っていますが、公開用 package としては今後も整理を続ける前提です。

## ライセンス

このプロジェクトは MIT License で公開します。詳細は [LICENSE](Assets/WataOfuton/WNDP/LICENSE) を参照してください。
