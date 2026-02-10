using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller; // Rigidbodyの代わりにこれを使う
    private Vector3 moveDirection;
    private float movementX;
    private float movementZ; // movementYから名前を変えると分かりやすいです（前後移動なので）

    public float speed = 10;
    public float gravity = 20.0f; // 重力の設定

    void Start()
    {
        // Rigidbodyの代わりにCharacterControllerを取得
        controller = GetComponent<CharacterController>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementZ = movementVector.y;
    }

    void Update()
    {
        // 1. 移動方向の計算（既存のコード）
        Vector3 moveInput = new Vector3(movementX, 0.0f, movementZ);

        if (controller.isGrounded)
        {
            moveDirection = moveInput * speed;

            // --- ここから追加：体の向きを変える処理 ---
            if (moveInput.magnitude > 0.1f) // 入力がある時だけ向きを変える
            {
                // 移動方向を向くための回転を作成
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);

                // 瞬時に向かせる場合：
                // transform.rotation = targetRotation;

                // 滑らかに向かせる場合（こっちがおすすめ）：
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            // ------------------------------------------
        }

        // 重力と移動の実行（既存のコード）
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 報酬アイテムに触れたとき
        if (other.gameObject.CompareTag("PickUp"))
        {
            // アイテムを消す
            Destroy(other.gameObject);

            // GameManagerにスコア加算を依頼する
            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore(1);            // スコア加算
                GameManager.instance.OnPickupCollected();   // アイテム獲得数を数える
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 1. まず「敵」かどうかを先に判定する（これならRigidbodyがなくても動く）
        if (hit.gameObject.CompareTag("Enemy"))
        {
            gameObject.SetActive(false);
            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver("You Lose!", true);
            }
            return; // 負けたのでここで処理終了
        }

        // 2. その後に箱を押す判定をする
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        if (hit.moveDirection.y < -0.3) return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.linearVelocity = pushDir * 2.0f;
    }
}

