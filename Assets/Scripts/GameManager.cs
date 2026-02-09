using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Listを使うために追加
using UnityEngine.SceneManagement;
using Unity.AI.Navigation; // これを一番上に書く

public class GameManager : MonoBehaviour
{
    // どこからでも GameManager.instance で呼べる
    public static GameManager instance;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI heightText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI remainingCountText;
    public GameObject winTextObject;


    [Header("Game Settings")]
    public float remainingTime = 60f;
    public Transform player;

    [Header("Floor Settings")]
    public int currentFloor = 1; // 初期値は1階
    private int nextFloorToCreate = 1; // 次に生成する階数

    [Header("Level Prefabs")]
    public GameObject levelUnitPrefab;
    public GameObject enemyPrefab;
    public GameObject pickupPrefab;
    public GameObject[] obstaclePrefabs; // ここに Obstacle(1)から(5) を入れる
    public GameObject dynamicBoxPrefab;

    [Header("Quest Settings")]
    public int requiredPickupsPerFloor = 1; // 1階あたりのノルマ
    private int currentFloorPickups = 0;   // 今の階で取った数
    public bool isWarpEnabled = false;      // ワープが有効か

    // 生成したフロアを管理するリスト（お掃除用）
    private List<GameObject> spawnedFloors = new List<GameObject>();

    private int score = 0;
    public bool isGameActive = false;

    void Awake()
    {
        // 他のスクリプトから使いやすくするための設定
        if (instance == null) instance = this;
    }

    void Start()
    {
        winTextObject.SetActive(false);

        SetupInitialFloor("Floor1", 1, 0.5f);
        SetupInitialFloor("Floor2", 2, 40f);

        RefreshQuestProgress();

        StartCoroutine(StartGameRoutine());
    }

    // 初期配置フロアのセットアップ
    void SetupInitialFloor(string name, int floorNum, float height)
    {
        GameObject f = GameObject.Find(name);
        if (f != null)
        {
            spawnedFloors.Add(f);
            Vector3 center = new Vector3(0, height, 0);

            // 要望: 出現数は階数の2倍
            SpawnEnemies(center, floorNum);
            SpawnPickups(center, floorNum * 2);
            SpawnObstacles(center, 3 + floorNum);
            SpawnDynamicBoxes(center, floorNum * 2);

            WarpStep ws = f.GetComponentInChildren<WarpStep>();
            if (ws != null) ws.SetLocked();
        }
    }

    IEnumerator StartGameRoutine()
    {
        isGameActive = false;
        countdownText.gameObject.SetActive(true);

        // すべてのものを非アクティブに
        SetAllEntitiesActive(false);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";

        //タイミングで全員の動きを解放する
        isGameActive = true;
        SetAllEntitiesActive(true);
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

    }

    // 動体をまとめて「停止/再開」させる
    void SetAllEntitiesActive(bool active)
    {
        // プレイヤーの物理を止める/動かす
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // active=falseなら物理演算をオフ(isKinematic)にしてピタッと止める
                rb.isKinematic = !active;
            }
        }

        // 敵のAIを止める/動かす
        var agents = Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            agent.isStopped = !active;
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        // タイマー処理
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(remainingTime).ToString();
        }
        else
        {
            GameOver("Time Up!", true);
        }

        // 高さ（Y座標）の更新
        if (player != null)
        {
            heightText.text = "Floor: " + currentFloor.ToString();
        }
    }

    // スコア加算メソッドを修正
    public void AddScore(int amount)
    {
        // 獲得スコア = 基本値 × 現在の階数
        score += amount * currentFloor;
        scoreText.text = "Score: " + score.ToString();
    }

    // 階数を増やすメソッド
    public void AdvanceFloor()
    {
        currentFloorPickups = 0; // リセット
        isWarpEnabled = false;   // ワープ封印
        currentFloor++; //階数の更新
        requiredPickupsPerFloor = currentFloor; // ノルマ増加

        UpdateRemainingUI();

        nextFloorToCreate = currentFloor ;
        float spawnHeight = nextFloorToCreate * 40f;
        Vector3 nextPos = new Vector3(0, spawnHeight, 0);

        GameObject newFloor = Instantiate(levelUnitPrefab, nextPos, Quaternion.identity);
        spawnedFloors.Add(newFloor); // リストに記録

        //生成したフロア内のワープ板を封印する
        WarpStep ws = newFloor.GetComponentInChildren<WarpStep>();
        if (ws != null) ws.SetLocked();

        NavMeshSurface surface = newFloor.GetComponent<NavMeshSurface>();
        if (surface != null) surface.BuildNavMesh();

        // ナビメッシュが焼き上がるのを1フレーム待ってから敵を出す
        StartCoroutine(DelayedSpawn(nextPos, nextFloorToCreate + 1));
    }

    // IEnumerator:処理を途中で一時停止したり、時間を置いて再開したりできる仕組み
    // voidで無理やりやろうとすると、ゲーム画面全体がフリーズするが、IEnumerator を使うとゲームの動きを止めずに、その処理だけを待機させることができる
    IEnumerator DelayedSpawn(Vector3 nextPos, int nextFloorToCreate)
    {
        yield return null; // 1フレーム待機

        // リストが空の場合のエラー防止策を追加
        Transform parentFloor = spawnedFloors.Count > 0 ? spawnedFloors[spawnedFloors.Count - 1].transform : null;

        // オブジェクト生成（階数 nextFloorToCreate に応じて増加）
        SpawnEnemies(nextPos, nextFloorToCreate);
        SpawnPickups(nextPos, 1 + 2 * nextFloorToCreate);
        SpawnObstacles(nextPos, 2 + (nextFloorToCreate * 2));
        SpawnDynamicBoxes(nextPos, nextFloorToCreate * 2);

        //お掃除機能：3つ以上前のフロアがあれば削除
        if (spawnedFloors.Count > 3)
        {
            GameObject oldFloor = spawnedFloors[0];
            spawnedFloors.RemoveAt(0);
            Destroy(oldFloor); // フロアごと（その上の敵やアイテムも）消去
        }

    }

    // クエスト状況をリセット・更新
    void RefreshQuestProgress()
    {
        requiredPickupsPerFloor = currentFloor;
        currentFloorPickups = 0;
        isWarpEnabled = false;
        UpdateRemainingUI();
    }

    void UpdateRemainingUI()
    {
        if (remainingCountText != null)
        {
            // "1 / 3" のような形式で表示
            remainingCountText.text = "Collect: " + currentFloorPickups.ToString() + " / " + requiredPickupsPerFloor.ToString();

            // 色を変える演出（任意）：ノルマ達成したら緑色にするなど
            remainingCountText.color = (currentFloorPickups >= requiredPickupsPerFloor) ? Color.green : Color.white;
        }
    }

    // --- 生成メソッド群 ---

    void SpawnEnemies(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPos(center);
            if (pos == Vector3.zero) continue;

            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            // 生成された敵をフロアの子にする（フロア削除時に一緒に消えるようにする）
            enemy.transform.SetParent(spawnedFloors[spawnedFloors.Count - 1].transform);

            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // ゲームがまだアクティブ（GO!の前）なら、最初から止めておく
                if (!isGameActive)
                {
                    agent.isStopped = true;
                }
            }
        }
    }

    void SpawnPickups(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPos(center);
            if (pos == Vector3.zero) continue;
            GameObject obj = Instantiate(pickupPrefab, pos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            obj.transform.SetParent(spawnedFloors[spawnedFloors.Count - 1].transform);
        }
    }

    void SpawnObstacles(Vector3 center, int count)
    {
        if (obstaclePrefabs.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPos(center);
            if (pos == Vector3.zero) continue;

            // 配列からランダムに1つ選ぶ
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            obj.transform.SetParent(spawnedFloors[spawnedFloors.Count - 1].transform);
        }
    }

    void SpawnDynamicBoxes(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPos(center);
            if (pos == Vector3.zero) continue;
            GameObject obj = Instantiate(dynamicBoxPrefab, pos, Quaternion.identity);
            obj.transform.SetParent(spawnedFloors[spawnedFloors.Count - 1].transform);
        }
    }

    // アイテムを取った時に呼ばれるメソッド
    public void OnPickupCollected()
    {
        currentFloorPickups++;
        UpdateRemainingUI();// UIを更新

        if (currentFloorPickups >= requiredPickupsPerFloor)
        {
            if (!isWarpEnabled)
            {
                isWarpEnabled = true;

                // ワープ解放宣言
                countdownText.gameObject.SetActive(true);
                countdownText.text = "Warp Enabled!";
                Invoke("HideMessage", 3f);

                //リストのインデックスだと混乱してしまったので、物理的に同じフロアにあるWarpStepを探すように修正
                WarpStep[] allWarpSteps = Object.FindObjectsByType<WarpStep>(FindObjectsSortMode.None);
                foreach (var ws in allWarpSteps)
                {
                    // プレイヤーとワープ板の高さ(Y座標)の差が10m以内なら「今いる階の板」と判定
                    if (Mathf.Abs(player.position.y - ws.transform.position.y) < 10f)
                    {
                        ws.SetUnlocked();
                    }
                }
            }
        }
    }

    void HideMessage() { if (countdownText != null) countdownText.gameObject.SetActive(false); }

    Vector3 GetRandomPos(Vector3 center)
    {
        Vector3 pos = center + new Vector3(Random.Range(-7f, 7f), 0, Random.Range(-7f, 7f));
        if (Vector3.Distance(pos, center) < 2.5f) return Vector3.zero;
        return pos;
    }

    // ゲームオーバー・勝利判定
    public void GameOver(string message, bool isCaught = false)
    {
        isGameActive = false;
        winTextObject.SetActive(true);
        winTextObject.GetComponent<TextMeshProUGUI>().text = message;

        // 1. プレイヤーの物理挙動を止める
        if (player != null)
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                //playerRb.linearVelocity = Vector3.zero; // 速度を0に
                //playerRb.angularVelocity = Vector3.zero; // 回転を0に
                playerRb.isKinematic = true; // 物理演算の影響を受けなくする
            }

            // もしプレイヤーのスクリプト（PlayerControllerなど）があれば無効化する
            player.GetComponent<PlayerController>().enabled = false;
        }

        // 2. すべての敵のナビゲーションを止める
        // 敵に NavMeshAgent がついている場合
        UnityEngine.AI.NavMeshAgent[] agents = Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            agent.isStopped = true; // 移動を停止
        }

        GameController gc = GetComponent<GameController>();
        if (gc != null)
        {
            gc.GameOver(score, currentFloor, isCaught);
        }

    }

}