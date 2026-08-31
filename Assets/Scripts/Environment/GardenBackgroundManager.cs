using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GardenBackgroundManager : MonoBehaviour
{
    public static GardenBackgroundManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private bool autoBuildOnStart = true;
    [SerializeField] private float lawnSize = 65f;
    [SerializeField] private float patioMargin = 0.65f;
    [SerializeField] private float curbHeight = 0.12f;
    [SerializeField] private float curbThickness = 0.35f;

    [Header("Decoration Density")]
    [Range(4, 20)] [SerializeField] private int bushClusters = 10;
    [Range(6, 30)] [SerializeField] private int flowerPatches = 16;
    [Range(6, 25)] [SerializeField] private int pebbleGroups = 14;

    private Transform environmentHolder;
    private int currentGridWidth = 11;
    private int currentGridHeight = 15;
    private float currentCellSize = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }
        }
        Instance = this;

        LevelManager.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDestroy()
    {
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void Start()
    {
        if (autoBuildOnStart && (environmentHolder == null || environmentHolder.childCount == 0))
        {
            BuildEnvironment(currentGridWidth, currentGridHeight, currentCellSize);
        }
    }

    private void HandleLevelLoaded(Level level)
    {
        if (level == null) return;
        BuildEnvironment(level.GridWidth, level.GridHeight, level.CellSize);
    }

    /// <summary>
    /// Constructs or updates the full garden patio environment to frame the board grid.
    /// </summary>
    public void BuildEnvironment(int gridWidth, int gridHeight, float cellSize)
    {
        currentGridWidth = gridWidth;
        currentGridHeight = gridHeight;
        currentCellSize = cellSize;

        ClearEnvironment();

        environmentHolder = new GameObject("EnvironmentVisuals").transform;
        environmentHolder.SetParent(transform, false);

        // 1. Expansive Lawn Base
        CreateLawn();

        // 2. Stone Patio Foundation & Rim Curb
        CreatePatio(gridWidth, gridHeight, cellSize);

        // 3. Garden Elements (Bushes, Flowers, River Pebbles, Stepping Stones)
        CreateGardenDecorations(gridWidth, gridHeight, cellSize);
    }

    private void ClearEnvironment()
    {
        if (environmentHolder != null)
        {
            if (Application.isPlaying)
            {
                Destroy(environmentHolder.gameObject);
            }
            else
            {
                DestroyImmediate(environmentHolder.gameObject);
            }
            environmentHolder = null;
        }

        // Clean any leftover direct children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreateLawn()
    {
        GameObject lawnObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lawnObj.name = "GardenLawn";
        lawnObj.transform.SetParent(environmentHolder, false);
        lawnObj.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        lawnObj.transform.localScale = new Vector3(lawnSize * 0.1f, 1f, lawnSize * 0.1f);

        RemoveCollider(lawnObj);

        var renderer = lawnObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.GrassGreen, 0.05f);
            renderer.receiveShadows = true;
        }
    }

    private void CreatePatio(int width, int height, float cellSize)
    {
        float boardW = width * cellSize;
        float boardH = height * cellSize;

        float patioW = boardW + (patioMargin * 2f);
        float patioH = boardH + (patioMargin * 2f);

        // --- Patio Stone Slab ---
        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "PatioStoneSlab";
        slab.transform.SetParent(environmentHolder, false);
        slab.transform.localPosition = new Vector3(0f, -0.09f, 0f);
        slab.transform.localScale = new Vector3(patioW, 0.12f, patioH);

        RemoveCollider(slab);

        var slabRenderer = slab.GetComponent<MeshRenderer>();
        if (slabRenderer != null)
        {
            slabRenderer.material = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.PatioBase, 0.15f);
            slabRenderer.receiveShadows = true;
        }

        // --- Stone Curbs / Raised Rim Frame ---
        Material curbMat = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.PatioBorder, 0.2f);

        float halfW = patioW * 0.5f;
        float halfH = patioH * 0.5f;

        // Top curb
        CreateCurbPiece("Curb_North", new Vector3(0f, -0.03f, halfH - (curbThickness * 0.5f)),
            new Vector3(patioW, curbHeight, curbThickness), curbMat);

        // Bottom curb
        CreateCurbPiece("Curb_South", new Vector3(0f, -0.03f, -halfH + (curbThickness * 0.5f)),
            new Vector3(patioW, curbHeight, curbThickness), curbMat);

        // Left curb
        CreateCurbPiece("Curb_West", new Vector3(-halfW + (curbThickness * 0.5f), -0.03f, 0f),
            new Vector3(curbThickness, curbHeight, patioH - (curbThickness * 2f)), curbMat);

        // Right curb
        CreateCurbPiece("Curb_East", new Vector3(halfW - (curbThickness * 0.5f), -0.03f, 0f),
            new Vector3(curbThickness, curbHeight, patioH - (curbThickness * 2f)), curbMat);

        // Corner posts for architectural finish
        float postSize = curbThickness * 1.25f;
        CreateCornerPost("Corner_NW", new Vector3(-halfW + (curbThickness * 0.5f), -0.02f, halfH - (curbThickness * 0.5f)), postSize, curbMat);
        CreateCornerPost("Corner_NE", new Vector3(halfW - (curbThickness * 0.5f), -0.02f, halfH - (curbThickness * 0.5f)), postSize, curbMat);
        CreateCornerPost("Corner_SW", new Vector3(-halfW + (curbThickness * 0.5f), -0.02f, -halfH + (curbThickness * 0.5f)), postSize, curbMat);
        CreateCornerPost("Corner_SE", new Vector3(halfW - (curbThickness * 0.5f), -0.02f, -halfH + (curbThickness * 0.5f)), postSize, curbMat);
    }

    private void CreateCurbPiece(string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        curb.name = name;
        curb.transform.SetParent(environmentHolder, false);
        curb.transform.localPosition = localPos;
        curb.transform.localScale = scale;

        RemoveCollider(curb);

        var ren = curb.GetComponent<MeshRenderer>();
        if (ren != null)
        {
            ren.material = mat;
            ren.receiveShadows = true;
        }
    }

    private void CreateCornerPost(string name, Vector3 localPos, float size, Material mat)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = name;
        post.transform.SetParent(environmentHolder, false);
        post.transform.localPosition = localPos;
        post.transform.localScale = new Vector3(size, curbHeight * 0.65f, size);

        RemoveCollider(post);

        var ren = post.GetComponent<MeshRenderer>();
        if (ren != null)
        {
            ren.material = mat;
            ren.receiveShadows = true;
        }
    }

    private void CreateGardenDecorations(int width, int height, float cellSize)
    {
        float boardW = width * cellSize;
        float boardH = height * cellSize;
        float minX = -(boardW * 0.5f) - patioMargin - 0.4f;
        float maxX = (boardW * 0.5f) + patioMargin + 0.4f;
        float minZ = -(boardH * 0.5f) - patioMargin - 0.4f;
        float maxZ = (boardH * 0.5f) + patioMargin + 0.4f;

        Transform decorRoot = new GameObject("GardenDecorations").transform;
        decorRoot.SetParent(environmentHolder, false);

        // Deterministic pseudo-random seed for repeatable aesthetics
        Random.InitState(width * 100 + height * 17);

        // Pre-create shared materials for performance
        Material matFoliageDark = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FoliageDark, 0.1f);
        Material matFoliageMed = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FoliageMedium, 0.1f);
        Material matFoliageLight = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FoliageLight, 0.1f);
        Material[] foliageMats = { matFoliageDark, matFoliageMed, matFoliageLight };

        Material matPebbleL = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.PebbleLight, 0.4f);
        Material matPebbleD = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.PebbleDark, 0.4f);

        Material[] flowerMats =
        {
            GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FlowerRed, 0.3f),
            GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FlowerYellow, 0.3f),
            GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FlowerBlue, 0.3f),
            GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FlowerPurple, 0.3f),
            GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FlowerWhite, 0.3f)
        };

        // 1. Shrub & Bush Clusters along perimeter
        for (int i = 0; i < bushClusters; i++)
        {
            Vector3 pos = GetPerimeterPosition(minX, maxX, minZ, maxZ, 0.6f, 2.5f);
            CreateBushCluster(decorRoot, pos, foliageMats);
        }

        // 2. Flower Patches nestled near bushes and patio
        for (int i = 0; i < flowerPatches; i++)
        {
            Vector3 pos = GetPerimeterPosition(minX, maxX, minZ, maxZ, 0.3f, 2.0f);
            CreateFlowerPatch(decorRoot, pos, flowerMats);
        }

        // 3. River Pebble Clusters
        for (int i = 0; i < pebbleGroups; i++)
        {
            Vector3 pos = GetPerimeterPosition(minX, maxX, minZ, maxZ, 0.2f, 1.8f);
            CreatePebbleCluster(decorRoot, pos, matPebbleL, matPebbleD);
        }

        // 4. Stepping Stones leading off bottom corner
        CreateSteppingStonesPath(decorRoot, minX, minZ);
    }

    private Vector3 GetPerimeterPosition(float minX, float maxX, float minZ, float maxZ, float minOffset, float maxOffset)
    {
        // 0: North, 1: South, 2: West, 3: East
        int side = Random.Range(0, 4);
        float offset = Random.Range(minOffset, maxOffset);

        float x = 0f;
        float z = 0f;

        switch (side)
        {
            case 0: // North
                x = Random.Range(minX - 1f, maxX + 1f);
                z = maxZ + offset;
                break;
            case 1: // South
                x = Random.Range(minX - 1f, maxX + 1f);
                z = minZ - offset;
                break;
            case 2: // West
                x = minX - offset;
                z = Random.Range(minZ - 1f, maxZ + 1f);
                break;
            case 3: // East
                x = maxX + offset;
                z = Random.Range(minZ - 1f, maxZ + 1f);
                break;
        }

        return new Vector3(x, -0.1f, z);
    }

    private void CreateBushCluster(Transform parent, Vector3 centerPos, Material[] foliageMats)
    {
        GameObject cluster = new GameObject("BushCluster");
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = centerPos;

        int spheresCount = Random.Range(3, 6);
        float baseScale = Random.Range(0.7f, 1.25f);

        for (int i = 0; i < spheresCount; i++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"Puff_{i}";
            puff.transform.SetParent(cluster.transform, false);

            Vector3 offset = (i == 0)
                ? Vector3.zero
                : new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.05f, 0.2f), Random.Range(-0.35f, 0.35f));

            puff.transform.localPosition = offset;
            float puffScale = baseScale * Random.Range(0.65f, 1.0f);
            puff.transform.localScale = new Vector3(puffScale * 1.1f, puffScale * 0.85f, puffScale * 1.1f);

            RemoveCollider(puff);

            var ren = puff.GetComponent<MeshRenderer>();
            if (ren != null)
            {
                ren.material = foliageMats[Random.Range(0, foliageMats.Length)];
                ren.receiveShadows = true;
            }
        }
    }

    private void CreateFlowerPatch(Transform parent, Vector3 centerPos, Material[] flowerMats)
    {
        GameObject patch = new GameObject("FlowerPatch");
        patch.transform.SetParent(parent, false);
        patch.transform.localPosition = centerPos;

        int flowerCount = Random.Range(2, 5);
        Material chosenFlowerMat = flowerMats[Random.Range(0, flowerMats.Length)];

        for (int i = 0; i < flowerCount; i++)
        {
            Vector3 localOffset = new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f));

            // Little stem/leaf base
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(patch.transform, false);
            stem.transform.localPosition = localOffset + new Vector3(0f, 0.05f, 0f);
            stem.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);
            RemoveCollider(stem);
            var stemRen = stem.GetComponent<MeshRenderer>();
            if (stemRen != null)
            {
                stemRen.material = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.FoliageDark, 0.1f);
            }

            // Flower blossom head
            GameObject blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blossom.name = "Blossom";
            blossom.transform.SetParent(patch.transform, false);
            blossom.transform.localPosition = localOffset + new Vector3(0f, 0.12f, 0f);
            float flowerScale = Random.Range(0.14f, 0.22f);
            blossom.transform.localScale = new Vector3(flowerScale, flowerScale * 0.65f, flowerScale);
            RemoveCollider(blossom);

            var blossomRen = blossom.GetComponent<MeshRenderer>();
            if (blossomRen != null)
            {
                blossomRen.material = chosenFlowerMat;
            }
        }
    }

    private void CreatePebbleCluster(Transform parent, Vector3 centerPos, Material lightMat, Material darkMat)
    {
        GameObject group = new GameObject("PebbleGroup");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = centerPos;

        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            GameObject pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pebble.name = "Pebble";
            pebble.transform.SetParent(group.transform, false);
            pebble.transform.localPosition = new Vector3(Random.Range(-0.2f, 0.2f), -0.03f, Random.Range(-0.2f, 0.2f));

            float s = Random.Range(0.12f, 0.26f);
            pebble.transform.localScale = new Vector3(s * 1.3f, s * 0.5f, s * 1.1f);
            pebble.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            RemoveCollider(pebble);

            var ren = pebble.GetComponent<MeshRenderer>();
            if (ren != null)
            {
                ren.material = (i % 2 == 0) ? lightMat : darkMat;
                ren.receiveShadows = true;
            }
        }
    }

    private void CreateSteppingStonesPath(Transform parent, float startX, float startZ)
    {
        GameObject pathRoot = new GameObject("SteppingStones");
        pathRoot.transform.SetParent(parent, false);

        Material paverMat = GardenEnvironmentPalette.CreateLitMaterial(GardenEnvironmentPalette.PatioBorder, 0.3f);

        Vector3 currentPos = new Vector3(startX - 0.5f, -0.12f, startZ - 0.4f);
        Vector3 direction = new Vector3(-0.9f, 0f, -0.7f).normalized;

        for (int i = 0; i < 4; i++)
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = $"SteppingStone_{i}";
            stone.transform.SetParent(pathRoot.transform, false);
            stone.transform.localPosition = currentPos;

            float stoneDiam = Random.Range(0.65f, 0.85f);
            stone.transform.localScale = new Vector3(stoneDiam, 0.05f, stoneDiam * 0.9f);
            stone.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

            RemoveCollider(stone);

            var ren = stone.GetComponent<MeshRenderer>();
            if (ren != null)
            {
                ren.material = paverMat;
                ren.receiveShadows = true;
            }

            currentPos += direction * 0.95f;
        }
    }

    private void RemoveCollider(GameObject obj)
    {
        var col = obj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    [ContextMenu("Rebuild Environment Now")]
    public void RebuildFromInspector()
    {
        BuildEnvironment(currentGridWidth, currentGridHeight, currentCellSize);
    }
}
