---
sidebar_position: 2
---

# 機体 全体設定

:::warning

この節を始める前に、（[ワールドの設定](/tutorial%20-%20world/overview)）を必ず終わらせてください。

:::

## 1.3Dモデルの配置

まず、機体の3Dモデルを配置します。
このとき、機体の脚が静止時の状態(タイヤがすべて水平な地面に接地した状態)を作成してください。

次に、プロジェクト内の `Packages/FDMi - Example/Prefabs` にある Prefab `PlaneTemplate.FDMi`を、モデルの子に入れます。
そのうえで
- TransformのPositionをすべて0にします
- TransformのRotationを以下のように調節します。
    - X軸（赤）を、機体の右手側
    - Y軸（緑）を、水平な地面に垂直
    - Z軸（青）を機体正面

ここで、一度 `PlaneTemplate.FDMi`を、3Dモデルの外に出します。このとき、`PlaneTemplate.FDMi`のRotationのX軸が0以外の場合、0を代入してください。
そのうえで、3Dモデルを `PlaneTemplate.FDMi/DataBus/Vehicle/Model/`の子にいれます。

ここまでできましたら、この時点で一度Prefab Variantを作成することを推奨します。
まず、`PlaneTemplate.FDMi`の名称を変更します。今回は`Early_Jet_1.FDMi`とします。
適切なフォルダ（通常 `/Assets`以下）にドラッグアンドドロップします。
このとき、選択肢が現れますので、`Original Prefab` または `Prefab Variant` を選択してください。

:::note

本章では`Prefab Variant`で設定を進めますが、基本的にはFDMiのバージョン更新による不特定な仕様変化を避けるため、`Original Prefab`をお勧めします。

:::

## 2. 同期の設定

機体の位置の同期は `Databus/Sync` が行います。これは後程でてくる`Setup FDMi`で自動設定されますので、放っておいてください。  

:::info

FDMiにおける同期の仕組みを簡単に説明します。（読み飛ばしてかまいません）

- 飛行機の外にいるときは、普通のVRChatワールドとほぼ同じ感じです。
- 飛行機の中に入る（もしくは飛行機の椅子に座る）と、**Unity上の原点に飛行機を固定**し、**地面が動く**ようになります。
- 飛行機の外に出ると、元の状態に戻ります。

これらの”原点の管理”は `Databus/Sync`で行われています。
FDMiは、`DataBus/Sync`を含む`FDMiReferencePoint`が、Transformを動的に組み替え、原点の位置を常にLocalPlayerの近くに移動させます。
これにより、地球程度の大きさであれば、自由に移動ができます。

:::


さて、FDMiでは同期を含む様々な挙動に **「機体内にLocalPlayerが入った・機体からLocalPlayerが出た」**判定が重要になります。
`Databus/Sync`ではこれらを行いません。`RelativeSyncPositionTrigger`を使用します。

`RelativeSyncPositionTrigger`は、今回二か所にあります。

- `Databus/Vehicle/NotInZone/EnterZone`
- `Databus/Vehicle/OnlyIsRoot/OnlyInZone/ExitZone`

それぞれについて見ていきます

### 2.1 EnterZone(機内に"入る")

さて、`Databus/Vehicle/NotInZone/EnterZone`を見ていきます。  
`PlaneTemplate.FDMi`から機体を制作している場合、まずコライダーの位置がずれているはずです。いい感じに修正してください。(Transformを動かしてかまいません。)
また、今回のような単座機では、判定は極小でかまいません。(逆に旅客機や爆撃機の場合、大き目にとると良いでしょう)

:::tip

Box ColliderのComponentの中身が見えないときは、一度ほかのGameObjectを選択後、再度`Databus/Vehicle/NotInZone/EnterZone`を選択してください。

:::

ここで、`RelativeSyncPositionTrigger`の以下の設定を確認して下さい
- "Detect Enter" ⇒ ☑ (有効)
- "Detect Exit" ⇒　☐ (無効)

次に、一つ上の階層、`Databus/Vehicle/NotInZone/`について、以下の設定を確認してください。
- `On When Disable`　 ⇒ ☑ (有効)

`PlaneTemplate.FDMi`から機体を制作している場合、`InZone`の項目は設定されていませんが、後から自動で設定されます。

:::info

`RelativeSyncPositionTrigger`は、OnTriggerEnter/OnTriggerExitにより、Box Colliderからの出入りを...

以下かく

:::

### 2.2 ExitZone


### 2.3 機内の床コライダー

ここで、`Databus/Vehicle/OnlyIsRoot/OnlyInZone/FloorCollider`をみてみます。
先程の`OnlyInZone`の機能を用い、機内だけで見える床を設定します。
※　ここで、Layerは"BoardingCollider"に設定します。

### 2.4 RelativeSyncModelTransform