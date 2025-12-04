using UnityEngine;

/// <summary>
/// 퀴즈 문제 데이터 구조
/// </summary>
[System.Serializable]
public class QuizData
{
    [Header("Question")]
    public string questionText;      // 문제 텍스트
    public bool correctAnswer;       // 정답 (true = O, false = X)
    [TextArea] public string explanation; // [추가] 정답 설명 (교육용)

    [Header("Visual Assets")]
    public GameObject correctAssetPrefab;   // 정답 에셋 (O)
    public GameObject incorrectAssetPrefab; // 오답 에셋 (X)

    public QuizData(string question, bool answer, GameObject correctAsset, GameObject incorrectAsset)
    {
        questionText = question;
        correctAnswer = answer;
        correctAssetPrefab = correctAsset;
        incorrectAssetPrefab = incorrectAsset;
    }
}
