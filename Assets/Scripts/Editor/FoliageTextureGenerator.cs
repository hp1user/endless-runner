using UnityEngine;
using UnityEditor;
using System.IO;

public static class FoliageTextureGenerator
{
    private const string FOLDER_PATH = "Assets/3d/Environment/Foliage/Textures";

    [MenuItem("Tools/Endless Runner/Generate Foliage Textures")]
    public static void GenerateAllFoliageTextures()
    {
        if (!Directory.Exists(FOLDER_PATH))
        {
            Directory.CreateDirectory(FOLDER_PATH);
        }

        GenerateMossTextures();
        GenerateVineTextures();

        AssetDatabase.Refresh();
        ConfigureTextureSettings();

        Debug.Log("<color=green><b>[Foliage Generator]</b></color> Successfully generated seamless Moss and Vine textures!");
    }

    private static void GenerateMossTextures()
    {
        int size = 512;
        Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, true);

        float[,] heightMap = new float[size, size];

        // 1. Generate multi-octave seamless noise heightmap
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                // 4D seamless noise torus mapping
                float pi2 = Mathf.PI * 2f;
                float nx = Mathf.Cos(u * pi2) * 1.5f;
                float ny = Mathf.Sin(u * pi2) * 1.5f;
                float nz = Mathf.Cos(v * pi2) * 1.5f;
                float nw = Mathf.Sin(v * pi2) * 1.5f;

                float n1 = Mathf.PerlinNoise(nx + 10f, nz + 10f);
                float n2 = Mathf.PerlinNoise(ny * 2f + 20f, nw * 2f + 20f) * 0.5f;
                float n3 = Mathf.PerlinNoise(nx * 4f + 30f, nz * 4f + 30f) * 0.25f;

                float h = Mathf.Clamp01((n1 + n2 + n3) / 1.75f);
                heightMap[x, y] = h;

                // Color palette
                Color darkMoss = new Color(0.12f, 0.26f, 0.08f);
                Color lushGreen = new Color(0.28f, 0.54f, 0.16f);
                Color oliveDecay = new Color(0.42f, 0.44f, 0.18f);
                Color soil = new Color(0.18f, 0.15f, 0.10f);

                Color c = Color.Lerp(soil, darkMoss, Mathf.SmoothStep(0.2f, 0.45f, h));
                c = Color.Lerp(c, lushGreen, Mathf.SmoothStep(0.45f, 0.75f, h));
                c = Color.Lerp(c, oliveDecay, Mathf.SmoothStep(0.75f, 0.95f, h));

                albedo.SetPixel(x, y, c);
            }
        }

        // 2. Generate normal map from heightmap
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int xL = (x - 1 + size) % size;
                int xR = (x + 1) % size;
                int yD = (y - 1 + size) % size;
                int yU = (y + 1) % size;

                float dX = (heightMap[xR, y] - heightMap[xL, y]) * 4f;
                float dY = (heightMap[x, yU] - heightMap[x, yD]) * 4f;

                Vector3 n = new Vector3(-dX, -dY, 1f).normalized;
                Color nCol = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                normal.SetPixel(x, y, nCol);
            }
        }

        albedo.Apply();
        normal.Apply();

        File.WriteAllBytes($"{FOLDER_PATH}/T_Moss_Albedo.png", albedo.EncodeToPNG());
        File.WriteAllBytes($"{FOLDER_PATH}/T_Moss_Normal.png", normal.EncodeToPNG());

        Object.DestroyImmediate(albedo);
        Object.DestroyImmediate(normal);
    }

    private static void GenerateVineTextures()
    {
        int size = 512;
        Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, true);

        float[,] vineAlpha = new float[size, size];

        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                albedo.SetPixel(x, y, clear);

        Color stemBrown = new Color(0.24f, 0.17f, 0.11f, 1f);
        Color darkLeaf = new Color(0.14f, 0.35f, 0.10f, 1f);
        Color brightLeaf = new Color(0.26f, 0.58f, 0.18f, 1f);
        Color edgeLeaf = new Color(0.38f, 0.65f, 0.22f, 1f);

        // 1. Draw 3 procedural vertical twisting tendril stems
        float[] stemXOffsets = { size * 0.25f, size * 0.55f, size * 0.85f };
        for (int s = 0; s < stemXOffsets.Length; s++)
        {
            float baseX = stemXOffsets[s];
            for (int y = 0; y < size; y++)
            {
                float t = (float)y / size;
                float sinOffset = Mathf.Sin(t * Mathf.PI * 4f + s * 2f) * 28f + Mathf.Cos(t * Mathf.PI * 8f) * 12f;
                int centerX = (int)((baseX + sinOffset + size) % size);

                // Thicker stem width
                for (int dx = -3; dx <= 3; dx++)
                {
                    int px = (centerX + dx + size) % size;
                    albedo.SetPixel(px, y, stemBrown);
                    vineAlpha[px, y] = 1f;
                }
            }
        }

        // 2. Scatter leafy clumps along the stems
        Random.InitState(42);
        int leafCount = 140;
        for (int i = 0; i < leafCount; i++)
        {
            int s = i % stemXOffsets.Length;
            float t = Random.value;
            int y = (int)(t * size);
            float sinOffset = Mathf.Sin(t * Mathf.PI * 4f + s * 2f) * 28f + Mathf.Cos(t * Mathf.PI * 8f) * 12f;
            int cx = (int)((stemXOffsets[s] + sinOffset + Random.Range(-25f, 25f) + size) % size);

            int radius = Random.Range(10, 22);

            for (int ly = -radius; ly <= radius; ly++)
            {
                for (int lx = -radius; lx <= radius; lx++)
                {
                    float dist = Mathf.Sqrt(lx * lx + ly * ly);
                    if (dist <= radius)
                    {
                        int px = (cx + lx + size) % size;
                        int py = (y + ly + size) % size;

                        float factor = dist / radius;
                        Color leafColor = Color.Lerp(darkLeaf, brightLeaf, 1f - factor);
                        if (dist > radius - 2) leafColor = edgeLeaf;

                        albedo.SetPixel(px, py, leafColor);
                        vineAlpha[px, py] = Mathf.Max(vineAlpha[px, py], Mathf.SmoothStep(1f, 0.7f, factor));
                    }
                }
            }
        }

        // 3. Normal map for vines
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int xL = (x - 1 + size) % size;
                int xR = (x + 1) % size;
                int yD = (y - 1 + size) % size;
                int yU = (y + 1) % size;

                float dX = (vineAlpha[xR, y] - vineAlpha[xL, y]) * 3f;
                float dY = (vineAlpha[x, yU] - vineAlpha[x, yD]) * 3f;

                Vector3 n = new Vector3(-dX, -dY, 1f).normalized;
                Color nCol = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, vineAlpha[x, y]);
                normal.SetPixel(x, y, nCol);
            }
        }

        albedo.Apply();
        normal.Apply();

        File.WriteAllBytes($"{FOLDER_PATH}/T_Vine_Albedo.png", albedo.EncodeToPNG());
        File.WriteAllBytes($"{FOLDER_PATH}/T_Vine_Normal.png", normal.EncodeToPNG());

        Object.DestroyImmediate(albedo);
        Object.DestroyImmediate(normal);
    }

    private static void ConfigureTextureSettings()
    {
        string[] normals = { $"{FOLDER_PATH}/T_Moss_Normal.png", $"{FOLDER_PATH}/T_Vine_Normal.png" };
        foreach (var path in normals)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        string[] albedos = { $"{FOLDER_PATH}/T_Moss_Albedo.png", $"{FOLDER_PATH}/T_Vine_Albedo.png" };
        foreach (var path in albedos)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                if (path.Contains("Vine")) importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}

