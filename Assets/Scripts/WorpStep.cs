using UnityEngine;

public class WarpStep : MonoBehaviour
{
    [Header("Color Settings")]
    public Material lockedMaterial;   // インスペクターで赤色を入れる
    public Material unlockedMaterial; // インスペクターで緑色を入れる

    private Renderer targetRenderer;

    void Awake()
    {
        // 自分の見た目を変えるためのレンダラーを取得
        targetRenderer = GetComponent<Renderer>();
        // 最初は赤色（封印）にする
        SetLocked();
    }

    // 赤色に変えるメソッド
    public void SetLocked()
    {
        if (targetRenderer != null && lockedMaterial != null)
            targetRenderer.material = lockedMaterial;
    }

    // 緑色に変えるメソッド
    public void SetUnlocked()
    {
        if (targetRenderer != null && unlockedMaterial != null)
            targetRenderer.material = unlockedMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. まずGameManagerにワープしていいか確認
            if (GameManager.instance != null && GameManager.instance.isWarpEnabled)
            {
                // --- ワープ実行 ---

                // 物理速度をリセット
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;

                // 目的地(Arrival)を探す
                Vector3 searchCenter = transform.position + new Vector3(0, 40f, 0);
                Collider[] hitColliders = Physics.OverlapSphere(searchCenter, 30f);
                Vector3 destination = searchCenter;

                foreach (var hit in hitColliders)
                {
                    if (hit.gameObject.CompareTag("Arrival"))
                    {
                        destination = hit.transform.position;
                        break;
                    }
                }

                // プレイヤーを移動
                other.transform.position = destination;

                // GameManagerに次の階の生成を依頼
                GameManager.instance.AdvanceFloor();

                // 次の階のために自分を赤色に戻しておく
                SetLocked();

                Debug.Log("次の階へ進みました！");
            }
            else
            {
                // アイテムが足りない場合
                Debug.Log("まだアイテムが足りない！ワープできません。");
                // ここで「まだ足りないよ！」というUIメッセージを出す処理を足してもいいですね
            }
        }
    }
}