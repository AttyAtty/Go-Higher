using UnityEngine;
using System.Collections.Generic;

public class ResultAudioManager : MonoBehaviour
{
    public AudioSource seSource;

    [Header("勝利・タイムアップ時の音（バリエーション）")]
    public List<AudioClip> winSounds;

    [Header("敵に当たった時の音（バリエーション）")]
    public List<AudioClip> loseSounds;

    // 勝利（完走）した時に呼ばれる
    public void PlayWinSE()
    {
        PlayRandomSound(winSounds);
    }

    // 敗北（衝突）した時に呼ばれる
    public void PlayLoseSE()
    {
        PlayRandomSound(loseSounds);
    }

    private void PlayRandomSound(List<AudioClip> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;

        // リストの中からランダムに1つ選ぶ
        int index = Random.Range(0, sounds.Count);
        AudioClip clip = sounds[index];

        if (seSource != null && clip != null)
        {
            seSource.PlayOneShot(clip);
        }
    }
}