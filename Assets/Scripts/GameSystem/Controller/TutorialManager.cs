using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialPanel tutorialPanel;

    public static bool isTutorial = false;
    public static bool readyToPlay = false;
    public static bool allowSkillDemo = false;

    [SerializeField] private BoardNodeData junctionNodeData;      // 모
    [SerializeField] private BoardNodeData aiPlacementNodeData;  // 개
    [SerializeField] private BoardNodeData nearFinishNodeData;  // 참먹이

    [SerializeField] private InputBlocker inputBlocker;
    [SerializeField] private DragAndDrop dragAndDrop;

    [SerializeField] private Button throwYutButton;
    [SerializeField] private Button blackYutButton;
    [SerializeField] private Button activeSkillButton;
    [SerializeField] private BoardNodeData[] skillDemoAiNodeDatas;

    [SerializeField] private GameMaster gameMaster;
    [SerializeField] private Image fadeOverlay;
    
    private void Awake()
    {
        isTutorial = true;
        readyToPlay = false;
        allowSkillDemo = false;
        ThrowYut.ForcedResults.Clear();
        fadeOverlay.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ThrowYut.ForcedResults.Clear();
        isTutorial = false;
        readyToPlay = false;
        allowSkillDemo = false;
    }

    private void Start()
    {
        StartCoroutine(CoRunTutorial());
    }

    private IEnumerator CoRunTutorial()
    {
        var p0 = gameMaster.GetPlayer(0);
        var p1 = gameMaster.GetPlayer(1);

        // (1) 소개
        yield return ShowPanel("TUTORIAL_INTRO_TITLE", "TUTORIAL_INTRO_BODY");

        // (2) 윷 던지기
        ThrowYut.ForcedResults.Enqueue(YutResult.Geol);
        readyToPlay = true;
        yield return WaitForThrow(1);

        // (3) 윷 결과 설명 (윷 가이드 표시)
        yield return ShowPanel("TUTORIAL_YUT_RESULT_TITLE", null, null, true);

        // 윷 가이드를 한 번 보여준 뒤부터 H 키 가이드 활성화
        gameMaster.ArmGuidePopup();

        // (4) 말 이동 (Geol로 pieces[0])
        yield return ShowPanel("TUTORIAL_MOVE_TITLE", "TUTORIAL_MOVE_BODY");
        yield return WaitForPieceMove(p0.pieces[0], 1);

        // (5) 갈림길
        ThrowYut.ForcedResults.Enqueue(YutResult.Mo);
        ThrowYut.ForcedResults.Enqueue(YutResult.Do);
        yield return ShowPanel("TUTORIAL_JUNCTION_TITLE", "TUTORIAL_JUNCTION_BODY");
        yield return WaitForThrow(2);   // Mo → 보너스 → Do
        inputBlocker.AllowDestinationOnly(gameMaster.GetNode(junctionNodeData));   // Mo: 갈림길 노드로만
        yield return WaitForPieceMove(p0.pieces[1], 1);   // Mo: 갈림길 노드까지
        inputBlocker.AllowDestinationOnly(gameMaster.GetNode(junctionNodeData.shortcutNext));
        yield return WaitForPieceMove(p0.pieces[1], 1);   // Do: 지름길만 허용

        // (6) 업기
        ThrowYut.ForcedResults.Enqueue(YutResult.Geol);
        yield return ShowPanel("TUTORIAL_STACK_TITLE", "TUTORIAL_STACK_BODY");
        yield return WaitForThrow(1);
        yield return WaitForStack(p0.pieces[2]);

        // (7) 잡기 + 보너스 턴 (새 턴)
        ThrowYut.ForcedResults.Enqueue(YutResult.Gae);
        ThrowYut.ForcedResults.Enqueue(YutResult.Do);
        gameMaster.PlaceTutorialPiece(p1.pieces[0], aiPlacementNodeData);
        yield return ShowPanel("TUTORIAL_CAPTURE_TITLE", "TUTORIAL_CAPTURE_BODY");
        yield return WaitForThrow(1);
        yield return WaitForCapture(p0.pieces[3]);
        yield return ShowPanel("TUTORIAL_GRUDGE_TITLE", "TUTORIAL_GRUDGE_BODY");
        yield return WaitForThrow(1);           // 보너스 던지기 (Do)
        inputBlocker.SetUIBlocked(true);        // 검은 윷·액티브 스킬 버튼 차단
        yield return WaitForPieceMove(p0.pieces[3], 1);

        // (8) 검은 윷 사용 (Gwishin 패시브로 이미 획득)
        yield return ShowPanel("TUTORIAL_BLACKYUT_TITLE", "TUTORIAL_BLACKYUT_BODY");
        yield return WaitForBlackYut();
        yield return WaitForPieceMove(p0.pieces[1], 1);

        // (9) 완주
        yield return ShowPanel("TUTORIAL_FINISH_TITLE", "TUTORIAL_FINISH_BODY");
        ThrowYut.ForcedResults.Enqueue(YutResult.Mo);
        ThrowYut.ForcedResults.Enqueue(YutResult.Do);    // 모 보너스 굴리기
        yield return WaitForThrow(2);                     // 모 + 보너스 도
        inputBlocker.AllowDestinationOnly(gameMaster.GetNode(nearFinishNodeData));
        yield return WaitForPieceMove(p0.pieces[1], 1);  // 모: 참먹이 착지
        yield return ShowPanel("TUTORIAL_LASTNODE_TITLE", "TUTORIAL_LASTNODE_BODY");
        yield return WaitForFinish(p0.pieces[1]);         // 남은 도로 완주

        // (10) 스킬 소개 + 시연
        yield return ShowPanel("TUTORIAL_SKILL_TITLE", "TUTORIAL_SKILL_BODY");

        for (int i = 0; i < skillDemoAiNodeDatas.Length; i++)
            gameMaster.PlaceTutorialPiece(p1.pieces[i], skillDemoAiNodeDatas[i]);

        allowSkillDemo = true;
        ThrowYut.ForcedResults.Enqueue(YutResult.Gae);
        yield return WaitForThrow(1);

        inputBlocker.SetUIBlocked(true);
        inputBlocker.BlockPieces();
        dragAndDrop.RefreshHighlights();
        inputBlocker.AllowButton(activeSkillButton.GetComponent<RectTransform>());
        yield return new WaitUntil(() => gameMaster.IsActiveSkillOn);

        inputBlocker.Deactivate();
        yield return WaitForPieceMove(p0.pieces[2], 1);

        allowSkillDemo = false;
        isTutorial = false;
        yield return ShowPanel("TUTORIAL_END_TITLE", "TUTORIAL_END_BODY");
        yield return CoFadeAndLoad("TitleScene");
    }

    private IEnumerator CoFadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(CoFade(0f, 1f, 0.8f));
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator CoFade(float from, float to, float duration)
    {
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        Color c = fadeOverlay.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = to;
        fadeOverlay.color = c;
    }

    private IEnumerator ShowPanel(string titleKey, string bodyKey, string bodyKeyRight = null, bool useYutGuide = false)
    {
        inputBlocker.SetUIBlocked(true);
        inputBlocker.AllowButton(tutorialPanel.NextButtonRect);
        yield return StartCoroutine(tutorialPanel.CoShow(titleKey, bodyKey, bodyKeyRight, useYutGuide));
        inputBlocker.Deactivate();
    }

    private IEnumerator WaitForThrow(int count)
    {
        inputBlocker.SetUIBlocked(true);
        inputBlocker.AllowButton(throwYutButton.GetComponent<RectTransform>());

        int thrown = 0;
        Action<int> handler = _ => thrown++;
        GameEvents.OnYutThrown += handler;
        yield return new WaitUntil(() => thrown >= count);
        GameEvents.OnYutThrown -= handler;

        inputBlocker.Deactivate();
    }

    private IEnumerator WaitForPieceMove(Piece allowedPiece, int count)
    {
        if (allowedPiece != null)
        {
            inputBlocker.AllowPieceOnly(allowedPiece);
            dragAndDrop.RefreshHighlights();
        }

        int moved = 0;
        Action<int> handler = _ => moved++;
        GameEvents.OnPieceMoved += handler;
        yield return new WaitUntil(() => moved >= count);
        GameEvents.OnPieceMoved -= handler;

        inputBlocker.Deactivate();
    }

    private IEnumerator WaitForStack(Piece allowedPiece)
    {
        inputBlocker.AllowPieceOnly(allowedPiece);

        bool stacked = false;
        Action<int> handler = _ => stacked = true;
        GameEvents.OnPieceStacked += handler;
        yield return new WaitUntil(() => stacked);
        GameEvents.OnPieceStacked -= handler;

        inputBlocker.Deactivate();
    }

    private IEnumerator WaitForCapture(Piece allowedPiece)
    {
        inputBlocker.AllowPieceOnly(allowedPiece);

        bool captured = false;
        Action<int> handler = _ => captured = true;
        GameEvents.OnCaptureSuccess += handler;
        yield return new WaitUntil(() => captured);
        GameEvents.OnCaptureSuccess -= handler;

        inputBlocker.Deactivate();
    }


    private IEnumerator WaitForBlackYut()
    {
        ThrowYut.ForcedResults.Enqueue(YutResult.Mo);

        inputBlocker.SetUIBlocked(true);
        inputBlocker.AllowButton(blackYutButton.GetComponent<RectTransform>());

        bool used = false;
        Action<int> handler = _ => used = true;
        GameEvents.OnBlackYutUsed += handler;
        yield return new WaitUntil(() => used);
        GameEvents.OnBlackYutUsed -= handler;

        inputBlocker.Deactivate();
    }

    private IEnumerator WaitForFinish(Piece allowedPiece)
    {
        if (allowedPiece != null)
        {
            inputBlocker.AllowPieceOnly(allowedPiece);
            dragAndDrop.RefreshHighlights();
        }

        bool finished = false;
        Action<int> handler = _ => finished = true;
        GameEvents.OnPieceFinished += handler;
        yield return new WaitUntil(() => finished);
        GameEvents.OnPieceFinished -= handler;

        inputBlocker.Deactivate();
    }
}