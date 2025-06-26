using UnityEngine;
using Oculus.Interaction.Samples;

public class CarResetButton : MonoBehaviour
{
    public void ResetCar()
    {
        var carRespawner = FindFirstObjectByType<RespawnOnDrop>();
        if (carRespawner != null)
        {
            carRespawner.Respawn();
            Debug.Log("[CarResetButton] Car respawned.");
        }
        else
        {
            Debug.LogWarning("[CarResetButton] No RespawnOnDrop found.");
        }
    }
}
