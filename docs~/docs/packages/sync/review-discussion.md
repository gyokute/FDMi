# FDMi 同期パッケージ仕様レビュー 三者突き合わせ・AI議論記録

## 0. 対象と方法

- 対象: `FDMi.sync` の設計仕様（`overview.md` および仕様中に提示されたコード）。
- レビュアー: GLM 5.3（15件 / Udon・同期視点）、Deepseek v4.1 flash（7件 / 座標・運動学視点）、Longcat 2.5 free（7件 / Sacc・座席視点）。計29指摘。
- 統合方針: 同一根因は1論点に統合（29→19論点）。
- 事実確認: VRChat 公式ドキュメント、UdonSharp 公式、SaccFlightAndVehicles 実コード（`Packages/io.github.sacchan-vrc.sacc-flight-and-vehicles/Scripts`）、FDMi core `FDMiDataSync.cs` を直接参照。
- 制約: 被レビュー実装（`FDMiEcefSync.cs` 等）は現ワークツリーに存在しない（sync パッケージは asmdef/asset のみ）。したがって**実装の細部は各レビューのコード引用を一次情報として扱い、VRChat/Sacc 側の事実のみ実コード・公式文書で検証**した。コード引用が食い違う箇所は「要検証」とした。

検証済みの外部事実（本記録の判定根拠）:

- VRChat: 「Only the **owner** of a networked object can modify its synchronized Udon variables.」 また `Networking.SetOwner` は**非所有者からでも所有権を移せる**（公式例は `Interact` でローカルプレイヤーが所有権を取得）。Sacc は `OnOwnershipRequest` を実装しておらず、奪取を拒否しない。
  - 出典: creators.vrchat.com `worlds/udon/networking/ownership`（要約）
- PlayerObject: 「players own a PlayerObject, they also own all GameObjects with synced UdonBehaviours within their PlayerObject. **They cannot transfer them to anyone else.**」「In the `Start` and `OnDeserialization` events, ownership is guaranteed to be correct.」
  - 出典: creator-docs `worlds/udon/persistence/player-object.md`
- Sacc リスポーン配送: `SaccEntity.EntityRespawn` → `Networking.SetOwner(localPlayer, gameObject)`（`SaccEntity.cs:1284`）→ `SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SendRespawn))`（`:1303`）→ `SendRespawn() { SendEventToExtensions("SFEXT_G_RespawnButton"); }`（`:1307`）。**全クライアントに配信される**。
- Sacc 側の同イベントは IsOwner ガードが通例: `DFUNC_Gun.cs:211 if (IsOwner)`、`DFUNC_Flaps.cs:104 if (!EntityControl.IsOwner) return;`、`SAV_SyncScript.cs:277 if (IsOwner)`。
- 所有権移譲は車両ルートで Sacc 拡張全体に波及: `SaccEntity.cs:713-750 OnOwnershipTransferred` → `SFEXT_O_TakeOwnership` / `SFEXT_O_LoseOwnership`（`SAV_SyncScript.cs:208,212` がシミュ権を切替）。
- core の同種処理 `FDMiDataSync.TrySerialize()` は「先に SetOwner → 直列化」「IsClogged 時は `SendCustomEventDelayedSeconds` で再試行」。
- ステーションイベントの同一 GO 要件: UdonSharp 公式「All you need for a chair is a **GameObject** with: (1) VRC_Station … (3) an UdonBehaviour with an Udon program that handles sitting」＝ `OnStationEntered/Exited` は **VRC_Station と同一 GameObject 上の UdonBehaviour にのみ届く**。

---

## 1. 論点別の議論

### T1. SFEXT_G_RespawnButton で非オーナーが所有権を奪い合う【3モデル一致・最重要】

- (a) 指摘: Sacc のリスポーンは全クライアントに `SFEXT_G_RespawnButton` を配送する。ブリッジがこれを受けて `vehicleSync.CaptureAndSend(true)` を呼び、`CaptureAndSend` 先頭の `Networking.SetOwner(localPlayer, gameObject)` が走るため、**リスポーン毎に非オーナー全員が車両ルートの所有権を奪い合う**。所有権は `SaccEntity` のシミュ権と直結し、操縦者以外へ移ると操縦不能・機体挙動破綻を招く。
- (b) 指摘モデル: GLM（G9, P2, conf0.65）、Deepseek（D1, **P0**, conf0.9）、Longcat（L1, P1, conf0.85）。
- (c) 議論:
  - 事実確認1: 「非オーナーの `SetOwner` は失敗する」という GLM の留保は**誤り**。VRChat 公式の所有権取得例は非所有者からの `SetOwner` であり、Sacc は `OnOwnershipRequest` で拒否しない。よって奪取は実際に成立する。
  - 事実確認2: 配送は全クライアント（`SaccEntity.cs:1303/1307`）、Sacc 自身の同名ハンドラは全て IsOwner ガード。無ガードは Sacc の慣習から外れる（Deepseek の指摘は根拠が強い）。
  - 事実確認3: 車両ルートの所有権移譲は `SFEXT_O_LoseOwnership`/`TakeOwnership` を介して SAV の物理権を奪う。ゆえに「所有権競合」は表示だけの問題ではなく**操作権の喪失**であり、P0〜P1 が妥当。
  - 反論（留保）: 前提として「FDMiEcefSync の `gameObject` が SaccEntity と同一ルート」である必要がある。子 GO に置かれれば影響は当該子の同期変数に限定される。3レビューとも同一ルート前提だが、これは**要検証**（配線前提の確認）。
  - 修正案の是非（本論点の核心）:
    - 案A「IsOwner ガード」: **妥当かつ十分**。リスポーンは元々 `SaccEntity` が押下者を新オーナーに設定してから全配送するため、直後の強制再送は**新オーナーが実行するのが正しい**。非オーナーの `RequestSerialization` は VRChat 仕様上 no-op（所有権が無いと同期変数を書けない）であり、非オーナーが送る意味が無い。
    - 案B「SetOwner を伴わない再送経路」: 方向性は正しい（`SetOwner` を送信関数に混ぜない）が、**単独では不十分**。非オーナーは直列化できないため、結局は「オーナーのみが送る」＝案A と等価になる。より良いのは **`CaptureAndSend` から `SetOwner` を除去**し、所有権取得を `SFEXT_O_PilotEnter` / リスポーン / `OnOwnershipTransferred` 等の意味的イベントに限定する設計（Sacc と同じ責務分離）。これで T5 の PlayerAnchor 問題も同時に解消する。
    - 共通の根因は「`CaptureAndSend` に隠れた暗黙の `SetOwner`」であり、呼び出し側にガードを足す対症療法より、**所有権取得を明示イベントへ移す**のが本質的。
- (d) 結論: **採用（P0）**。最低限 `if (!vehicleSync || !Networking.IsOwner(vehicleSync.gameObject)) return;` のオーナーガード。望ましくは `CaptureAndSend` の暗黙 SetOwner を撤去し、所有権取得を専用イベントへ集約。同一ルート前提の確認は要検証。

### T2. 速度外挿の dt が常に 0（外挿がデッドコード）【2モデル一致】

- (a) 指摘: `OnDeserialization` が `lastRemoteRevisionTime` を「今回の受信時刻」に更新した状態で `ApplyReceived` が呼ばれるため、`dt = (serverTimeDiff + Time.time) - lastRemoteRevisionTime` が厳密に 0 となり、`smoothPosition += velocity * dt` が一度も実行されない。仕様「非オーナーは速度で外挿」と不一致で、short×3 の速度は完全な無駄データ。
- (b) 指摘モデル: GLM（G6, P2, conf0.95）、Deepseek（D2, P1, conf0.9）。
- (c) 議論:
  - 両者とも結論は一致するが、**コード引用が食い違う**。GLM は「115行で設定してから ApplyReceived」（＝dt=0 になる順序）と明記。Deepseek の引用は `ApplyReceived(); lastRemoteRevisionTime = ...;`（＝前回時刻が残るため dt は前回からの経過時間となり**外挿が動く**順序）。Deepseek の引用は自らの結論と矛盾する。→ **実際の行順は要検証**。
  - ただし、どちらの想定でも「修正方向（順序入替）」は一致して妥当: `dt` は**前回受信時刻に対して**測る必要がある。すなわち `lastRemoteRevisionTime` を `ApplyReceived` の**後**に更新する（または前回値を退避して渡す）。この修正は副作用が小さく、低速時に外挿が暴れないよう dt の上限クランプ（例 `Mathf.Min(dt, 0.5f)`）を併用するのが安全。
  - 補足: 有効化しても外挿は `updateInterval` 間隔の補間のみを改善する。回転は角速度未送信のため依然遅れる（→ T8 と関連）。よって「外挿を直せば同期品質が大きく改善」は過大評価気味で、改善幅は中程度（100 m/s でラグ約1時定数 ≒ 10 m 級が数 m 減る程度）。
  - 反論: 外挿は非オーナー表示専用で、機体挙動を壊さない。P0 ではなく P1〜P2 が妥当（Deepseek P1 / GLM P2）。
- (d) 結論: **採用（P1）**。実装は「前回受信時刻に対して dt を計算」＋ dt クランプ。行順の実物確認は要検証。

### T3. オーナー側で freezeLeft が減算されず補間凍結が永久化【2モデル一致】

- (a) 指摘: `Update` はオーナー時に `CaptureAndSend` 後に即 return するため、`freezeLeft` の減算〜`RestoreInterpolation` が非オーナー分岐にしか無い。`TeleportEcef` が `RigidbodyInterpolation.None` を設定したまま復元されず、リスポーン後オーナーの補間が恒久的に無効。仕様「2フレーム凍結」違反。
- (b) 指摘モデル: GLM（G3, P1, conf0.9）、Longcat（L2, P1, conf0.9）。
- (c) 議論: 機序は明確で双方一致（高信頼）。締切処理を共通メソッド化して両分岐から呼ぶか、凍結カウントダウンをオーナー分岐の前に置くのが正解。「減算をオーナー分岐にも入れる」だけでも直る。`TeleportEcef` は所有権→配置→凍結→送信を1呼に集約しており（GLM も長所として評価）、凍結解除だけが漏れた形。副作用なしの純粋修正。
- (d) 結論: **採用（P1）**。

### T4. SaccBridge が updateInterval を無視して毎フレーム送信【1モデル（+T17関連）】

- (a) 指摘: Bridge の `Update` が `followSacc` かつ所有時に毎フレーム `CaptureAndSend(false)`。`CaptureAndSend` に時間ゲートが無く、`updateInterval` 抑制は `FDMiEcefSync.Update` 側にしかないため、移動中は約90回/秒の `RequestSerialization` と `revision++` が発生し、帯域制限で IsClogged を誘発。仕様「updateInterval 既定0.1s」と矛盾。
- (b) 指摘モデル: GLM（G4, P1, conf0.85）。
- (c) 議論: 実装は仕様と明確に矛盾する（1モデルのみだが論理的に自明）。`CaptureAndSend` に時間ゲートを実装するのが一元化の観点で最善（Bridge/FDMiEcefSync 双方の呼び出しで自動的に抑制される）。`followSacc` を false に落とす経路が無い（T17）ことも二重送信を招く根因。
- (d) 結論: **採用（P1）**。`CaptureAndSend` 側に `nextSendTime` ゲートを設けるのが推奨。

### T5. FDMiPlayerAnchor の IsOwner 事前ゲートが所有権取得を妨げる【2モデル一致・要検証】

- (a) 指摘: `Update` 冒頭の `if (!Networking.IsOwner(playerSync.gameObject)) return;` により、所有権が無いと一切送信しない。しかしパッケージ内に playerSync の所有権を取得する経路が無い（唯一の `CaptureAndSend` 呼び出しがゲート後）。結果、所有者以外では無言で機能しない。
- (b) 指摘モデル: GLM（G5, P1, conf0.7）、Deepseek（D4, P2, conf0.6）。
- (c) 議論: **本論点は playerSync の実装形態に完全に依存する。**
  - ケース1（PlayerObject として `VRCPlayerObject` を使う）: VRChat は各プレイヤーにコピーを自動生成し、**各コピーは当該プレイヤーが自動所有し移譲不可**、`Start`/`OnDeserialization` で所有権が保証される。この場合 **IsOwner ゲートは正しく、指摘は不成立**。
  - ケース2（シーンに1個の素の同期オブジェクト）: 指摘は成立し、しかも単なる「送れない」より深刻。全プレイヤーが同一オブジェクトを奪い合い**自分の位置で上書きし合う**ため、ゲート撤廃（GLM案）は**かえって破綻を悪化させる**。正しいのは Deepseek 案（per-player インスタンス生成＋生成時に `SetOwner(localPlayer)`）。
  - したがって「ゲート撤廃 vs per-player 生成時 SetOwner」の論点は、**per-player 生成が正解**。ゲート撤廃は共有オブジェクト前提では誤り。core の `FDMiDataSync.TrySerialize` が「先に所有権→直列化」の順である事実は、順序の参考にはなるが、per-player 化の必要性は別問題。
  - 最善策: playerSync を **PlayerObject 化**して VRChat の自動所有権機構に乗せる。これで T5 は仕様上消滅する（本設計の目的＝各プレイヤーが自分の位置を送る、に完全一致）。
- (d) 結論: **要検証（高レバレッジ）**。playerSync が PlayerObject なら却下、素のシーンオブジェクトなら採用（P2）で修正は「per-player 生成＋SetOwner」。ゲート撤廃単独は不可。

### T6. 姿勢のみの変化が送信されない（PlayerAnchor）＋ idleMeters/idleDegrees が死んだ設定

- (a) 指摘: 位置セル3軸一致で即 return するため、静止したままの旋回（yaw 変化）が一切送信されず、遠隔アバターの方位が更新されない。また `idleMeters`/`idleDegrees` はどこからも参照されない未使用フィールド。
- (b) 指摘モデル: GLM（G8, P2, conf0.9）、Deepseek（D3, P2, conf0.85）。
- (c) 議論: 仕様は「位置と角度を GetPosition/GetRotation で量子化」としており角度も同期対象 → 明確な仕様違反。修正は `Quaternion.Angle(target.rotation, rotation) <= idleDegrees` をアイドル判定に組み込む（Deepseek の具体案は妥当）。未使用フィールドは削除か接続の二択だが、Tooltip で公開している以上**接続**が筋。
- (d) 結論: **採用（P2）**。

### T7. IsClogged でドロップした送信が二度と再送されない（lastSent が先に進む）【2モデル一致】

- (a) 指摘: `CaptureAndSend` が `lastSentX/Y/Z`（および revision）を更新してから `IsClogged` なら `RequestSerialization` をスキップ。未送信値が「送信済み」扱いになり、以降のアイドル抑制で再送されず、混雑時に減速〜停止した機体が古い姿勢で恒久固着。
- (b) 指摘モデル: GLM（G1, P1, conf0.9）、Deepseek（D6, P2, conf0.7）。
- (c) 議論: 機序は明確。修正案は2種——GLM「IsClogged 時に lastSent 更新と revision++ を取り消す」、Deepseek「lastSent 更新を IsClogged 判定の後に移す」。**Deepseek 案の方が単純で副作用が少ない**（revision のロールバックはデシリアライズ判定と絡み危険）。さらに core `FDMiDataSync` が `SendCustomEventDelayedSeconds` で再試行している前例があり、**「実際に送った後で送信状態をコミット」＋「遅延再試行」**の併用が最も堅牢。優先度は P1（同期の恒久固着は実害大）〜P2。
- (d) 結論: **採用（P1）**。lastSent/revision のコミットを送信成功後へ移動。

### T8. アイドル抑制が位置セルのみで姿勢変化を無視（車両側）

- (a) 指摘: `CaptureAndSend` のアイドル判定が位置セル差のみで、Quaternion を見ない。その場 yaw 旋回や砲塔旋回機は一切シリアライズされず、遠隔の姿勢が固定される。
- (b) 指摘モデル: GLM（G2, P1, conf0.95）。
- (c) 議論: T6 と同根（車両版）。1モデルのみだが機序は明白で、仕様「アイドル抑制」の定義に姿勢を含めるかの仕様判断を含む。位置不変・姿勢変化は航空機で頻繁に起きるため実害大。T6 と同一の修正（quat 差分判定）を車両にも適用すべき。
- (d) 結論: **採用（P1）**。

### T9. 同期済み速度が非オーナー Rigidbody に適用されず、補間が毎パケットトグルする

- (a) 指摘: 非オーナーはローカル物理が止まらず、`ApplyReceived` が毎パケット `BeginFreeze`（補間 None↔元値）を呼ぶため補間バッファが 10Hz でリセットされ、Lerp とローカル物理が transform を奪い合う。量子化 velocity を Rigidbody に適用すべき。
- (b) 指摘モデル: GLM（G11, P2, conf0.65）。
- (c) 議論: 「kinematic 化しない」は仕様の既知設計判断なので、ここは**設計判断の妥当性**の問題。velocity 適用は重力落下の発散とトグル頻度を抑える効果があり、T2 の外挿構想とも整合する。ただし非オーナー物理の扱いは設計の根幹に触れるため、実装投入前に方針決定が必要。
- (d) 結論: **要検証（P2）**。設計判断として採否を明示（velocity 適用＋凍結トグル削減を推奨）。

### T10. GeodeticHeightM が originHeightM を加算せず、曲率誤差も残る【1モデルのみ】

- (a) 指摘: `GeodeticHeightM` は `Vector3.Dot(UnityToEcefOffsetM(unityM), upEcef)` を返すため、Unity 原点で常に 0 となり原点自身の楕円体高 `originHeightM` が落ちる（例: 原点標高600 m のワールドで原点=0 m、真値600 m）。さらに原点接平面への射影なので曲率誤差 `≈ d²/(2R)` が残る（10 km で約8 m、100 km で約780 m）。
- (b) 指摘モデル: Deepseek（D5, P2, conf0.7）。
- (c) 議論:
  - **originHeightM の欠落は明白な不具合**（関数名が Geodetic Height を名乗り、仕様も計器表示に使うとする）。加算は低コストで意図に合致。→ 実装修正を採用。
  - **曲率は設計の本質的近似**。本設計は原点相対の ENU 接平面であり、同期経路自体が曲率を持たない。VRChat ワールドは通常 1 km 未満（誤差 <0.1 m）で、10 km/100 km は非現実的スケール。ゆえに**実装修正ではなく仕様への限界追記**が妥当（適用ワールド規模の上限を明記）。
- (d) 結論: **originHeightM 加算＝採用（P2）／曲率＝却下（実装修正）、代わりに仕様追記（P3）**。

### T11. EntityControl/CacheVehicle の型検査欠如で無言未登録【3モデル一致】

- (a) 指摘: Editor 側 AutoSetup は `GetUdonTypeName()=="…SaccEntity"` で型検査するのに、ランタイム `CacheVehicle` のフォールバックは `GetComponent<UdonSharpBehaviour>()`（型検査なし）。SaccEntity 以外（FDMiEcefSync 自身等）を掴むと `ExtensionUdonBehaviours` が null → `Register()` が無言 no-op → SFEXT_* 強制再送が一切効かない。
- (b) 指摘モデル: GLM（G10, P2, conf0.7）、Deepseek（D7, P2, conf0.55）、Longcat（L7, P3, conf0.55）。
- (c) 議論: 3モデル一致で、Editor 側に既に正しい型検査パターンがあるのに runtime で模倣していない点が明確な不整合。`GetComponents<UdonSharpBehaviour>()` を列挙して型名一致（Editor と同じ探索）に統一し、未解決時は**警告ログ**を出すべき（無言失敗の排除が本質）。
- (d) 結論: **採用（P2）**。

### T12. 初回デシリアライズ前に既定値(0)を適用し原点へスナップ【2モデル一致】

- (a) 指摘: 非オーナーの `Start` が最初の `OnDeserialization` 到着前に `ApplyReceived` を実行しうる。`revision=0`・`lastAppliedRevision=int.MinValue` で適用条件を満たし、cell=0・identity が適用され target が Unity ワールド原点へ一瞬（〜パケット到達まで）飛ぶ。
- (b) 指摘モデル: GLM（G7, P2, conf0.6）、Longcat（L5, P2, conf0.45）。
- (c) 議論: 適用条件（`revision > lastAppliedRevision`、初期は恒真）から機序は成立しうる。両者とも信頼度は低め（0.45〜0.6）だが、設計として「初回受信フラグで `Start` の適用をガードする」は低リスク・高効果。遠距離配置・遅延参加の多いワールドで顕著。
- (d) 結論: **採用（P2）**（`hasReceived`／`hasSmoothPose` で Start 適用をガード）。

### T13. seatView の毎フレーム追従が着席者クライアントのみ

- (a) 指摘: `Update` が `seated`（ローカルプレイヤーのみ true）でゲートされるため、`seatOffsetM` 追従と `headingSource` コピーは着席者本人のクライアントだけ。遠隔側は `stationEnterPlayerLocation` が移動されず、表示が車両親子構造次第で乖離しうる。加えて `vehicleSync` 未配線時 `Interact` が無言で return、仕様「着席中は機体の下に配置」を行うコードも無い。
- (b) 指摘モデル: GLM（G12, P2, conf0.5）。
- (c) 議論: `seatView` が機体の子で移動不要なら追従自体が不要、独立オブジェクトなら全クライアントで追従が必要。**親子構造の設計前提が未記載**であることが本質。無言 return（警告なし）は実害のある保守性問題。
- (d) 結論: **要検証（P2）**。親子構造を設計で確定し、`seatOffsetM`/`headingSource` の適用クライアントを明示。`vehicleSync` 未配線時は警告ログ。

### T14. OnStationExited が着席前の古い姿勢を一瞬強制送信

- (a) 指摘: `OnStationExited` が `seated=false` 直後に `CaptureAndSend(true)` するが、着席中は `Update` が早期 return のため target は着席前のワールド姿勢のまま。次フレームで訂正されるまでリモートへ古位置が放送される。
- (b) 指摘モデル: GLM（G13, P3, conf0.55）。
- (c) 議論: 機序は妥当だが 1〜数フレームの軽微な視覚グリッチ。降車時に現在位置で target を更新してから送信すれば解消。
- (d) 結論: **採用（P3）**。

### T15. FDMiPlayerAnchor の座席検知が同一 GameObject 要件を満たさない【Longcat 独自・真偽が焦点】

- (a) 指摘: `OnStationEntered/Exited` はステーションと同一 GameObject 上の UdonBehaviour にのみ届く。`FDMiPlayerAnchor` はこのコールバックのみで `seated` を立てるが、設置場所の要件が AutoWire・仕様に無い。要件を満たさないと `seated` が常に false となり、着席中もアバター位置を送信し続け仕様「着席中は送信しない」が破られる。
- (b) 指摘モデル: Longcat（L3, P2, conf0.55）。
- (c) 議論: **真偽検証 → 指摘は正しい**。UdonSharp 公式「椅子に必要なのは VRC_Station と Collider と、座る処理をする UdonBehaviour を載せた **1つの GameObject**」であり、イベントはその UdonBehaviour に届く。したがって「同一 GO 要件」の主張は**事実として裏付けられる**（confidence 0.55 は過小評価）。ただし 1モデルのみの指摘であり、他の2モデルが見落とした独立論点である点は注記。
  - 反論・限界: 実際に `FDMiPlayerAnchor` を VRC_Station と同一 GO に置く設計なら問題化しない。よって「要件の文書化＋Editor 側検証（同一 GO の VRC_Station 有無チェック）」が実務的な解決であり、コード自体の誤りと断定はできない。（ClientSim での挙動確認も可能）
- (d) 結論: **採用（P2）**（事実確認済）。仕様に同一 GO 要件を明記し、AutoWire で VRC_Station 同居を検証／警告。

### T16. headingSource 空時の挙動がツールチップと矛盾

- (a) 指摘: Tooltip は「Empty = keep station yaw」だが、実装は空のとき `vehicleRoot.rotation` を採用し、毎フレーム機体の向きへ上書きする。
- (b) 指摘モデル: Longcat（L4, P2, conf0.7）。
- (c) 議論: 1モデルのみだが、Tooltip と実装の不一致は明確。どちらが意図か（ステーション yaw 維持 vs 機体追従）を確定し、実装とドキュメントを揃えるべき。機体のロール/ピッチにアバターが追従するのは着席者体験として不自然なため、空なら**ステーション初期 yaw を保持**が妥当と思われる。
- (d) 結論: **採用（P2）**（実装とツールチップの整合）。

### T17. followSacc が false になる経路が存在しない（死んだフラグ）

- (a) 指摘: `followSacc` は宣言時 true、PilotEnter でも true 代入のみで false にする箇所が無い。Tooltip「Turn off while Sacc owns motion」が未実装で実質定数化し、着席中も Bridge の毎フレーム送信と FDMiEcefSync 自身の周期送信が二重に走る。
- (b) 指摘モデル: Longcat（L6, P3, conf0.8）。
- (c) 議論: T4 と密接に関連（毎フレーム送信の一因）。フィールドを削除して常時追従に一本化するか、着席中に false へ落とす実装を追加する。
- (d) 結論: **採用（P3）**。

### T18. AutoSetup の FirstOrigin が「シーン先頭」を保証しない

- (a) 指摘: `FindObjectsOfType` の先頭要素はヒエラルキー先頭を保証しない。仕様「origin 未設定はシーン先頭の FDMiEcefOrigin」と不一致で、複数原点シーンで誤配線されうる。
- (b) 指摘モデル: GLM（G14, P3, conf0.7）。
- (c) 議論: `FindObjectsOfType` の順序不定は事実。`Transform.GetSiblingIndex` でソートするか、仕様側を「最初に見つかったもの」に修正。前者が意図に忠実。
- (d) 結論: **採用（P3）**。

### T19. EditMode テストが実装を検証していない（トートロジー）

- (a) 指摘: `IdleEpsilonSuppressesSubCmJitter` は `|100000-100001|<2` を検証するだけで実装コードを参照しない。各テストは定数間の関係のみで、`UnityToEcefOffsetM↔EcefOffsetToUnityM` 往復、`PackQuaternion/UnpackQuaternion` 往復誤差、velocity の ±327 m/s クランプ等、実変換経路の数値精度が無検証。
- (b) 指摘モデル: GLM（G15, P3, conf0.8）。
- (c) 議論: **指摘は妥当**。定数の関係だけを見るテストは実装の退行を検出できない（テストの存在意義が薄い）。`EcefOffsetToUnityM` は ENU 基底の厳密転置（往復誤差ゼロ）と評価されており、ならば往復テストは高価値。純関数なら EditMode で実行可能なため実装参照テストへ置換可能。
  - 限界: 被テストコードが現ワークツリーに無く、テストの実状は仕様引用ベース。実行不能なテスト（Udon 依存）はテスト設計の工夫が必要。
- (d) 結論: **採用（P3）**。往復・誤差・クランプ境界の実装参照テストへ置換。

---

## 2. 3モデルの判定一覧

| モデル | 総合判定 | 指摘件数 | 最高優先度 | confidence | 主眼 | 特徴的な貢献 |
|---|---|---|---|---|---|---|
| GLM 5.3 | incorrect / **差し戻し** | 15 | P1（4件） | 0.85 | Udon・同期・送信ループ | IsClogged 再送喪失、姿勢無視アイドル、Bridge 毎フレーム送信、テスト同語反復を独自検出（被覆最大） |
| Deepseek v4.1 flash | incorrect / **差し戻し（P0修正後に再レビュー）** | 7 | **P0**（1件） | 0.80 | 座標・運動学 | Respawn 所有権奪取を P0 と最重量評価、GeodeticHeightM の originHeightM 欠落、core 前例（TrySerialize）との対比 |
| Longcat 2.5 free | incorrect / **差し戻し** | 7 | P1（2件） | 0.72 | Sacc・座席 | 座席イベントの同一 GO 要件、headingSource の Tooltip 矛盾、followSacc 死フラグを独自検出 |

一致状況:
- **3モデル一致**: Respawn 所有権奪取（T1）、EntityControl 型検査（T11）。
- **2モデル一致**: dt=0（T2）、freezeLeft 永久化（T3）、PlayerAnchor 所有権ゲート（T5）、姿勢のみ変化の未送信（T6）、IsClogged 再送喪失（T7）、初回原点スナップ（T12）。
- **1モデルのみ**: T4, T8, T9, T10, T13, T14, T15, T16, T17, T18, T19。
- 判定は3モデルとも「差し戻し」で一致。優先度スケールがモデル間で異なる（Deepseek は P0 を導入、GLM/Longcat は P1 止まり）ため、下表で正規化した。

---

## 3. 修正優先度リスト（P0→P3）

### P0（マージブロッカー・実害が操縦/同期を壊す）
1. **T1 Respawn 所有権奪取**（3/3）— `CaptureAndSend` 先頭の暗黙 `SetOwner` を撤去し、リスポーン再送は IsOwner ガード（＝新オーナーのみ送信）。所有権取得は意味的イベントへ集約。※同一 GO 前提を確認。

### P1（実同期を破壊。P0 修正と同時修正推奨）
2. **T3 オーナー側 freezeLeft 永久化**（2/3）— 凍結カウントダウンを共通化し両分岐から実行。
3. **T8 車両アイドル抑制が姿勢を無視**（1/3, 高信頼）— quat 差分をアイドル判定に追加。
4. **T7 IsClogged 時の再送喪失**（2/3）— lastSent/revision を送信成功後へ移動 ＋ 遅延再試行（core 前例準拠）。
5. **T2 速度外挿 dt=0**（2/3）— 前回受信時刻に対して dt を計算（＋クランプ）。実物の行順を確認。
6. **T4 Bridge の毎フレーム送信**（1/3）— `CaptureAndSend` に updateInterval ゲートを実装。

### P2（同期品質・保守性・仕様整合）
7. **T5 PlayerAnchor 所有権**（2/3, 要検証）— playerSync を PlayerObject 化（最善）／素オブジェクトなら per-player 生成＋SetOwner。ゲート撤廃単独は不可。
8. **T11 EntityControl 型検査**（3/3）— Editor と同じ型名探索に統一し、未解決時は警告。
9. **T12 初回スナップ**（2/3）— 初回受信フラグで Start 適用をガード。
10. **T6 姿勢のみ変化の未送信＋idle 死設定**（2/3）— `Quaternion.Angle` 判定を接続。
11. **T10 GeodeticHeightM に originHeightM 加算**（1/3）— 明白な不具合。
12. **T15 座席同一 GO 要件**（1/3, 事実確認済）— 仕様明記＋Editor 検証。
13. **T16 headingSource の実装/Tooltip 整合**（1/3）。
14. **T13 seatView 追従の適用範囲確定＋無言 return の警告**（1/3, 要検証）。
15. **T9 非オーナーへの velocity 適用**（1/3, 要検証）— 設計判断として採否を明示。

### P3（軽微・仕様/DX）
16. **T14 降車時の古姿勢送信**（1/3）— 降車時に現在位置で更新してから送信。
17. **T17 followSacc 死フラグ**（1/3）— 削除か着席時に false。
18. **T18 FirstOrigin**（1/3）— SiblingIndex ソート or 仕様修正。
19. **T19 テスト同語反復**（1/3）— 往復・誤差・クランプの実装参照テストへ置換。
20. **T10 曲率誤差**（1/3）— 実装修正は却下、仕様に適用スケール限界（≒1 km 未満で誤差 <0.1 m）を追記。

---

## 4. 総括

- 3モデルは**判定（差し戻し）で一致**。最も重い指摘は **Respawn 所有権奪取（T1）** で、単なる表示不具合ではなく Sacc のシミュレーション権を奪う実害。根本原因は `CaptureAndSend` に隠れた暗黙 `SetOwner` であり、これを意味的イベントへ分離すれば T1・T5 を同時に解消できる。
- 送信ループ系（T1/T2/T3/T4/T7/T8）に P0〜P1 が集中しており、骨格（ECEF オフセット＋整数量子化、Sacc 無改変の自己登録、Editor ベイク＋Start 遅延ベイク）は3モデルとも高評価。修正は局所的で設計破壊を伴わない。
- 争点となった事項の結論: ①Respawn＝IsOwner ガード採用（「SetOwner なし再送」は単独不可、根因は暗黙 SetOwner の除去）。②dt=0＝順序入替が正しい（GLM/Deepseek の引用不一致は実物確認が必要）。③PlayerAnchor＝per-player 生成（最善は PlayerObject 化）が正解、ゲート撤廃単独は不可。④GeodeticHeightM＝originHeightM 加算は実装修正、曲率は仕様追記。⑤Longcat の座席同一 GO 要件＝**真**（公式で裏付け、confidence 過小）。⑥テスト同語反復＝**妥当**（実装参照テストへ置換）。

---

## 5. 反映状況(実装側の決定と現在のコード)

本節は 2026-10-18 時点の実装(`FDMiGeodetic*` に改名後のコード)に対する反映記録。
判断の理由(Why not)は各ファイルのコメントに置いてある。

| 論点 | 状態 | 実装 |
|---|---|---|
| T1 Respawn 所有権奪取 | **採用**(実装済) | `CaptureAndSend` から暗黙 `SetOwner` を撤去し、非オーナーは no-op。所有権取得は `FDMiSeatAnchor.Interact` と Sacc の `SFEXT_O_*` のみ |
| T2 dt=0 / T3 freezeLeft | 該当なし | V2 外挿器ベースの実装に該当機構が無い(補間凍結を移植していない) |
| T4 Bridge 毎フレーム送信 | **採用**(実装済) | Bridge の `Update` を撤去し、`CaptureAndSend(false)` は `TrySerialize` の interval ゲートを通る |
| T5 PlayerAnchor 所有権 | 設計変更で解決 | PlayerObject 化ではなく assigner が per-player に割り当て、master が `SetOwner`。IsOwner ゲートは正しいまま |
| T6/T8 姿勢のみ変化・アイドル判定 | **採用**(実装済) | `TrySerialize` が 1 cm 量子化位置 + 姿勢 + 速度の量子化値を比較。静止機は送らない |
| T7 IsClogged の再送喪失 | **採用**(実装済) | lastSent は送信成功後にコミット(`IsClogged` 中は次フレーム再試行) |
| T9 非オーナーへの velocity 適用 | 却下 | 非オーナーは外挿位置を transform/body に置く。kinematic 化はしない(設計判断) |
| T10 GeodeticHeightM | 却下+削除 | 未使用だったためメソッドごと削除。曲率は overview の「適用範囲」に追記 |
| T11 EntityControl 型検査 | **採用**(実装済) | 型名 `SaccFlightAndVehicles.SaccEntity` で解決。未解決なら警告 |
| T12 初回デシリアライズ前の原点スナップ | **採用**(実装済) | `OnDeserialization` を受けるまで PlayerPosition は transform を触らない |
| T13 seatView 追従の適用範囲 | 仕様に明記 | seatView を機体の子に置き localPosition = seatOffsetM(遠隔は追従しない) |
| T14 降車時の古姿勢送信 | 該当なし | `OnStationExited` は送信しない |
| T15 座席の同一 GO 要件 | **採用**(実装済) | `[RequireComponent(typeof(VRCStation))]` + overview に明記 |
| T16 headingSource の Tooltip 矛盾 | **採用**(実装済) | 「空 = 機体ルート姿勢に従う」へ Tooltip を修正 |
| T17 followSacc 死フラグ | **採用**(実装済) | フィールドごと削除 |
| T18 FirstOrigin | 仕様で許容 | 複数原点シーンは対象外(AutoSetup は最初に見つかった 1 つを使う) |
| T19 テストの同語反復 | **採用**(実装済) | 1 cm 量子化・チャンク折り畳み・月距離・ENU 基底・短 Quat 往復の実装参照テストへ置換 |
| T20 曲率の適用範囲 | **採用**(仕様追記) | overview「適用範囲」(flat-earth 近似、数十 km 規模) |
| (新規) 位置同期の再設計 | **採用** | 粗部 int チャンク(1 chunk = 655.36 m) + 細部 short(1 cm、short 全域 = 1 chunk)。絶対位置を float で実体化しないため、距離によらず 1 cm 格子 |
| (新規) root 切替(車体の原点固定) | **採用**(実装済) | 着席で `changeRootRefPoint(vehicleSync)`、降車で Manager へ戻す。root がフレーム原点かつフレーム姿勢になり、root 自身は毎フレーム厳密に原点・無回転 |
| (新規) ピン留めは位置と回転の両方 | **V2 と同じ** | root 車体を毎フレーム原点・無回転に固定し、位置と回転を論理姿勢へ累算。機体ローカル軸 = フレーム軸になり、重力・空力は `Inverse(FrameRotationEcef) * gravityEcef` で機体ローカルに落とせる(dynamics の入力) |
| (新規) フレームは root の厳密姿勢 | **V2 と同じ** | 原点 = root の論理位置そのもの(セル量子化しない)、姿勢 = root の論理姿勢。root 自身は厳密に原点・無回転、世界は root からの相対で毎フレーム描き直す |
| (新規) ワールド側の追従 | **採用**(実装済) | `FDMiGeodeticWorldAnchor`(V2 `FDMiRelativeGroundSync` 相当)。地面などをこの下に置けば、地形コライダーごとフレームに追従し衝突判定も正しいまま。素の参照点でも可 |
| (新規) 切替時のアバター整合 | **採用**(実装済) | `rebaseFrame` が論理位置を切替前のフレームで読み直し(`oldOrigin` + `oldFrameRot * avatarPos`)、`FrameShift` のずれ分を `TeleportTo` する。着席中は VRChat が座席へ置き直すので触らない |
| (新規) 非オーナーの描画更新 | **採用**(実装済) | `FDMiGeodeticObjectSync.Update` が非オーナーで `windupPositionAndRotation()` を呼び、受信値の外挿を毎フレーム描画する(旧実装は capture のみで描画更新が無かった) |
