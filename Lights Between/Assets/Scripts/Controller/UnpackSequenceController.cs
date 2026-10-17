using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnpackSequenceController : MonoBehaviour
{
    [Header("Visuals To Hide On Unpack")]
    [Tooltip("Drag the bag visual GameObjects that were enabled during snapping here.")]
    [SerializeField] private List<GameObject> placedBagVisuals = new List<GameObject>();

    [Header("Sequence Timing & Dialogue")]
    [SerializeField] private float blackoutHoldDuration = 0.5f;
    [SerializeField] private DialogueSO postUnpackDialogue;

    public void TriggerUnpackSequence()
    {
        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.DoBlinkTransition(OnBlackoutStateReached);
        }
        else
        {
            OnBlackoutStateReached();
        }
    }

    private void OnBlackoutStateReached()
    {
        HidePlacedBags();

        StartCoroutine(PostBlackoutRoutine());
    }

    private void HidePlacedBags()
    {
        foreach (GameObject bag in placedBagVisuals)
        {
            if (bag != null)
            {
                bag.SetActive(false);
            }
        }
    }

    private IEnumerator PostBlackoutRoutine()
    {
        yield return new WaitForSeconds(blackoutHoldDuration);

        if (DialogueManager.Instance != null && postUnpackDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(postUnpackDialogue);
        }
    }
}