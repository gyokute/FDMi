---
sidebar_position: 1
---

# Sync - Overview

旧 `FDMi.Sync`（平面 + km-split）の V3 復活。世界座標は WGS84 の ECEF オフセット、同期値は `int`/`short` の量子化整数のみ。

## 座標系

- `FDMiEcefOrigin` が Unity 原点の測地緯度・経度・楕円体高と、ベイク済み ECEF 原点 + ENU 基底（east/north/up）を持つ。
- Unity `+X=east, +Y=up, +Z=north` を原点周りの ECEF オフセット（m）に変換する。緯度経度では管理しない（極の特異点回避）。
- `GeodeticHeightM()` は up 基底への射影で高度を返す。計器表示はここから。

## 同期値（1 behaviour = 40 bytes）

| 値 | 型 | 刻み | 範囲 |
|---|---|---|---|
| 位置 XYZ | `int` x3 | 1 cm | 軸あたり約 ±21,474 km（地球直径の約 3.4 倍） |
| 姿勢 XYZW | `short` x4 | 360/65535 度 | 正規化済み quaternion |
| 速度 XYZ | `short` x3 | 1 cm/s | ±327 m/s |
| revision | `int` | 1 | 再適用ガード |

`Float`/`Vector3`/`Quaternion` の `[UdonSynced]` は使わない。`Float` 精度では遠距離で量子化誤差が機体寸法を超えるため。

## 振る舞い

- オーナーが `target` の姿勢を cm セルに量子化し、`updateInterval`（既定 0.1 s）+ アイドル抑制 + `IsClogged` ガードで送る。
- 非オーナーは速度で外挿し、通常は `Lerp/Slerp` で追従、再受信直後のみスナップする。
- `TeleportEcef()` は所有権取得 → `target` 配置 → 2 フレームの補間凍結 → 強制送信を 1 呼で行う。テレポート失敗（所有権なし送信・再配置なし送信）を構造的に起こさない。
- フリーズ対策は補間凍結のみ。旧 `stopUpdate` のような Rigidbody kinematic 化はしない。所有権移動時は `OnOwnershipTransferred` で即時再送信する。

## 座席（SyncedStation 廃止）

- `FDMiSeatAnchor` は `VRCStation` と同じ GameObject に置く `NoVariableSync` の座席。`vehicleSync` の所有権を取り、即時送信してから `UseAttachedStation()` する。
- 着席中は `stationEnterPlayerLocation` を機体ローカルの `seatOffsetM` に毎フレーム追従させる。`FDMiRelativeSyncStation` / `FDMiRelativeSyncStationManager` は不要。
- `FDMiPlayerAnchor` は非着席時のみローカルアバターを追い、着席中は送信しない。プレイヤー位置と角度は `GetPosition`/`GetRotation` をそのまま量子化する。

## Sacc 対応（変化点最小の 1 案）

- `FDMiSaccBridge` を SaccEntity と同 GameObject に置き、`ExtensionUdonBehaviours` の末尾に自己登録する。Sacc 本体への変更はゼロ。
- 検討した他案：Sacc 側に `SendEventToExtensions` 受信専用スクリプトを差す案（Sacc 改変あり）、`SaccEntity` を継承で置換する案（プレハブ全改修）。いずれも変化点が大きく不採用。
- ブリッジは `SFEXT_O_PilotEnter` / `SFEXT_O_TakeOwnership` / `SFEXT_G_RespawnButton` / `SFEXT_O_PilotExit` で強制再送信する。Sacc の `ShouldTeleport`（`FlagDiscontinuity`）と競合しないよう、FDMi 側の再配置は `TeleportEcef()` に一本化する。

## Editor

- `FDMi/Sync/Resolve Sync Wiring`、ExitingEditMode、ビルド要求時（order 1）に配線を解決する。`origin` 未設定はシーン先頭の `FDMiEcefOrigin`、`target` 未設定は自 `transform`、`seatView` 未設定は `stationEnterPlayerLocation`。
- `FDMiEcefOriginEditor` の Bake が ECEF 原点と ENU 基底を書き込む。`Start()` の遅延 Bake は保険で、原則 Editor ベイク済みを使う。
