---
sidebar_position: 4
---

# ワールド(Scene)作成方法

本章では、ワールドの作成方法を説明します。

:::warning

FDMiを利用した飛行機ギミックの導入では、以下の操作が必須となります。
サンプル例として、`FDMi - Example/Scene/FDMiTempleteScene`を用意しています。こちらをコピーし、ワールド制作を開始すると、以下の設定がすでになされた状態で作業を開始できます。

:::

## 概要

本章では、以下の設定を行い、FDMiを使用可能なワールドを作成します。

- FDMi stack(ワールド全体設定)
  - 入力
  - 大気/風
- RelativeSyncManager(同期設定)
  - リスポーン位置
- 地面/空の設定
  - FDMiRelativeGroundSync(地面)
  - FDMiRelativeSkybox(空)
- 航空機の追加

## FDMi stack(ワールド全体設定)

FDMi stackは、機体間で共通する設定を管理します。
ワールドに`FDMi - example/Prefabs/FDMi stack.prefab`を配置するだけで設定完了します。

### 入力

`FDMi - input`で用いられる、指先に触れたときの入力系を配置します。
左右の人差し指に追従し、コントローラーの入力を監視します。
基本的には`FDMi stack`を配置するだけで完了します。

### 大気/風
`FDMi stack/Environment`下では、大気・風の`FDMi Float`を定義しています。
これは、各機体の空力算出に使用されます。
※　今後のアップデートで仔細が変更される可能性があります。バージョンアップ時にお伝えします。

- Ground: 海抜0m地点での大気設定
  - GroundPressure: 気圧
  - GroundTemperature: 外気温
- Wind: 海抜0m地点での風。現在はSaccflight互換の設定にしてあります。

## RelativeSyncManager(同期設定)

RelativeSyncManagerは、機体・人・地面全ての位置関係をオーバーライドし、自分・自機が常にワールドの原点付近に来るよう、毎フレーム調整します。
`FDMi - sync/Prefabs/RelativeSyncManager.prefab`を導入し、上部メニュー`FDMi/Setup FDMi`を押します。
**このとき、RelativeSyncManagerは以下の位置に配置してください**
- Sceneの一番親（`RelativeSyncManager`に親がない状態）
- ワールド座標の原点(0,0,0)

### リスポーン位置

次に、ワールド・機体毎のリスポーン位置を指定します。  
FDMiでは、ワールド・機内で、それぞれリスポーン位置をオーバーライドします。
`RelativeSyncManager`及び各機体の`FDMiRelativeObjectSync`の、`Respaun Point`に、各位置でのリスポーン位置を指定します。

※　**機体の`Respaun Point`は、機体の`Rigidbody`からの相対位置で指定されます。**
※　**`Respaun Point`を付けない場合、ワールド原点/`Rigidbody`(0,0,0)の位置にリスポーンします。**

## 地面/空の設定

FDMiでは、地面・空を回転させ、ワールド座標のFloat制約を解決しています。
そのため、地面・空にそれぞれ設定が必要です。

### FDMiRelativeGroundSync(地面)

地面のメッシュをある程度まとめたGameObjectを作り、`FDMiRelativeGroundSync`のスクリプトを適用します。
上部メニュー`FDMi/Setup FDMi`を押せば、自動で設定されます。

※ `FDMiRelativeGroundSync`配置位置に制約はありません。
※`FDMiRelativeGroundSync`は、1シーンに複数個存在してかまいません。
※ `FDMiRelativeGroundSync`の`RepawnPoint`設定は無効です。

### FDMiRelativeSkybox(空)

FDMiでは、空を回転させるため、`FDMi - Example/Prefabs/FDMiSkybox`を使用します。
当該オブジェクトをScene内に配置したうえで、以下の設定をします。
最後に上部メニュー`FDMi/Setup FDMi`を押せば、自動で設定されます。

※ `FDMiRelativeSkybox`配置位置に制約はありません。

- `FDMiSkybox/uvSphere`のマテリアルを、使用したいSkyboxに差し替えます。
- `FDMiSkybox/Directional Light`の設定を調整します。
- `Main Camera`(ワールド設定用カメラ)の設定を調整します。
  - `Clear Flags`を`Solid Color`にします。
  - `Clipping Planes`を`FDMiSkybox/uvSphere`の２倍以上のサイズに設定します。

## 航空機の追加

各機体の設定がなされたprefabを**シーンのルート**(親がない状態)に配置します。  
この状態で、上部メニュー`FDMi/Setup FDMi`を押せば、自動で設定されます。
