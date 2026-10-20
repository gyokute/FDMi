---
sidebar_position: 2
---

# 非rootオーナー物体のフレーム合成メモ

オーナー所有かつ非rootの物体における、蓄積式と表示式のフレーム不整合の記録。断定できない箇所は「未検証」と明記した。

## 1. 事象

`FixedUpdate` の非rootオーナー分岐(`FDMiRelativeObjectSync.cs:88-110`)は `smr/smp/rbdp` を合成し `setPosition(Quaternion.Inverse(smr)*(bp-smp-rbdp))` で蓄積する。一方 `getViewPosition`(`FDMiReferencePoint.cs:180-201`)は `_position/_posMm` を絶対座標として扱い root との差分で表示を解く。蓄積側(Unity 相対)と表示側(論理絶対)の向きが異なる2式が同一 `_position` を共有している(未検証: mm・chunk 項込みの厳密な等価性は未確認)。

## 2. root == manager の代数

manager が root のとき `localRotation/localPosition` は `identity/-localTotalM`(`Manager.cs:30-37`)。root参照点に `body` は無い(未検証: 前提)ため `rbdp=0`、`smr=Identity`、`smp=-localTotalM` を代入すると蓄積式は `bp+localTotalM` となり絶対座標の読みと一致する。通常状態では不整合は表面化しない。

## 3. root == 機体の代数

root参照点が機体になると `smr=機体body回転*manager回転`、`rbdp=機体body位置` が加わり、蓄積原点は Unity 空間上の `(smp+rbdp)` になる。表示式は root の論理 `_position` 基準で差分を取るため、フレーム原点の符号が反転した形で蓄積と表示がずれる(未検証: ずれ量・向きの実測は未実施)。

## 4. 発火条件

自分のフレーム root が機体であり、かつ root でない同期物体を所有する場合のみ。例: 他人の機体に同乗(root 切替済み)しながら別物体を所有する場合。通常の単独搭乗・単独所有では起きない(未検証: 実機再現は未実施)。

## 5. 遠隔クライアントへの見え方

ずれた `_position` は同期変数として配信されるため、遠隔では当該物体がフレーム原点に対し反転・オフセットして見えると推測される(未検証)。root 機体自体の表示には影響しない。

## 6. 修正対象外とした理由

レビュー④-3の判定による。V2(`c6b4416` の `Update` 内 `parentRefPoint` capture相当)と同等で回帰ではないこと、発火条件が狭いこと、検証に2クライアント実機が必要なこと、の3点から対象外とした。

## 7. 将来検証手順

A(機体オーナー兼搭乗者)・B(観測者)の2クライアントで確認する。

1. B から A の機体が原点・無回転に見えること。
2. A が別物体を所有して同乗したとき、B からの別物体表示に反転・オフセットが無いこと(現状未検証)。
3. 降車(root 復帰)後に別物体の表示が元に戻ること。
4. V2 ワールドと同条件で比較し差分が無いこと。
