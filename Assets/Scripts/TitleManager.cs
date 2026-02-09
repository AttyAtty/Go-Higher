using UnityEngine;
using UnityEngine.SceneManagement; // これが必要！

public class TitleManager : MonoBehaviour
{
    // publicでないと、UnityエディタのOn Click一覧に出てきません
    public void StartGame()
    {
        // 1番目のシーン（Minigame）を読み込む
        // Build Profilesで Minigame が Index 1 になっている必要があります
        SceneManager.LoadScene(1);
    }
}