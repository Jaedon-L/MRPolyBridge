using UnityEngine;

public class LevelEndTrigger : MonoBehaviour
{
    private string _carTag = "Car";
    private System.Action _onComplete = null;

    /// <summary>
    /// Call this right after instantiating a LevelEndTrigger so it knows
    /// which tag to watch for, and what callback to invoke when triggered.
    /// </summary>
    public void Initialize(string carTag, System.Action onComplete)
    {
        _carTag = carTag;
        _onComplete = onComplete;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_carTag))
        {
            // Car has entered the finish zone
            _onComplete?.Invoke();
        }
    }
}
