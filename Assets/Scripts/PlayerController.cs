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

    private Animator anim;

    void Start()
    {
        // Rigidbodyの代わりにCharacterControllerを取得
        controller = GetComponent<CharacterController>();
        // 自身のAnimatorコンポーネントを取得
        anim = GetComponent<Animator>();

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


        //---アニメーション制御-- -
        if (anim != null)
        {
            // 入力があるときは 1 に近くなり、止まっているときは 0 になる
            float currentSpeed = moveInput.magnitude;
            anim.SetFloat("Speed", currentSpeed);
        }

        if (controller.isGrounded)
        {
            moveDirection = moveInput * speed;

            // --- ここから追加：体の向きを変える処理 ---
            if (moveInput.magnitude > 0.1f) // 入力がある時だけ向きを変える
            {
                // 移動方向を向くための回転を作成
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);
 
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

            // 【重要】まず相手のコライダーを即座に無効化する！
            // これにより、このフレーム内での2回目以降の判定を物理的に遮断します。
            // 謎に一つ集めたのに対して複数回反応してしまう問題があったので、その対策
            Collider pickupCollider = other.gameObject.GetComponent<Collider>();
            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

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
                GameManager.instance.GameOver("You Lose!", true, false);
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

