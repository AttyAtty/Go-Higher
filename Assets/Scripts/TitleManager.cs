using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替えに必要

public class TitleManager : MonoBehaviour
{
    // ボタンから呼ばれる関数
    public void OnStartButtonClick()
    {
        // "MainScene" は今ゲームを作っているシーンの名前に書き換えてください
        SceneManager.LoadScene("MainScene");
    }
}