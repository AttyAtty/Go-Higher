using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替えに必要
using TMPro; // 追加

public class GameController : MonoBehaviour
{
    public GameObject resultMenu; // InspectorでResultMenuをドラッグ＆ドロップ
    public TextMeshProUGUI finalResultText;

    void Start()
    {
        // 始まった時はメニューを隠しておく
        resultMenu.SetActive(false);
    }

    // ゲームが終わった時にこれを呼ぶ（何かに当たった時など）
    public void GameOver(int score, int floor, bool isCaught)
    {
        resultMenu.SetActive(true); // メニューを表示
        Time.timeScale = 0f;        // ゲームの時間を止める

        int finalScore = 0;


        if (isCaught)
        {
            // 敵に当たった場合は、情け容赦なく0点！
            finalResultText.text = $"<color=red>CAUGHT BY ENEMY!</color>\n" +
                                   $"TOTAL: 0";
        }
        else
        {
            // ボーナス計算
            int floorBonus = floor * 5;
            finalScore = score + floorBonus;

            // 結果を表示
            finalResultText.text = $"Pickups: {score}\n" +
                               $"Floor Bonus: {floorBonus}\n" +
                               $"TOTAL: {finalScore}";
        }
    }

    // Replayボタンに割り当てる関数
    public void Replay()
    {
        Time.timeScale = 1f; // 時間を動かす
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 今のシーンを再読み込み
    }

    // Go Homeボタンに割り当てる関数
    public void GoHome()
    {
        Time.timeScale = 1f; // 時間を動かす
        SceneManager.LoadScene("Title"); // タイトルシーンへ（名前を合わせてね）
    }
}