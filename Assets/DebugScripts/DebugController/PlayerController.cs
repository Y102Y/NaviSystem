// Assets/Scripts/PlayerController.cs

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;            // 移動速度
    public float rotationSpeed = 720f;      // 回転速度（度/秒）

    [Header("Camera Settings")]
    public Transform cameraTransform;       // カメラのTransform
    public float mouseSensitivity = 100f;   // マウス感度

    private Rigidbody rb;
    private float xRotation = 0f;           // カメラの上下回転角度

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbodyの回転を制限してプレイヤーが倒れないようにする
        rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationY | 
                         RigidbodyConstraints.FreezeRotationZ;

        // カメラがアタッチされていない場合、エラーを出力
        if (cameraTransform == null)
        {
            Debug.LogError("PlayerController: Camera Transform が設定されていません。");
        }

        // マウスカーソルを非表示にし、画面中央にロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 入力取得はUpdateで行い、FixedUpdateで物理処理
        float inputMoveX = Input.GetAxis("Horizontal"); // A/D または 左右矢印キー
        float inputMoveZ = Input.GetAxis("Vertical");   // W/S または 上下矢印キー

        float inputMouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float inputMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        HandleMouseLook(inputMouseY);
        HandlePlayerRotation(inputMouseX);
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    /// <summary>
    /// プレイヤーの移動処理
    /// </summary>
    void HandleMovement()
    {
        float inputMoveX = Input.GetAxis("Horizontal");
        float inputMoveZ = Input.GetAxis("Vertical");

        // 移動ベクトルの計算
        Vector3 movement = new Vector3(inputMoveX, 0f, inputMoveZ).normalized * moveSpeed;

        // プレイヤーの向きに基づいた移動
        Vector3 moveDirection = transform.right * movement.x + transform.forward * movement.z;

        // Rigidbodyを使用して移動
        rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);
    }

    /// <summary>
    /// プレイヤーの回転処理（左右回転）
    /// </summary>
    /// <param name="inputMouseX">マウスのX軸入力</param>
    void HandlePlayerRotation(float inputMouseX)
    {
        // プレイヤーの左右回転
        Quaternion deltaRotation = Quaternion.Euler(0f, inputMouseX, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    /// <summary>
    /// カメラの上下回転処理
    /// </summary>
    /// <param name="inputMouseY">マウスのY軸入力</param>
    void HandleMouseLook(float inputMouseY)
    {
        // カメラの上下回転を制限
        xRotation -= inputMouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // カメラの回転を更新
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void OnDisable()
    {
        // スクリプトが無効化されたときにカーソルを解放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
