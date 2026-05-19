using System;
using UnityEngine;

[Serializable] public class AIPersonality
{
    float captureWeight = 1f;   // 잡기 선호도
    float progressWeight = 1f;  // 전진 선호도
    float finishWeight = 1f;    // 완주 선호도
    float stackWeight = 1f;     // 업기 선호도
    float randomness = 0.3f;    // 무작위성 (높을수록 예측 불가)
}

[CreateAssetMenu(fileName = "CharacterData", menuName = "Yutnori/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string name;
    public Sprite icon;

    public AIPersonality aiPersonality;
    // public CharacterAbility ability;
}
