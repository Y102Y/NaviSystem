# ARグラス用歩行者向けナビゲーションシステム

## 仕様

Android端末で取得した緯度・経度・方位情報をPC上のUnityに送信し、ARグラス上で歩行者向けナビゲーションを行うシステム

### システム概要
- Android端末に別途コンパスアプリをインストールする
- PCとAndroid端末を同一LAN内に接続する
- Android端末で取得した緯度・経度・方位情報を、PC上で起動しているTCPサーバーで受信する
- 受信した情報をUnity上のプレイヤーやカメラに反映する
- スタート地点の緯度・経度（Origin）と、曲がり角となるチェックポイントの緯度・経度を事前に設定する
- 設定した緯度・経度をもとに、ナビゲーション用オブジェクトを生成・配置する

---

## 本実験で使用しているオブジェクトとスクリプト

### 1. Playerオブジェクト
**使用スクリプト：`Assets/Scripts/Managers/GNSSReceiver`**

- 緯度・経度をUnity座標に変換し、プレイヤーの位置を更新する
- 方位角・ピッチ・ロールをもとに、カメラの向きを更新する
- 現在の緯度・経度・方位角などを画面上のUIに表示する

### 2. TCPServerオブジェクト
**使用スクリプト：`Assets/Scripts/Managers/UnityTcpServer`**

- Unity内でTCPサーバーを起動する
- 外部端末から送信されるデータを受信する
- 方位角（azimuth）、pitch、roll、緯度、経度を受信し、Unity内で利用可能な形で保持する

### 3. MainManagerオブジェクト
**使用スクリプト：`Assets/DebugScripts/DebugManagers/DebugMainManager`**  
※ デバッグ時に使用していたものを流用

- デバッグシーン開始時に初期化処理を行う
- キー入力を監視し、デバッグ情報を表示する
- 処理結果を `DebugLogger` に記録する

### 4. RouteManagerオブジェクト
**使用スクリプト：`Assets/Scripts/Managers/RouteManager`**

- （なぜかこのスクリプトのみGitHubに上げられていない）
- 内容は`Assets/DebugScripts/DebugManagers/DebugRouteManager`と類似しているため、そちらを参照

- 緯度経度で作成されたルートをUnity座標に変換する
- ルートに沿って、**矢印 / ゲート / ライン** を配置する
- ユーザーの現在位置に応じて、次に案内するチェックポイントを更新する
- `Assets/Scripts/Data/RouteData.cs`で作成したチェックポイント表を参照

### 5. ObjectManagerオブジェクト
**使用スクリプト：`Assets/DebugScripts/DebugMAnagers/DebugObjectManager`**  
※ デバッグ時に使用していたものを流用

- 生成したチェックポイント・ゲート・ラインを一元管理する
- オブジェクトの削除や個数確認をしやすくする

### 6. Loggerオブジェクト
**使用スクリプト：`Assets/DebugScripts/DebugManagers/DebugLogger`**  
※ デバッグ時に使用していたものを流用

- ログ出力をまとめて管理する

### 7. Canvasオブジェクト
- 子オブジェクトとして、緯度・経度や方位（pitch、roll など）を表示するUIを配置する
- デバッグ時に、これらの情報を画面上へ表示する

### 8. Arrow / Gate / Lineオブジェクト
- Blenderで作成したナビゲーション表示用オブジェクト

### 9. 筒オブジェクト
- スケールは大きめに設定（例：`5000 × 5000 × 700`）
- ARグラスの特性である **「黒色は透過される」** ことを利用し、Arrow / Gate / Line オブジェクトの表示範囲を制限する

### 10. ESCオブジェクト
**使用スクリプト：`Assets/Scripts/Managers/EscapeQuit`**

- `ESC`キーでアプリケーションを終了する

---
