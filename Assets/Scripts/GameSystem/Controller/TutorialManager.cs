using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialPanel tutorialPanel;

    public static bool isTutorial = false;
    public static bool readyToPlay = false;

    [SerializeField] private BoardNodeData junctionNodeData;      // "5모" 에셋
    [SerializeField] private BoardNodeData aiPlacementNodeData;  // "2개" 에셋
    [SerializeField] private BoardNodeData nearFinishNodeData;   // "28안찌" 에셋

    [SerializeField] private InputBlocker inputBlocker;
    [SerializeField] private DragAndDrop dragAndDrop;

    [SerializeField] private Button throwYutButton;
    [SerializeField] private Button blackYutButton;

    [SerializeField] private GameMaster gameMaster;
    
    private void Awake()
    {
        isTutorial = true;
        readyToPlay = false;
        ThrowYut.ForcedResults.Clear();
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
        yield return WaitForThrow(1);           // 보너스 던지기 (Do)
        inputBlocker.SetUIBlocked(true);        // 검은 윷·액티브 스킬 버튼 차단
        yield return WaitForPieceMove(p0.pieces[3], 1);

        // (8) 검은 윷 사용 (Gwishin 패시브로 이미 획득)
        yield return ShowPanel("TUTORIAL_BLACKYUT_TITLE", "TUTORIAL_BLACKYUT_BODY");
        yield return WaitForBlackYut();
        yield return WaitForPieceMove(null, 1); // 결과로 아무 말이나 이동

        // (9) 완주
        yield return ShowPanel("TUTORIAL_FINISH_TITLE", "TUTORIAL_FINISH_BODY");
        yield return WaitForFinish(null);

        // (10) 스킬 소개
        yield return ShowPanel("TUTORIAL_SKILL_TITLE", "TUTORIAL_SKILL_BODY");

        isTutorial = false;
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
            inputBlocker.AllowPieceOnly(allowedPiece);

        bool finished = false;
        Action<int> handler = _ => finished = true;
        GameEvents.OnPieceFinished += handler;
        yield return new WaitUntil(() => finished);
        GameEvents.OnPieceFinished -= handler;

        inputBlocker.Deactivate();
    }
}