using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerlinTest : MonoBehaviour {

	// worldWidth —    chunk x count * chunk size (8)
	// worldHeight —   chunk y count * chunk size (8)

	[SerializeField]
	int worldsWidth = 400; // must be divivable by 8
	[SerializeField]
	int worldHeight = 400; // must be divivable by 8
	[SerializeField]
	float amplitude = 50;
	[SerializeField]
	int seedsCount = 30;
	[SerializeField]
	float scale = 20;
	[SerializeField]
	int chunkHeightLimit = 4;
	[SerializeField]
	List<Renderer> textureViewers = new List<Renderer>();

	List<int> Seeds = new List<int>();

	void Start () {

		GenerateSeeds();

		DoTest("Unity Perlin Noise", Mathf.PerlinNoise, textureViewers[0]);

		DoTest("Madweedfall Perlin Noise",
			(x, y) =>
			{
				return 2f * (float)Math.Sin(Vector2.Dot(new Vector2(x, y), new Vector2(12.9898f, 78.233f)) * 43758.5453f) - 1.0f;
			},
			textureViewers[1]);

		DoTest("keijiro Perlin", (x, y) => keijiro.Perlin.Noise(x, y), textureViewers[2]);

        // https://github.com/WardBenjamin/SimplexNoise
        DoTest("Simplex Noise",
			(x, y, seed) =>
			{
                // It's a bit shitty because Simplex Noise returns float[,], not just a float. 
                // I may write better code for such situations in the future
                // Same for generators which receives seed - duplicated DoTest
                Simplex.Noise.Seed = seed;
				return Simplex.Noise.Calc2D(worldsWidth, worldHeight, seed)[(int)(x/scale * worldsWidth), (int)(y/scale * worldHeight)] / 255;
			},
			textureViewers[3]);
	}

	public void GenerateSeeds()
	{
		Debug.Log("Generating seeds...");
		for(int i = 0; i < seedsCount; i++)
			Seeds.Add(Random.Range(1000,9999));

		Debug.Log("Seeds were generated!");

	}

    public void DoTest(string name, Func<float, float, int, float> generatorWithSeed, Renderer renderer = null)
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
                    world[x, y] = generatorWithSeed(((float)x) / worldsWidth * scale, ((float)y) / worldHeight * scale, seed) * amplitude;
                }
            }
            result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
            if (renderer != null && seed == Seeds[Seeds.Count - 1])
                RenderWorld(world, renderer);
        }
        Debug.Log(result);
    }
	
	public void DoTest(string name, Func<float, float, float> generator, Renderer renderer = null)
	{
		Debug.LogFormat("Starting tests for <b>{0}</b>...", name);
		var result = string.Format("Result for <b>{0}</b>", name);
		foreach (var seed in Seeds)
		{
			var world = new float[worldsWidth, worldHeight];
			for(int y = 0; y < worldHeight; y++)
			{
				for(int x = 0; x < worldsWidth; x++)
				{
                    world[x, y] = generator(((float)x + seed) / worldsWidth * scale, ((float)y + seed) / worldHeight * scale) * amplitude;
                }
			}
			result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
			if (renderer != null && seed == Seeds[Seeds.Count - 1])
				RenderWorld(world, renderer); 
		}
		Debug.Log(result);
	}

	void RenderWorld(float[,] world, Renderer renderer)
	{
		var texture = new Texture2D(worldsWidth, worldHeight);
		for (int y = 0; y < worldHeight; y++)
		{
			for (int x = 0; x < worldsWidth; x++)
			{
				var clr = new Color(world[x, y] / amplitude, world[x, y] /amplitude , world[x, y] / amplitude);
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
		for(int y = 0; y < worldHeight-8; y+=8)
		{
			for(int x = 0; x < worldsWidth-8; x+=8)
			{
				for(int i = 0; i < 8; i++)
				{
					if ((world[x, y + i] - world[x + 8, y]) > chunkHeightLimit)
						return false;
					if ((world[x + i, y] - world[x, y + 8]) > chunkHeightLimit)
						return false;
				}
			}
		}

		return true;
	}
}
