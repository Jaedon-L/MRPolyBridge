using UnityEngine;
using System.Collections.Generic;

public class LevelPageManager : MonoBehaviour
{
    [Header("Level Setup")]
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Transform pagesContainer;
    [SerializeField] private int buttonsPerPage = 8;

    [SerializeField] private List<GameObject> pages = new List<GameObject>();
    private int currentPageIndex = 0;


    public void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Count) return;

        // Hide all pages
        foreach (var page in pages)
            page.SetActive(false);

        // Show selected page
        pages[index].SetActive(true);
        currentPageIndex = index;
    }
    [ContextMenu("nextpage")]
    public void NextPage()
    {
        if (currentPageIndex < pages.Count - 1)
            ShowPage(currentPageIndex + 1);
    }

    [ContextMenu("prevpage")]
    public void PreviousPage()
    {
        if (currentPageIndex > 0)
            ShowPage(currentPageIndex - 1);
    }
}
