AllowDebugScene

ArrowCheckPoint:プレハブ

１．オブジェクト一覧

1.1. Playerオブジェクト
Capsule
CapsuleCollider
RigidBody:FreezeRotationを「x,y,z」すべてにチェック

PlayerController.cs
→「CameraTransform」に「MainCmaera」をアタッチ

PlayerResetter.cs
→「PlayerTransform」に「Player(Transform)」をアタッチ
→「OriginCoordinates」に基準点の座標を入力

1.2. MainCameraオブジェクト：Playerオブジェクトにアタッチ

1.3．DebugMainManagerオブジェクト
DebugMainManager.cs
→「PlayerController」に「Player」オブジェクトをアタッチ
→「RouteManager」に「DebugRouteManager」オブジェクトをアタッチ

1.4．DebugRouteManagerオブジェクト
DebugRouteManager.cs
→「OriginCoordinates」に基準点の座標を入力
→「ObjectManager」に「DebugObjectManager」オブジェクトをアタッチ
→「Prefabs」に「AllowCheckpoint」プレハブをアタッチ
→「Prefabs」に「Gate」プレハブをアタッチ
→「NavigationMode」で使用するナビゲーションタイプを選択「Allow，Gate」

//説明欄
Gate：チェックポイントで曲がる方向を見失わないように「前方ゲート」「後方ゲート」作成
	各チェックポイントの前方と後方に3m間隔をあけて配置
	その他のゲートは、各セグメントの残り距離（前方ゲートおよび後方ゲートを除いた距離）に基づき、指定されたgateIntervalごとにゲートを配置

1.5．DebugObjectManagerオブジェクト
DebugObjectManager.cs
→「TargetObject」に「Player」オブジェクトをアタッチ

1.6．DebugLogger
DebugLogger.cs

1.7　Groundオブジェクト：50×50くらいのデカいのを作る

1.8．GroundManagerオブジェクト：使ってない

1.9．EventSystemオブジェクト：Create → UI → EventSystem

1.10．Canvasオブジェクト:Create → UI → Canvas
子に「PlayerPositionText」を配置
→「TextMeshPro」
	→「ExtraSetting」→「RaycastTarget」のチェックを外す
PlayerPositionDisplay.cs
→「PositionText」に「PlayerPositionText」オブジェクトをアタッチ
→「PlayerTransform」に「Player(Transform)」をアタッチ

