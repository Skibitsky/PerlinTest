using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerlinTest : MonoBehaviour
{
    [SerializeField]
    int worldsWidth = 400;
    [SerializeField]
    int worldHeight = 400;
    [SerializeField]
    float amplitude = 50;
    [SerializeField]
    int seedsCount = 30;
    [SerializeField]
    float scale = 20;
    [SerializeField]
    float chunkHeightLimit = 4;
    [SerializeField]
    int octavesCount = 5;
    [SerializeField]
    List<Renderer> textureViewers = new List<Renderer>();
    [SerializeField]
    GameObject cubePrefab;

    List<int> Seeds = new List<int>();
    List<GameObject> cubeWorlds = new List<GameObject>();
    int cubeWorldsCount = 0;

    [ContextMenu("Refersh")]
    void Start()
    {
        GenerateSeeds();

        foreach (var t in cubeWorlds)
            Destroy(t);
        cubeWorlds.Clear();
        cubeWorldsCount = 0;

        DoTest("Unity Perlin Noise", Mathf.PerlinNoise, textureViewers[0]);

        DoTest("Madweedfall Perlin Noise",
            (x, y) =>
            {
                return 2f * (float)Math.Sin(Vector2.Dot(new Vector2(x, y), new Vector2(12.9898f, 78.233f)) * 43758.5453f) - 1.0f;
            },
            textureViewers[1]);

        DoTest("keijiro Perlin", (x, y) => keijiro.Perlin.Noise(x, y), textureViewers[2], true);

        // https://github.com/WardBenjamin/SimplexNoise
        DoTest("Simplex Noise",
            (x, y, seed) =>
            {
                Simplex.Noise.Seed = seed;
                return Simplex.Noise.CalcPixel2D((int)x, (int)y, scale) / 255;
            },
            textureViewers[3]);
    }

    public void GenerateSeeds()
    {
        Debug.Log("Generating seeds...");
        for (int i = 0; i < seedsCount; i++)
            Seeds.Add(Random.Range(1000, 9999));

        Debug.Log("Seeds were generated!");

    }

    public void DoTest(string name, Func<float, float, int, float> generatorWithSeed, Renderer renderer = null, bool generateCubeWorld = false)
    {
        Debug.LogFormat("Starting tests for <b>{0}</b>...", name);
        var result = string.Format("Result for <b>{0}</b>", name);
        foreach (var seed in Seeds)
        {
            var world = new float[worldsWidth, worldHeight];

            for (int y = 0; y < worldHeight; y++)
            {
                for (int x = 0; x < worldsWidth; x++)
                {
                    for (int i = 1; i < octavesCount + 1; i++)
                        world[x, y] += generatorWithSeed(((float)x) / worldsWidth * scale/2*i, ((float)y) / worldHeight * scale/2*i, seed) * amplitude;

                    world[x, y] /= octavesCount;
                }
            }
            result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
            if (seed == Seeds[Seeds.Count - 1])
            {
                if (renderer != null)
                    RenderWorld(world, renderer);
                if (generateCubeWorld)
                    GenerateCubeWorld(name, world);
            }
        }
        Debug.Log(result);
    }

    public void DoTest(string name, Func<float, float, float> generator, Renderer renderer = null, bool generateCubeWorld = false)
    {
        Debug.LogFormat("Starting tests for <b>{0}</b>...", name);
        var result = string.Format("<b>Result</b> for <b>{0}</b>", name);
        foreach (var seed in Seeds)
        {
            var world = new float[worldsWidth, worldHeight];

            for (int y = 0; y < worldHeight; y++)
            {
                for (int x = 0; x < worldsWidth; x++)
                {
                    for(int i = 1; i < octavesCount+1; i++)
                        world[x, y] += generator(((float)x + seed) / worldsWidth * scale/2*i, ((float)y + seed) / worldHeight * scale/2*i) * amplitude;

                    world[x, y] /= octavesCount;
                }
            }
            result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
            if (seed == Seeds[Seeds.Count - 1])
            {
                if (renderer != null)
                    RenderWorld(world, renderer);
                if (generateCubeWorld)
                    GenerateCubeWorld(name, world);
            }
        }
        Debug.Log(result);
    }

    void GenerateCubeWorld(string name, float[,] world)
    {
        Debug.LogFormat("Started generating cube world for <b>{0}</b>....", name);
        var parent = new GameObject(name).transform;
        parent.position = new Vector3(worldsWidth * cubePrefab.transform.localScale.x * cubeWorldsCount + 10 * cubePrefab.transform.localScale.x, 0, worldHeight * cubePrefab.transform.localScale.y *cubeWorldsCount + 10 * cubePrefab.transform.localScale.y);
        cubeWorlds.Add(parent.gameObject);
        for (int x = 0; x < worldsWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                var height = Round(world[x, y]);
                var go = Instantiate(cubePrefab, 
                    new Vector3(x * cubePrefab.transform.localScale.x + worldsWidth * cubeWorldsCount + 10 * cubePrefab.transform.localScale.x, height * cubePrefab.transform.localScale.y, y * cubePrefab.transform.localScale.z + worldHeight * cubeWorldsCount + 10 * cubePrefab.transform.localScale.z),
                    Quaternion.identity);
                go.transform.SetParent(parent);                
            }
        }
        cubeWorldsCount++;
    }

    void RenderWorld(float[,] world, Renderer renderer)
    {
        var texture = new Texture2D(worldsWidth, worldHeight);
        for (int y = 0; y < worldHeight; y++)
        {
            for (int x = 0; x < worldsWidth; x++)
            {
                var clr = new Color(world[x, y] / amplitude, world[x, y] / amplitude, world[x, y] / amplitude);
                texture.SetPixel(x, y, clr);
            }
        }
        texture.Apply();
        renderer.material.mainTexture = texture;
    }

    // In our project we spawn premade chunks of 8x8 hight points. The differece betwen two chanks 
    // cannot be more than chunkHeightLimit.
    // false is failed
    bool CheckForLimit(float[,] world)
    {
        for (int y = 0; y < worldHeight - 1; y++)
        {
            for (int x = 0; x < worldsWidth - 1; x++)
            {

                if (Round(world[x, y]) - Round(world[x, y + 1]) > chunkHeightLimit)
                    return false;
                if (Round(world[x, y]) - Round(world[x + 1, y]) > chunkHeightLimit)
                    return false;
                if (Round(world[x, y]) - Round(world[x + 1, y + 1]) > chunkHeightLimit)
                    return false;
            }
        }

        return true;
    }

    float Round(float value)
    {
        return Mathf.Round(value * 2) / 2; // I need 0.5 height step for my chunk prefabs. You can round it as you like
    }
}
