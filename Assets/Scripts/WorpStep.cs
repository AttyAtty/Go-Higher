using UnityEngine;

public class WarpStep : MonoBehaviour
{
    [Header("Color Settings")]
    public Material lockedMaterial;   // インスペクターで白色を入れる
    public Material unlockedMaterial; // インスペクターで緑色を入れる
    public AudioSource audioSource; // インスペクターで自身、または子のAudioSourceをセット
    public AudioClip warpSound;      // ワープ音のファイルをセット

    private Renderer targetRenderer;
    void Start()
    {
        SetLocked();
    }
    void Awake()
    {
        // 自分の見た目を変えるためのレンダラーを取得
        targetRenderer = GetComponent<Renderer>();
        // 最初は封印する
        SetLocked();
    }

    // 深緑色に変えるメソッド
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
            if (GameManager.instance != null && GameManager.instance.isWarpEnabled)
            {
                // ワープ音を鳴らす
                if (audioSource != null && warpSound != null)
                {
                    // audioSource.time = 0.1f; //音をとばす
                    audioSource.PlayOneShot(warpSound);
                }


                // 1. CharacterControllerを取得
                CharacterController cc = other.GetComponent<CharacterController>();

                // 2. 物理速度のリセット（CharacterControllerの場合は不要ですが、念のため）
                // Rigidbody rb = other.GetComponent<Rigidbody>(); // これは消すかコメントアウト
                // if (rb != null) rb.linearVelocity = Vector3.zero;

                // 目的地(Arrival)を探す処理はそのまま
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

                // 3. 重要：CharacterControllerを一時的に無効化してワープ
                if (cc != null) cc.enabled = false;
                other.transform.position = destination;
                if (cc != null) cc.enabled = true; // ワープ後にすぐ有効化

                GameManager.instance.AdvanceFloor();
                SetLocked();
                Debug.Log("次の階へ進みました！");
            }
        }
    }
}