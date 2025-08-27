using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI; // For ScrollRect

public class ButtonVisibilityManager : MonoBehaviour
{
    [Header("Scroll View References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    private PokeInteractable[] pokeButtons;

    private void Start()
    {
        // Cache all PokeInteractables in the content
        pokeButtons = content.GetComponentsInChildren<PokeInteractable>(true);
        UpdateButtonVisibility();

        // Update when scrolling occurs
        scrollRect.onValueChanged.AddListener((_) => UpdateButtonVisibility());
    }

    private void UpdateButtonVisibility()
    {
        foreach (var button in pokeButtons)
        {
            if (button == null) continue;

            RectTransform buttonRect = button.GetComponent<RectTransform>();

            // Convert button position to viewport space
            Vector3 worldPos = buttonRect.position;
            Vector3 localPos = viewport.InverseTransformPoint(worldPos);

            // Check bounds of viewport
            bool isVisible = Mathf.Abs(localPos.y) <= (viewport.rect.height / 2)
                             && Mathf.Abs(localPos.x) <= (viewport.rect.width / 2);

            button.gameObject.SetActive(isVisible);
        }
    }
}
