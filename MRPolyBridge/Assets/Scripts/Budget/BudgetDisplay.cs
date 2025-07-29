using UnityEngine;
using TMPro;

public class BudgetDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI budgetText;

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
        if (budgetText == null)
        {
            Debug.LogError("BudgetDisplay: budgetText is not assigned!");
            return;
        }
        budgetText.text = $"${newBudget:F1}";
    }
}
