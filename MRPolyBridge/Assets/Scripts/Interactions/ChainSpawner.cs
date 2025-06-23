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
            for (int i = 0; i < numberOfChains; i++)
            {
                Vector3 spawnPosition = Random.insideUnitSphere * spawnRadius;
                spawnPosition.y = spawnHeight;
                spawnPosition += spawnPosition;
                Instantiate(chainPrefab, spawnPosition, Quaternion.identity);
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