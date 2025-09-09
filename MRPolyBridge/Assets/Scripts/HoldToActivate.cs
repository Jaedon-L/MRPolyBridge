using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HoldToActivate : MonoBehaviour
{
    [Tooltip("Seconds required to hold before activation")]
    public float holdDuration = 1.0f;

    [Tooltip("Radial UI Image (Type = Filled, Fill Method = Radial360)")]
    public Image radialImage;

    [Tooltip("Invoked once when hold completes")]
    public UnityEvent onActivated;

    Coroutine holdCoroutine;
    float elapsed;


    // Called from InteractableUnityEventWrapper.WhenSelect (start the hold)
    public void StartHold()
    {
        if (holdCoroutine == null)
            holdCoroutine = StartCoroutine(HoldRoutine());
    }

    // Called from InteractableUnityEventWrapper.WhenUnselect (cancel the hold)
    public void CancelHold()
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
        ResetVisual();
    }

    IEnumerator HoldRoutine()
    {
        elapsed = 0f;
        SetFill(0f);
        // Use unscaled time so UI won't be affected by timeScale changes (optional)
        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFill(Mathf.Clamp01(elapsed / holdDuration));
            yield return null;
        }

        // Completed
        holdCoroutine = null;
        SetFill(1f);
        onActivated?.Invoke();
        ResetVisual();
    }

    void SetFill(float v)
    {
        if (radialImage != null)
            radialImage.fillAmount = v;
    }

    void ResetVisual()
    {
        elapsed = 0f;
        SetFill(0f);
    }
}
