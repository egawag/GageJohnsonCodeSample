using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PlayerManager : MonoBehaviour
{
    public WorldManager WM;
    public EnemyManager EM;
    public UIManager UIM;

    public Camera maincam;



    float cameraMaxZoom;
    float cameraStartingYPos;
    public bool started;
    public bool claimMode;// activated one player clicks claim button
    List<PixelScript> territories = new List<PixelScript>();
    List<PixelScript> borderingTerritories = new List<PixelScript>();// all hexs that are bordering a territory tile, and are not walls, and are not corrupted
    Dictionary<PixelScript, string> beingBuilt = new Dictionary<PixelScript, string>();// string represents build type for example territory, tower, turret

    Dictionary<PixelScript, GameObject> outlinesCurrentlyOut = new Dictionary<PixelScript, GameObject>();
    public int energy;
    public int food;
    public int ether;
    public int flux;
    public Sprite[] resourceIconSprites = new Sprite[4];

    [Header("prefabs")]
    [SerializeField]
    GameObject selectedOutline;

    [Header("variables")]
    [SerializeField]
    float scrollSpeed;
    [SerializeField]
    float cameraMoveSpeed;
    [SerializeField]
    Vector3 startingPosition;
    [SerializeField]
    Color territoryColor;
    [SerializeField]
    Color wallColor;
    [SerializeField]
    Material territoryMat;
    [SerializeField]
    Material homeBaseMat;
    [SerializeField]
    Material neutralTileMat;


    void Start()
    {
        cameraMaxZoom = maincam.orthographicSize;
        cameraStartingYPos = maincam.transform.position.y;
    }

    void Update()
    {
        Controls();
        if (WM.isMapGenerating == false && started == false)
        {
            SetUp();
        }
        if (started)
        {
            Tick();
        }
    }

    void Tick()
    {
        if (claimMode == true)
        {
            UpdateOutlines(borderingTerritories);
            if (Input.GetMouseButtonDown(0))
            {
                PixelScript pixel = PixelClicked((PixelScript p) => borderingTerritories.Contains(p) && p.gps.beingBuilt == false);
                if (pixel != null)
                {
                    if (energy >= 5)
                    {
                        ClaimTerritory(pixel, "territory");
                    }
                    else
                    {
                        UIM.NotEnoughEnergy();
                    }
                }
            }
        }
    }

    void ClaimTerritory(PixelScript pixel, string type = "territory")// Was clicked
    {
        energy -= 5;
        beingBuilt[pixel] = type;
        pixel.gps.beingBuilt = true;
        borderingTerritories.Remove(pixel);
        StartCoroutine(BuildDelayTemp(pixel, type));
        // Start building
    }

    IEnumerator BuildDelayTemp(PixelScript pixel, string type)
    {
        float i = 0.0f;
        while (i < 1.0)
        {
            i += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        beingBuilt.Remove(pixel);
        pixel.gps.beingBuilt = false;
        AddTerritory(pixel, type);
    }
    public static bool IsOverUI()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);

        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.GetComponent<CanvasRenderer>())
                {
                    return true;
                }
            }
        }

        return false;
    }
    PixelScript PixelClicked(Func<PixelScript, bool> predicate)
    {

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (IsOverUI() == false && hit.collider != null && hit.collider.tag == "pixel" && predicate(hit.collider.gameObject.GetComponent<PixelScript>()))
            {
                return hit.collider.gameObject.GetComponent<PixelScript>();
            }
        }
        return null;
    }
    PixelScript PixelClicked()// overload
    {
        return (PixelClicked((PixelScript p) => true));
    }

    void SetUp()
    {
        foreach (var pixel in WM.pixels)
        {
            if (pixel.Value.trueColor == wallColor)
            {
                pixel.Value.gps.isWall = true;
                pixel.Value.transform.localScale = new Vector3(1, 1, UnityEngine.Random.Range(3, 7));
            }
        }
        started = true;
        PixelScript startingPixel = WM.pixels[startingPosition];
        AddTerritory(startingPixel, "homeBase");
        EM.StartCorruption();
    }

    public void ClaimModeSetUp()
    {
        if (claimMode == false)
        {
            claimMode = true;
            // 1 make the button animate
            StartCoroutine(UIM.ClaimButtonAnimation());

            // 2 highlight all of the claim apple territory in the borderingTerritories
            CreateOutlines(borderingTerritories);

        }
    }

    public void ClaimModeStop()
    {
        claimMode = false;
        DeleteOutlines();
    }
    public void UpdateOutlines(List<PixelScript> pixelsToOutline)
    {
        //remove all outlines that are no longer in pixelsToOutline
        var outlinesToRemove = new List<PixelScript>();
        foreach (var pixel in outlinesCurrentlyOut.Keys)
        {
            if (pixelsToOutline.Contains(pixel) == false)
            {
                outlinesToRemove.Add(pixel);
                StartCoroutine(outlinesCurrentlyOut[pixel].GetComponent<outline>().Shrink());
            }
        }
        foreach (var pixel in outlinesToRemove)
        {
            outlinesCurrentlyOut.Remove(pixel);
        }
        CreateOutlines(pixelsToOutline);
    }
    public void CreateOutlines(List<PixelScript> pixelsToOutline)
    {
        foreach (var pixel in pixelsToOutline)
        {
            CreateOutline(pixel);
        }
    }
    public void CreateOutline(PixelScript pixel)
    {
        if (outlinesCurrentlyOut.ContainsKey(pixel) == false)
        {
            var outline = Instantiate(selectedOutline, new Vector3(pixel.transform.position.x, pixel.transform.position.y, -PixelScript.heightAdjust), Quaternion.identity);
            outlinesCurrentlyOut.Add(pixel, outline);
        }
    }

    public void DeleteOutlines()
    {
        foreach (var outline in outlinesCurrentlyOut.Values)
        {
            StartCoroutine(outline.GetComponent<outline>().Shrink());
        }
        outlinesCurrentlyOut.Clear();
    }

    void AddTerritory(PixelScript pixel, string type = "territory")// was finished building/claiming
    {
        pixel.gps.territory = true;
        territories.Add(pixel);
        borderingTerritories.Remove(pixel);
        //pixel.ChangeColorBasic(territoryColor);
        if (type == "territory")
        {
            pixel.mr.material = territoryMat;
        }
        else if (type == "homeBase")
        {
            pixel.mr.material = homeBaseMat;
            pixel.transform.SetSiblingIndex(0);
            pixel.gps.homeBase = true;
        }
        CheckAddRemoveBorderingNeighbors(pixel);
    }

    public void RemoveTerritory(PixelScript pixel)
    {
        territories.Remove(pixel);
        pixel.gps.SetAllTerritoryRelatedFalse();
        CheckAddRemoveBorderingNeighbors(pixel);
    }

    public void CheckAddRemoveBorderingNeighbors(PixelScript pixel)
    {
        foreach (var n in pixel.neighbors)
        {
            CheckAddRemoveBordering(n);
        }
    }
    public void CheckAddRemoveBordering(PixelScript pixel)
    {
        if (pixel != null && pixel.gps.IsAllFalseExcBeingCorrupted())
        {
            bool hasTerritoryNeighbor = false;
            foreach (var t in pixel.neighbors)
            {
                if (t != null && t.gps.territory)
                {
                    hasTerritoryNeighbor = true;
                    break;
                }
            }
            if (hasTerritoryNeighbor && borderingTerritories.Contains(pixel) == false)
            {
                borderingTerritories.Add(pixel);
            }
            else if (hasTerritoryNeighbor == false && borderingTerritories.Contains(pixel))
            {
                borderingTerritories.Remove(pixel);
            }
        }
        else if (pixel != null && pixel.gps.IsAllFalseExcBeingCorrupted() == false && borderingTerritories.Contains(pixel))
        {
            borderingTerritories.Remove(pixel);
        }
    }


    void Controls()
    {
        //controls
        maincam.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        if (maincam.orthographicSize <= 2)
        {
            maincam.orthographicSize = 2;
        }
        else if (maincam.orthographicSize > cameraMaxZoom)
        {
            maincam.orthographicSize = cameraMaxZoom;
        }

        if (Input.GetKey(KeyCode.W))
        {
            if (maincam.transform.position.y < (cameraMaxZoom - maincam.orthographicSize - Mathf.Abs(cameraStartingYPos)))
            {
                maincam.transform.Translate(Vector3.up * Time.deltaTime * cameraMoveSpeed * (maincam.orthographicSize + 20));
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (maincam.transform.position.x < (cameraMaxZoom - maincam.orthographicSize))
            {
                maincam.transform.Translate(Vector3.right * Time.deltaTime * cameraMoveSpeed * (maincam.orthographicSize + 20));
            }
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (maincam.transform.position.y > -(cameraMaxZoom - maincam.orthographicSize + Mathf.Abs(cameraStartingYPos)))
            {
                maincam.transform.Translate(Vector3.down * Time.deltaTime * cameraMoveSpeed * (maincam.orthographicSize + 20));
            }
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (maincam.transform.position.x > -(cameraMaxZoom - maincam.orthographicSize))
            {
                maincam.transform.Translate(Vector3.left * Time.deltaTime * cameraMoveSpeed * (maincam.orthographicSize + 20));
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
