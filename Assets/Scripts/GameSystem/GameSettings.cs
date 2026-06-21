using UnityEngine;

// 게임플레이 옵션(2배속 / 윷 자동 던지기) — PlayerPrefs에 영구 저장.
// SoundManager의 PlayerPrefs 패턴과 동일하되 사운드와 무관해 별도 static으로 분리.
public static class GameSettings
{
    const string SpeedKey = "DoubleSpeed";
    const string AutoThrowKey = "AutoThrow";

    // #91 2배속
    public static bool DoubleSpeed
    {
        get => PlayerPrefs.GetInt(SpeedKey, 0) == 1;
        set { PlayerPrefs.SetInt(SpeedKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static float SpeedMultiplier => DoubleSpeed ? 2f : 1f;

    // #119 윷 자동 던지기
    public static bool AutoThrow
    {
        get => PlayerPrefs.GetInt(AutoThrowKey, 0) == 1;
        set { PlayerPrefs.SetInt(AutoThrowKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }
}