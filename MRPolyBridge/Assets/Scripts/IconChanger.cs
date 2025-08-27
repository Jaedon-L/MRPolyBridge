using UnityEngine;

public class IconChanger : MonoBehaviour
{
    [SerializeField] private GameObject wood;
    [SerializeField] private GameObject support;

    private bool toggle = false;

    [ContextMenu("toggle")]
    public void OnSupportToggle()
    {
        toggle = !toggle;
        wood.SetActive(!toggle);
        support.SetActive(toggle);
    }
}
