using UnityEngine;
using System.Collections.Generic;

public class DefenderPlacement : MonoBehaviour
{

//getting the references needed to place the defenders, get the spots to place them and play with the materials 
    public TerrainGeneration terrainGen;
    public GameObject [] defenderPrefabs;
    public float spaceRadius = 1f; 
    public Material highlightMaterial;
    public Material defaultMaterial;
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;
    public LayerMask placementLayer; 
    public bool isPlacing = false;
    public GameManager gameManager;
    public int defenderCost = 5;
    private int currentDefenderIndex = -1;

    //private variables, getting the tower in scene, making a list for the defender placements and object for the preview ghost thign 
    private Tower tower;
    private List<GameObject> defenderSpots = new List<GameObject>();
    private GameObject previewDefender;

    void Start() // running checks that the terrain and tower in the scene 
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

    //run on the defender button click, highlights the spots the defenders can be places and makes the preview 
    public void EnterPlacementMode(int defenderIndex)
    {
        if (defenderIndex < 0 || defenderIndex >= defenderPrefabs.Length)
        {
            Debug.LogError("Invalid defender index!");
            return;
        }

        currentDefenderIndex = defenderIndex;
        isPlacing = true;
        ShowValidSpots(true);

        // Remove old preview if any
        if (previewDefender) Destroy(previewDefender);

        // Spawn preview for selected defender
        previewDefender = Instantiate(defenderPrefabs[currentDefenderIndex]);
        SetPreviewMaterial(previewDefender, validPreviewMaterial);
    }

    //exits the placement mode, removes the highlights and destroys the preview object
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

        // player cna right click to exit the placement mode 
        if (Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }

        //using a raycast to follow the players mouse, see where they clicking and setting up a ghost to follow the 
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayer))
        {
            GameObject spot = hit.collider.gameObject;

            // Move preview defender to spot
            if (previewDefender)
            {
                previewDefender.transform.position = spot.transform.position;

                //checking if the player can afford the defender from the game maanger 
                bool canAfford = gameManager != null && gameManager.bones >= defenderCost;

                // making the preview ghost buddy when the spots good and the player has enough bones 
                if (IsSpotFree(spot) && canAfford)
                {
                        SetPreviewMaterial(previewDefender, validPreviewMaterial);
                }
                    
               
            }

            // placing defenders if conditions met and the player uses left click 
            if (Input.GetMouseButtonDown(0))
            {
                //checks if the place the player wants to place the defender is free first 
                if (IsSpotFree(spot))
                {
                    //checks if the player has enough bones and spawns them if true 
                    if (gameManager.SpendBones(defenderCost))
                    {
                        SpawnDefender(spot);
                        ShowValidSpots(true);
                    }
                    else
                    {
                        Debug.Log("Not enough bones to place defender!");
                    }
                }
            }
                //exiting the placement mode when the player doesnt have enough bones
            if(gameManager.GetBones() < defenderCost)
            {
                ExitPlacementMode();
            }
        }
    }

    //changes the mateials on the grass areas to show the spots the defenders can go, was set up in terrain gen to only be adjacent to the paths
// also checks if the player has enough bones cos the defender costs 5 and they cant place if they dont have 
    private void ShowValidSpots(bool enable)
    {
        foreach (GameObject spot in terrainGen.defenderAreas)
        {
            Renderer rend = spot.GetComponent<Renderer>();
            if (rend != null)
            {
                bool canAfford = (gameManager == null) || (gameManager != null && gameManager.bones >= defenderCost);
                rend.material = (enable && IsSpotFree(spot) && canAfford) ? highlightMaterial : defaultMaterial;
            }
        }
    }

//soawning the defender in the spot the player clicked on and adding it to the list
    private void SpawnDefender(GameObject place)
{
    if (currentDefenderIndex < 0 || currentDefenderIndex >= defenderPrefabs.Length)
    {
        Debug.LogError("No valid defender selected for placement!");
        return;
    }

    GameObject defender = Instantiate(defenderPrefabs[currentDefenderIndex], place.transform.position, Quaternion.identity);
    defenderSpots.Add(defender);
}

//running checks to see if a spot is free, uses the space radius so the player cant put too many defenders on top of each other and overload an area
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
                return false; 
            }
        }
        return true; 
    }

    //used to change the preview materials for if its a okay spot or not 
    private void SetPreviewMaterial(GameObject preview, Material mat)
    {
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = mat;
        }
    }

  
}