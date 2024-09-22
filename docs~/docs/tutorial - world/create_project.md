---
sidebar_position: 3
---

# プロジェクトへのFDMiの追加

本章では、プロジェクトへのFDMiの追加方法、並びにWorldへの初期設定方法を説明します。

:::warning

FDMiを利用した飛行機ギミックの導入では、以下の操作が必須となります。

:::

## 前提条件

- Unity 2022F3.22以上がインストールされていること
- ALCOM [https://vrc-get.anatawa12.com/alcom](https://vrc-get.anatawa12.com/alcom), もしくはVRChat Creator Companionがインストールされていること
- `VRChat SDK - World`　の最新版がプロジェクトにインストールされていること


## 1.ALCOM/VRChat Creator Companionへの追加

[https://vpm.gyoku.tech/redirect](https://vpm.gyoku.tech/redirect) より、お手持ちのALCOM/Creator CompanionにFDMiをインストールしてください。

うまくいかなかったときは、ALCOMの「パッケージ管理」->「VPMリポジトリを追加」をクリックし、リポジトリ情報に `https://vpm.gyoku.tech/vpm.json`を追加してください。

## 2. プロジェクトへの追加

もし新規プロジェクトの場合、Worldのプロジェクトを作成してください。
ALCOMのプロジェクトから、「管理」を選択し　`FDMi - Example` にチェックを入れたのち、「選択したものをインストール」を選択してください。

:::warning

特段の事情なき場合、最新のFDMiを使用してください。

FDMi 2.1.0以前では、`FDMi - Example` に加え、以下にもチェックを入れてください

- FDMi - Core
- FDMi - Avionics
- FDMi - Aerodynamics
- FDMi - Dynamics
- FDMi - Input
- FDMi - Sync

更に、FDMi 2.0.1以前では、上記に加え以下にもチェックを入れてください。
(※FDMi 2.1.0以降では**導入しないでください**)
- FDMi - editor

:::


## 3. Unityの起動

ALCOMの「Unityを開く」より、Unityを起動してください。
