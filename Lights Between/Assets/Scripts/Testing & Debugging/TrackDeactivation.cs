using UnityEngine;

public class TrackDeactivation : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log($"[DEACTIVATION DETECTED] {gameObject.name} was disabled!\nCallstack:\n{System.Environment.StackTrace}");
    }

    private void OnDestroy()
    {
        Debug.Log($"[DESTRUCTION DETECTED] {gameObject.name} was destroyed!\nCallstack:\n{System.Environment.StackTrace}");
    }
}