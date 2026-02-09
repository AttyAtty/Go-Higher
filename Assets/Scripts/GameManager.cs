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
    public int requiredPickupsPerFloor = 2; // 1階あたりのノルマ
    private int currentFloorPickups = 0;   // 今の階で取った数
    public bool isWarpEnabled = false;      // ワープが有効か

    // 生成したフロアを管理するリスト（お掃除用）
    private List<GameObject> spawnedFloors = new List<GameObject>();

    private int score = 0;
    private bool isGameActive = false;

    void Awake()
    {
        // 他のスクリプトから使いやすくするための設定
        if (instance == null) instance = this;
    }

    void Start()
    {
        winTextObject.SetActive(false);

        GameObject f1 = GameObject.Find("Floor1");

        // ゲーム開始時に1階と2階の敵を生成
        // 1階(高さ0m)に1体
        if (f1 != null) spawnedFloors.Add(f1);
        SpawnEnemies(new Vector3(0, 0, 0), 1);
        SpawnPickups(new Vector3(0, 0.5f, 0), 3);
        SpawnObstacles(new Vector3(0, 0.5f, 0), 4); // メソッド名を修正

        GameObject f2 = GameObject.Find("Floor2");
        
        // 2階(高さ40m)に2体
        if (f2 != null) spawnedFloors.Add(f2);
        SpawnEnemies(new Vector3(0, 40f, 0), 2);
        SpawnPickups(new Vector3(0, 40f, 0), 3);
        SpawnObstacles(new Vector3(0, 40f, 0), 5); // メソッド名を修正

        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        isGameActive = false;
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        isGameActive = true;
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
            GameOver("Time Up!");
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
        requiredPickupsPerFloor++; // ノルマ増加

        UpdateRemainingUI();

        nextFloorToCreate = currentFloor ;
        float spawnHeight = nextFloorToCreate * 40f;
        Vector3 nextPos = new Vector3(0, spawnHeight, 0);

        GameObject newFloor = Instantiate(levelUnitPrefab, nextPos, Quaternion.identity);
        spawnedFloors.Add(newFloor); // リストに記録

        NavMeshSurface surface = newFloor.GetComponent<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
        }

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
        SpawnObstacles(nextPos, 3 + (nextFloorToCreate * 2));
        SpawnDynamicBoxes(nextPos, nextFloorToCreate * 2);

        //お掃除機能：3つ以上前のフロアがあれば削除
        if (spawnedFloors.Count > 3)
        {
            GameObject oldFloor = spawnedFloors[0];
            spawnedFloors.RemoveAt(0);
            Destroy(oldFloor); // フロアごと（その上の敵やアイテムも）消去
        }

    }

    void UpdateRemainingUI()
    {
        if (remainingCountText != null)
        {
            // "1 / 3" のような形式で表示
            remainingCountText.text = currentFloorPickups.ToString() + " / " + requiredPickupsPerFloor.ToString();

            // 色を変える演出（任意）：ノルマ達成したら緑色にするなど
            if (currentFloorPickups >= requiredPickupsPerFloor)
            {
                remainingCountText.color = Color.green;
            }
            else
            {
                remainingCountText.color = Color.white;
            }
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

        // UIを更新
        UpdateRemainingUI();

        if (currentFloorPickups >= requiredPickupsPerFloor)
        {
            if (!isWarpEnabled)
            {
                isWarpEnabled = true;

                // ワープ解放メッセージ（こちらは一時的な通知として利用）
                countdownText.gameObject.SetActive(true);
                countdownText.text = "ワープ解放！";
                Invoke("HideMessage", 3f);

                WarpStep ws = Object.FindFirstObjectByType<WarpStep>();
                if (ws != null) ws.SetUnlocked();
            }
        }
    }

    void HideMessage() { countdownText.gameObject.SetActive(false); }

    Vector3 GetRandomPos(Vector3 center)
    {
        Vector3 pos = center + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
        if (Vector3.Distance(pos, center) < 2.5f) return Vector3.zero;
        return pos;
    }

    // ゲームオーバー・勝利判定
    public void GameOver(string message)
    {
        isGameActive = false;
        winTextObject.SetActive(true);
        winTextObject.GetComponent<TextMeshProUGUI>().text = message;

        // 3秒後にリスタートさせるなどの処理も可能
         Invoke("Restart", 3f);
    }



}