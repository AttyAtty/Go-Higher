using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private Transform player;

    // 敵が反応する高さの範囲（例：上下5m以内にプレイヤーがいたら動く）
    public float detectionHeightRange = 5f;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        // シーン内のPlayerを自動で探す
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        //ゲームがアクティブでない（カウントダウン中やタイムアップ後）なら何もしない
        if (GameManager.instance != null && !GameManager.instance.isGameActive)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        if (player == null || !navMeshAgent.isOnNavMesh) return;

        // プレイヤーと自分の高さ（Y座標）の差を計算
        float heightDiff = Mathf.Abs(transform.position.y - player.position.y);

        // 同じ階層（高さが近い）にいる時だけ追跡する
        if (heightDiff < detectionHeightRange)
        {
            navMeshAgent.isStopped = false; // 動く
            navMeshAgent.SetDestination(player.position);
        }
        else
        {
            navMeshAgent.isStopped = true; // 止まる
        }
    }
}