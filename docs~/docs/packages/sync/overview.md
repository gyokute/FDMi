---
sidebar_position: 1
---

# Sync - Overview

V2(`FDMi-2022`)の「km-carry + 毎フレーム原点解決」を ECEF フレームへ移植した系。世界の絶対座標は WGS84 ECEF オフセット、同期値は粗密2段の int + short 量子化。クラス名は `FDMiGeodetic*`(ブランド名 GeodeticSync)。

## 座標系

- `FDMiEcefOrigin` が Unity 原点の測地緯度・経度・楕円体高と、ベイク済み ECEF 原点 + ENU 基底(east/north/up)を持つ。
- 論理位置 = `MulChunk(chunk) + position`。`chunk` は **int のチャンクカウンタ**、`position` は 1 chunk 以内のセル内成分(ECEF, m)で、同期時は **1 cm 単位の short** に量子化される。
  - **1 chunk = 細部 short の全範囲(count 65536 個) = 655.36 m**(`FineUnitM = 0.01f`、`ChunkM = FineUnitM * 65536f`)。0.5 cm 単位にする場合は `FineUnitM` を `0.005f` にするだけ。
  - `chunk` は int なので 2^31 chunk ≈ 1.4×10^12 m ≈ 9.4 AU まで表現できる(月 3.84×10^8 m は余裕)。
  - セル内成分は 1 cm 格子に乗る(`setPosition` が量子化と繰り上げを行う)。受信側も同じ格子を再現する。
- 論理位置は **transform 階層に依存しない**(`_parentIndex` は階層を決めるだけで、位置計算には入らない)。ワールドの ECEF 原点 = Unity 原点。
- Unity フレームへの写像は `FDMiGeodeticSyncManager` が**毎フレーム**「フレームのセルを原点として」解決する。フレームのセルは非着席時はローカルプレイヤーのセル、着席中(root 切替後)は **root 車両のセル**。ENU 基底はセルが変わった時にのみ `ENUAt(offsetM)` で再計算(測地緯度は Bowring 逆算・double)。`ENUAt` へは `MulChunk(chunk)` を渡し、×655.36 は double 経由で行うのでチャンク差の精度を落とさない。
- 同期値の分解能は原点からの距離に依存せず 1 cm のまま(chunk と細部を分けたまま扱い、絶対位置を float で実体化しない)。**描画側の上限は Unity float32 の座標精度**で決まる(原点から 100 km で約 8 mm、1000 km で約 6 cm)。
- 適用範囲: 本系は原点接平面(flat-earth)近似。ENU 基底はフレームのセルでしか更新しないため、長距離では実緯度との差(曲率誤差)が残る。数十 km 規模のワールド/飛行を想定する。

## 同期値(車両1 behaviour)

| 値 | 型 | 意味 |
|---|---|---|
| syncedChunkX/Y/Z | `int` x3 | チャンクカウンタ(1 chunk = 655.36 m) |
| syncedFineX/Y/Z | `short` x3 | セル内成分(1 cm 格子。short 全域 = 1 chunk) |
| syncedRot | `short` x4 | 15bit 成分の quaternion(受信側で正規化)。Quaternion 型同期(16 B)の半分 |
| syncedVel | `Vector3` | m/s(ECEF) |
| ownerSyncedTime | `double` | 外挿用送信時刻 |
| _parentIndex | `int` | 参照点親(母艦等)。**階層のみ**を決め位置には影響しない |

定常 payload は **50 B**。`TrySerialize` は interval(既定 0.25 s)+ **1 cm 量子化値**での lastSent 比較(静止機・姿勢不変は送らない)+ `IsClogged` ガード。送信状態は実際に送った後でコミットする。約 200 B/s(移動時)。

## 振る舞い

- オーナー: `target` の Unity 座標を `ToEcef` で回転させ、**フレームのセルを基準に**細部へ畳む。絶対位置を float で作らないので、遠距離でも細部は 1 cm を保つ。
- 非オーナー: `ExtrapolationAndSmoothing` が速度+加速度+ping 補正で外挿し、`Update` で自分のフレームへ解決して描画する。ping は `[0, MaxPingExtrapolationInSeconds]` にクランプする(負の外挿=過去方向への飛びをしない)。速度が送られていない場合は位置と時刻の差分から推定する。
- プレイヤーの Unity 座標は ±1 chunk 以内に保たれ、チャンク通過のたびに `TeleportTo` で巻き戻す。世界はフレームのセルが動くことで再錨定する(rebase)。

## root 切替(車体の原点固定)

**root 対象を毎フレーム固定し、他のすべてをそれに対する相対で毎フレーム動かす**のが本パッケージの中核(V2 と同じ制御を、ECEF の論理座標管理の上で行う)。

- 発火: `FDMiSeatAnchor.OnStationEntered`(ローカル) → `Manager.changeRootRefPoint(vehicleSync)`、`OnStationExited` → `changeRootRefPoint(Manager)`。
- フレームの定義(`FDMiGeodeticSyncManager`):
  - **原点** = root の論理位置そのもの(着席中 = 車体の chunk + セル内成分、非着席時 = プレイヤーのセル)。
  - **姿勢** = `FrameRotationEcef`(着席中 = 車体の論理姿勢 `_rotation`、非着席時 = フレームセルの ENU 基底)。
  - 描画は `position = ToUnity(論理位置 − 原点)`、`rotation = ToUnityRot(論理姿勢)`。**root 自身は必ず原点・無回転**になる。
- root 車体の物理(`FDMiGeodeticObjectSync.PinRootBodyToFrame`)は毎フレーム `body.position = 0` / `body.rotation = identity` に戻し、物理が動かした分を**位置と回転の両方**論理姿勢へ累算する。velocity は `Inverse(br)` で回して、世界の回転分を速度の向きに反映させる。
- 帰結: 機体のローカル軸 = フレーム軸 なので、重力・空力は `Inverse(FrameRotationEcef) * gravityEcef` として機体ローカルで扱える(dynamics 側の入力。同期パッケージは姿勢を提供するだけ)。機体は常に Unity 原点付近で物理が回り、遠距離でも float 精度が落ちない。
- 切替時は、原点と姿勢の変化で**見えているもの全部が同じだけずれる**。refPoints は毎フレーム `getViewPosition` から描き直されるので自動で追随する。VRChat 管理のアバターだけは手で合わせる(`PlayerPosition.rebaseFrame`: 論理位置を切替前のフレームで読み直し、ずれ分を `TeleportTo`。着席中は VRChat が座席へ置き直すので触らない)。

### ワールド側の要件

フレームは**描画に参加するものすべて**を root からの相対で置く。したがってワールドの静的構造物は次のいずれかにする。

- `FDMiGeodeticWorldAnchor`(推奨): 地面・滑走路などをこの下にまとめて置く。論理位置はシーン記述のまま固定し、`transform` だけを毎フレーム root に合わせて動かす。子は transform ごと動くので地形コライダーも一緒に動き、root 車体との衝突判定が正しいまま保たれる。
- 素の `FDMiGeodeticReferencePoint` を各構造物に付ける。
- これらを使わない(シーン記述のまま Unity に置く)場合、root 切替後にワールドが置き去りになり、車体だけが原点に固定されて見える。

## プレイヤー

- 各プレイヤーに `FDMiGeodeticPlayerPosition`(参照点)をマスターが割り当てる(`FDMiGeodeticPlayerSyncManager` + `FDMiGeodeticPlayerPositionAssigner`、整合自動修復付き)。
- 非着席時はローカルアバターを追い、チャンク通過時に `TeleportTo` で巻き戻して(約 655 m ごと)世界を再錨定する。遠隔アバターは受信値へ Lerp して描画する(チャンク跨ぎのみスナップ)。
- 着席中(`inVehicle = true`)はプレイヤー参照点を休止させ、車両が座標系の主役になる。**`inVehicle` を書くのは `FDMiSeatAnchor` だけ**(同一 GO 上の PlayerPosition 側は読むだけ)。
- root 切替の瞬間は `rebaseFrame(cellShift)` がアバターをフレームのセル差分だけ動かし(見え方は連続)、論理位置を新フレーム基準で読み直す。着席中に凍結したセルを降車後に引きずらないための処理。
- `respawnPoint` は**論理 ECEF(m)** で、既定はワールド ECEF 原点。AutoSetup は配線しない(原点リスポーン)。

## 座席(SyncedStation 廃止)

- `FDMiSeatAnchor` は `VRCStation` と同じ GameObject に置く `NoVariableSync` の座席。`Interact` で `vehicleSync` の所有権を取り、即時送信してから `UseAttachedStation()` する。**所有権取得はこの経路と Sacc の O イベントだけ**(同期の送信関数は SetOwner しない)。
- 着席中は `stationEnterPlayerLocation` を機体ローカルの `seatOffsetM` に毎フレーム追従させる(着席者のクライアントのみ)。遠隔側は追従しないため、`seatView` は機体の子に置き `localPosition` を `seatOffsetM` と一致させること。
- `headingSource` が空なら機体ルートの姿勢を向きに使う。
- 着席イベントはステーションと同一 GameObject の UdonBehaviour にしか届かないため、`FDMiSeatAnchor` は必ず `VRCStation` と同居させる(`[RequireComponent]` で担保)。

## Sacc 対応(変化点最小)

- `FDMiSaccBridge` を SaccEntity と同 GameObject に置き、`ExtensionUdonBehaviours` の末尾に自己登録する。Sacc 本体への変更はゼロ。
- `EntityControl` は型名 `SaccFlightAndVehicles.SaccEntity` で解決する(Editor の AutoSetup と同じ探索)。解決できない場合は警告を出す(無言 no-op をしない)。
- ブリッジは `SFEXT_O_PilotEnter` / `SFEXT_O_TakeOwnership` / `SFEXT_G_RespawnButton` / `SFEXT_O_PilotExit` で強制再送する。`SFEXT_G_*` は全クライアントへ配送されるが、`CaptureAndSend` は非オーナーでは何もしないので所有権は奪われない。
- `GetProgramVariable` の戻りを `(UdonSharpBehaviour[])` で直接 cast する。`as` 演算子は UdonSharp binder が NullReferenceException で落ちる(SDK 3.10.5 時点、本修正で解消)。
- オーナー側の周期 capture は `FDMiGeodeticObjectSync.Update` に一本化した(旧 Bridge `Update` の二重送信は廃止)。

## Editor

- `FDMi/Sync/Resolve Sync Wiring`、ExitingEditMode、ビルド要求時(order 1)に配線を解決する。配線対象: `FDMiEcefOrigin`(Bake)、`FDMiGeodeticSyncManager`(origin + refPoints 一括登録 + 自己 syncManager)、`FDMiGeodeticObjectSync`(syncManager/body/target)、`FDMiGeodeticPlayerPosition`(syncManager/station)、`FDMiGeodeticPlayerPositionAssigner`(playerSyncMan/playerPosition)、`FDMiSeatAnchor`(vehicleSync/syncManager/seatView)、`FDMiSaccBridge`(vehicleSync/EntityControl)。
- `FDMiGeodeticSyncManager` は **refPoints に自身を含む**必要がある(Editor の再収集が常に含める)。自身の `index` は配列内の位置として Start で確定する。
- シーン側に必要なもの: `FDMiEcefOrigin`、`FDMiGeodeticSyncManager`(+ 同 GO に `FDMiGeodeticPlayerSyncManager`)、プレイヤー数分の assigner 子(各 GO に `FDMiGeodeticPlayerPosition` + `VRCStation`)、車両ごとの `FDMiGeodeticObjectSync`、**ワールドの静的構造物を載せた `FDMiGeodeticWorldAnchor`**(地面・滑走路など。root 切替で車体を原点に固定するために必須)。
- `FDMiEcefOriginEditor` の Bake が ECEF 原点と ENU 基底を書き込む。`Start()` の遅延 Bake は保険で、原則 Editor ベイク済みを使う。
