using UnityEngine;
using TMPro;

public class BudgetDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text budgetText;

    private void OnEnable()
    {
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.OnBudgetChanged.AddListener(UpdateBudgetText);
    }

    private void OnDisable()
    {
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.OnBudgetChanged.RemoveListener(UpdateBudgetText);
    }

    public void UpdateBudgetText(float newBudget)
    {
        budgetText.text = $"Budget: {newBudget:F1}";
    }
}
