using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class OpponentCharacterController : MonoBehaviour
{
    [SerializeField] private int opponentCharacterId = 1;
    [SerializeField] private CinemachineCamera characterCam;
    [SerializeField] private float camHoldDuration = 2f;
    [SerializeField] public CharacterData linkedCharacter;

    private Animator _animator;
    private CinemachineBrain _brain;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    void OnEnable()
    {
        GameEvents.OnYutThrown      += HandleYutThrown;
        GameEvents.OnCaptureSuccess += HandleCaptureSuccess;
        GameEvents.OnCaptured  += HandleCaptured;
        GameEvents.OnPieceFinished  += HandlePieceFinished;
    }

    void OnDisable()
    {
        GameEvents.OnYutThrown      -= HandleYutThrown;
        GameEvents.OnCaptureSuccess -= HandleCaptureSuccess;
        GameEvents.OnCaptured  -= HandleCaptured;
        GameEvents.OnPieceFinished  -= HandlePieceFinished;
    }

    void HandleYutThrown(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("YutThrown");
        StartCoroutine(CoFocusCharacter());
    }

    void HandleCaptureSuccess(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("CaptureSuccess");
        StartCoroutine(CoFocusCharacter());
    }

    void HandleCaptured(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("Captured");
        StartCoroutine(CoFocusCharacter());
    }

    void HandlePieceFinished(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("PieceFinished");
        StartCoroutine(CoFocusCharacter());
    }

    IEnumerator CoFocusCharacter()
    {
        if (characterCam == null) yield break;

        characterCam.Priority = new PrioritySettings { Value = 25 };

        yield return null;
        if (_brain != null)
            yield return new WaitUntil(() => !_brain.IsBlending);

        yield return new WaitForSeconds(camHoldDuration);

        characterCam.Priority = new PrioritySettings { Value = 10 };
    }
}