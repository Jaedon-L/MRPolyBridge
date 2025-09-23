using System.Collections;
using UnityEngine;

public class notification : MonoBehaviour
{
    [SerializeField] private float timer = 5f; 
    void Start()
    {
        StartCoroutine("TurnOffTimer"); 
    }

    private IEnumerator TurnOffTimer()
    {
        yield return new WaitForSeconds(timer);
        gameObject.SetActive(false); 
    }

}
