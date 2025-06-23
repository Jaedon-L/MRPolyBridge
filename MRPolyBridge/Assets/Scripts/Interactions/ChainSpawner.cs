using System.Collections.Generic;
using UnityEngine;

namespace Interactions
{
    public class ChainSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject chainPrefab;
        [SerializeField] private int numberOfChains = 5;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private float spawnHeight = 1f;

        private List<Vector3> spawnPositions;

        private void Start()
        {
            spawnPositions = new List<Vector3>();
            for (int i = 0; i < numberOfChains; i++)
            {
                var spawnPosition = Random.insideUnitSphere * spawnRadius + transform.position +
                                    Vector3.up * spawnHeight;
                Instantiate(chainPrefab, spawnPosition, Quaternion.identity);
                spawnPositions.Add(spawnPosition);
            }
        }

        public void SpawnChains()
        {
            foreach (var spawnPosition in spawnPositions)
            {
                Instantiate(chainPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}