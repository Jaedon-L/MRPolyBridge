using System.Collections;
using UnityEngine;

public class DestroyOnTimer : MonoBehaviour
{
    [SerializeField] private float timer = 7f; 
    void Start()
    {
        StartCoroutine("TurnOffTimer"); 
    }

    private IEnumerator TurnOffTimer()
    {
        yield return new WaitForSeconds(timer);
        Destroy(gameObject); 
    }
}
