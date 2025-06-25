using System;
using UnityEngine;
using UnityEngine.Events;

public class BudgetManager : MonoBehaviour
{
    // Singleton instance
    public static BudgetManager Instance { get; private set; }

    [Header("Initial Budget Settings")]
    [Tooltip("Starting budget for the level.")]
    // [SerializeField] private float initialBudget = 100f;

    [Header("Cost Settings")]
    // [Tooltip("Cost for placing each node.")]
    [SerializeField] private float nodeCost = 5f;

    [Tooltip("Cost per unit length for main beams.")]
    [SerializeField] private float beamCostPerUnitLength = 2f;
    [Tooltip("Cost per unit length for support beams.")]
    [SerializeField] private float supportCostPerUnitLength = 1f;

    [Header("Refund Settings")]
    [Tooltip("Multiplier applied when refunding deleted objects. 1 = full refund, 0.5 = half refund, 0 = no refund.")]
    [Range(0f, 1f)]
    [SerializeField] private float refundMultiplier = 1f;

    public float currentBudget;

    /// <summary>
    /// Fired whenever the budget changes; float parameter is new budget.
    /// Subscribe UI display or other systems to this to update display.
    /// </summary>
    public UnityEvent<float> OnBudgetChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BudgetManager] Another instance exists, destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Persist if desired between scenes:
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Resets budget to a specified amount (e.g. at level start).
    /// </summary>
    public void ResetBudget(float amount)
    {
        currentBudget = amount;
        Debug.Log($"[BudgetManager] Budget reset to {currentBudget}");
        OnBudgetChanged?.Invoke(currentBudget);
    }

    /// <summary>
    /// Attempts to spend 'amount'. Returns true if successful; false if insufficient budget.
    /// </summary>
    public bool TrySpend(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning("[BudgetManager] TrySpend called with negative amount.");
            return false;
        }
        if (currentBudget >= amount)
        {
            currentBudget -= amount;
            Debug.Log($"[BudgetManager] Spent {amount}. Remaining budget: {currentBudget}");
            OnBudgetChanged?.Invoke(currentBudget);
            return true;
        }
        else
        {
            Debug.Log($"[BudgetManager] Insufficient budget. Tried to spend {amount}, but only {currentBudget} left.");
            // Optionally trigger a UI warning event here
            return false;
        }
    }

    /// <summary>
    /// Refunds 'amount' multiplied by refundMultiplier back into budget.
    /// </summary>
    public void Refund(float amount)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning("[BudgetManager] Refund called with non-positive amount.");
            return;
        }
        float refundAmount = amount * refundMultiplier;
        currentBudget += refundAmount;
        Debug.Log($"[BudgetManager] Refunded {refundAmount} (raw {amount} * multiplier {refundMultiplier}). New budget: {currentBudget}");
        OnBudgetChanged?.Invoke(currentBudget);
    }

    /// <summary>
    /// Returns current budget.
    /// </summary>
    public float GetCurrentBudget() => currentBudget;

    /// <summary>
    /// Accessors for cost parameters:
    /// </summary>
    public float NodeCost => nodeCost;
    public float BeamCostPerUnitLength => beamCostPerUnitLength;
    public float SupportCostPerUnitLength => supportCostPerUnitLength;
    public float RefundMultiplier => refundMultiplier;
}
