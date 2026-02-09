using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private float movementX;
    private float movementY;

    public float speed = 10; // 速度の初期値を少し入れておくとテストしやすいです

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);
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

    private void OnCollisionEnter(Collision collision)
    {
        // 敵に触れたとき
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // プレイヤーを消すのではなく非表示にする（カメラや管理スクリプトのエラー防止）
            gameObject.SetActive(false);

            // GameManagerに負けを通知する
            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver("You Lose!", true);
            }
        }
    }
}

