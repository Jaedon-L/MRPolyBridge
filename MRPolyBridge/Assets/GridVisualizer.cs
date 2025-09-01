using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public Vector3 gridSize = new Vector3(1f, 1f, 1f); // Half-size in each direction
    public float cellSize = 0.075f;                     // Distance between lines
    public float pointSize = 0.01f;                     // Size of intersection markers
    public bool drawPoints = true;

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        origin.y = 0; // Keep floor alignment

        // Calculate start and end positions (negative to positive)
        Vector3 start = origin - gridSize;
        start.y = 0;
        Vector3 end = origin + gridSize;

        // Draw horizontal slices at each Y level
        for (float y = origin.y; y <= end.y; y += cellSize)
        {
            // X-axis lines (Red)
            Gizmos.color = Color.red;
            for (float x = start.x; x <= end.x; x += cellSize)
            {
                Gizmos.DrawLine(new Vector3(x, y, start.z), new Vector3(x, y, end.z));
            }

            // Z-axis lines (Blue)
            Gizmos.color = Color.blue;
            for (float z = start.z; z <= end.z; z += cellSize)
            {
                Gizmos.DrawLine(new Vector3(start.x, y, z), new Vector3(end.x, y, z));
            }

            // Points (Yellow)
            if (drawPoints)
            {
                Gizmos.color = Color.yellow;
                for (float x = start.x; x <= end.x; x += cellSize)
                {
                    for (float z = start.z; z <= end.z; z += cellSize)
                    {
                        Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one * pointSize);
                    }
                }
            }
        }
    }
}
