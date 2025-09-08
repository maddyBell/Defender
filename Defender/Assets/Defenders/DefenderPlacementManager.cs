using UnityEngine;
using System.Collections.Generic;

public class DefenderPlacement : MonoBehaviour
{
    [Header("References")]
    public TerrainGeneration terrainGen;
    private Tower tower;
    public GameObject defenderPrefab;

    [Header("Placement Settings")]
    public float spaceRadius = 1f; // no overlap
    public Material highlightMaterial;
    public Material defaultMaterial;
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;

    [Header("Raycast Settings")]
    public LayerMask placementLayer; // Assign "PlacementSpot" layer in inspector

    private bool isPlacing = false;
    private List<GameObject> defenderSpots = new List<GameObject>();
    private GameObject previewDefender;

    void Start()
    {
        if (!terrainGen)
        {
            Debug.LogError("DefenderPlacement: Missing TerrainGeneration reference!");
            return;
        }

        tower = FindObjectOfType<Tower>();
        if (!tower)
        {
            Debug.LogError("DefenderPlacement: No Tower found in scene!");
        }
    }

    public void EnterPlacementMode()
    {
        isPlacing = true;
        ShowValidSpots(true);

        // Spawn preview defender if it doesn’t exist
        if (!previewDefender)
        {
            previewDefender = Instantiate(defenderPrefab);
            SetPreviewMaterial(previewDefender, validPreviewMaterial);
        }
    }

    public void ExitPlacementMode()
    {
        isPlacing = false;
        ShowValidSpots(false);
        if (previewDefender) Destroy(previewDefender);
    }

    void Update()
    {
        if (!isPlacing) return;

        if (!tower)
        {
            tower = FindObjectOfType<Tower>();
            if (!tower) return;
        }

        if (!tower.attackRadiusCollider)
        {
            Debug.LogError("Tower attackRadiusCollider is missing!");
            return;
        }

        // Right-click cancels
        if (Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }

        // Raycast only against placement spots
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayer))
        {
            GameObject spot = hit.collider.gameObject;

            // Move preview defender to spot
            if (previewDefender)
            {
                previewDefender.transform.position = spot.transform.position;

                // Change material based on validity
                if (IsSpotFree(spot))
                    SetPreviewMaterial(previewDefender, validPreviewMaterial);
                else
                    SetPreviewMaterial(previewDefender, invalidPreviewMaterial);
            }

            // Place real defender on left click if valid
            if (Input.GetMouseButtonDown(0))
            {
                if (IsSpotFree(spot))
                {
                    SpawnDefender(spot);
                    ShowValidSpots(true); // refresh highlights
                }
                
            }
        }
    }

    private void ShowValidSpots(bool enable)
    {
        foreach (GameObject spot in terrainGen.defenderAreas)
        {
            Renderer rend = spot.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = (enable && IsSpotFree(spot)) ? highlightMaterial : defaultMaterial;
            }
        }
    }

    private void SpawnDefender(GameObject place)
    {
        GameObject defender = Instantiate(defenderPrefab, place.transform.position, Quaternion.identity);
        defenderSpots.Add(defender);
    }

    bool IsSpotFree(GameObject spot)
    {
        foreach (GameObject existingDefender in defenderSpots)
        {
            Collider col = existingDefender.GetComponent<Collider>();
            if (col == null) continue;

            float distance = Vector3.Distance(spot.transform.position, col.transform.position);
            float combinedRadius = col.bounds.extents.x + spaceRadius;

            if (distance < combinedRadius)
            {
                return false; // Spot is too close
            }
        }
        return true; // Spot is free
    }

    private void SetPreviewMaterial(GameObject preview, Material mat)
    {
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = mat;
        }
    }

    public void RemoveDefender(GameObject defender)
{
    if (defenderSpots.Contains(defender))
    {
        defenderSpots.Remove(defender);

        // Refresh highlights after removal
        ShowValidSpots(true);
    }
}
}