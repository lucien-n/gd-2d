using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Chunk : Node
{
    public Vector2I position = Vector2I.Zero;
    public int[] tiles;

    public Chunk(Vector2I position, FastNoiseLite moisture_noise, FastNoiseLite altitude_noise, FastNoiseLite temperature_noise)
    {
        this.position = position;

        Generate(moisture_noise, altitude_noise, temperature_noise);
    }

    private void Generate(FastNoiseLite moisture_noise, FastNoiseLite altitude_noise, FastNoiseLite temperature_noise)
    {
        Vector2I offset = position * Global.CHUNK_SIZE;
        Array<int> generated_tiles = new();
        for (int x = 0; x < Global.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Global.CHUNK_SIZE; y++)
            {
                Vector2I tile_pos = new(x, y);
                float moisture = 2 * Math.Abs(moisture_noise.GetNoise2Dv(tile_pos + offset));
                float altitude = 2 * Math.Abs(altitude_noise.GetNoise2Dv(tile_pos + offset));
                float temperature = 2 * Math.Abs(temperature_noise.GetNoise2Dv(tile_pos + offset));
                generated_tiles.Add(WorldGenerator.GetTileType(moisture, altitude, temperature));
            }
        }
        tiles = generated_tiles.ToArray();
    }
}