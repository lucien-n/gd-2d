using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class WorldGenerator : Node
{
    [Export]
    int tile_size = Global.TILE_SIZE;
    [Export]
    int chunk_size = Global.CHUNK_SIZE;
    [Export]
    int render_distance = Global.RENDER_DISTANCE;
    [Export]
    private CharacterBody2D player;

    private Array<Vector2I> render_distance_offsets = new();
    private Vector2I player_current_chunk = Vector2I.Zero;

    private TileMap tiles_tm;

    Dictionary<string, int> tiles = new() {
        { "deep_water", 0 },
        { "shallow_water", 1 },
        { "sand", 2 },
        { "lush_grass", 3 },
        { "grass", 4 },
        { "cold_grass", 5 },
        { "stone", 6 },
        { "snow", 7 },
    };


    private FastNoiseLite noise = new();
    private FastNoiseLite moisture_noise = new();
    private FastNoiseLite altitude_noise = new();
    private FastNoiseLite temperature_noise = new();

    private Dictionary<Vector2I, Dictionary<Vector2I, string>> generated_chunks = new();

    private Array<Vector2I> unready_chunks = new();
    private Array<Vector2I> rerender_chunks = new();

    public override void _Ready()
    {
        tiles_tm = GetNode<TileMap>("tiles");

        RandomNumberGenerator rand = new();
        noise.Seed = (int)rand.Randi();
        moisture_noise.Seed = (int)rand.Randi();
        altitude_noise.Seed = (int)rand.Randi();
        temperature_noise.Seed = (int)rand.Randi();

        render_distance_offsets = GenerateRenderDistanceOffsets(render_distance);
    }

    public override void _Process(double _delta)
    {
        player_current_chunk = new Vector2I(
            (int)Math.Floor(player.GlobalPosition.X / tile_size / chunk_size),
            (int)Math.Floor(player.GlobalPosition.Y / tile_size / chunk_size)
        );

        GenerateChunks();
        RenderChunks();
    }

    private async void GenerateChunks()
    {
        Dictionary<Vector2I, Dictionary<Vector2I, string>> new_chunks = new();
        Task chunk_generation = Task.Run(() =>
        {
            foreach (Vector2I offset in render_distance_offsets)
            {
                Vector2I chunk_pos = player_current_chunk + offset;
                if (generated_chunks.ContainsKey(chunk_pos) || unready_chunks.Contains(chunk_pos)) continue;
                new_chunks[chunk_pos] = GenerateChunk(chunk_pos);
            }
        });

        await chunk_generation;
        foreach (var new_chunk in new_chunks)
        {
            generated_chunks[new_chunk.Key] = new_chunk.Value;
            RerenderChunk(new_chunk.Key);
        }
    }

    private void RenderChunks()
    {
        foreach (Vector2I offset in render_distance_offsets)
        {
            Vector2I chunk_pos = player_current_chunk + offset;
            if (!rerender_chunks.Contains(chunk_pos)) continue;
            RenderChunk(chunk_pos);
        }
    }

    private Dictionary<Vector2I, string> GenerateChunk(Vector2I chunk_pos)
    {
        Dictionary<Vector2I, string> chunk = new();
        Vector2I offset = chunk_pos * chunk_size;

        for (int x = 0; x < chunk_size; x++)
        {
            for (int y = 0; y < chunk_size; y++)
            {
                Vector2I tile_pos = new(x, y);
                float moisture = 2 * Math.Abs(moisture_noise.GetNoise2Dv(tile_pos + offset));
                float altitude = 2 * Math.Abs(altitude_noise.GetNoise2Dv(tile_pos + offset));
                float temperature = 2 * Math.Abs(temperature_noise.GetNoise2Dv(tile_pos + offset));

                string t = GetTileType(moisture, altitude, temperature);
                chunk[tile_pos] = t;
            }
        }

        return chunk;
    }

    private void RenderChunk(Vector2I chunk_pos)
    {
        for (int x = 0; x < chunk_size; x++)
        {
            for (int y = 0; y < chunk_size; y++)
            {
                SetTile(new Vector2I(
                    chunk_pos.X * chunk_size + x,
                    chunk_pos.Y * chunk_size + y),
                    generated_chunks[chunk_pos][new Vector2I(x, y)]);
                rerender_chunks.Remove(chunk_pos);
            }
        }
        rerender_chunks.Remove(player_current_chunk);
    }

    private string GetTileType(float moisture, float altitude, float temperature)
    {
        string type = "grass";

        if (altitude < .2) // ocean
        {
            type = "shallow_water";
        }
        else if (Global.IsBetween(altitude, .2, .3))
        {
            if (temperature < .3)
            {
                type = "stone"; // stone beach
            }
            else
            {
                type = "sand"; // sand beach
            }
        }
        else if (Global.IsBetween(altitude, .3, .8))
        {
            if (Global.IsBetween(moisture, .4, .9) && temperature > .6) // jungle
            {
                type = "lush_grass";
            }
            else if (temperature > .7 && moisture < .4) // desert
            {
                type = "sand";
            }
            else if (temperature < .1 && moisture < .9) // snow
            {
                type = "snow";
            }
        }
        else
        {
            if (altitude < 1) // taiga
            {
                type = "cold_grass";
            }
            else if (altitude > 1.2) // mountain
            {
                if (temperature < .1) type = "snow";
                else type = "stone";
            }

        }

        return type;
    }

    private Array<Vector2I> GenerateRenderDistanceOffsets(int render_distance)
    {
        Array<Vector2I> offsets = new();

        int grid_size = render_distance * 2 + 1;

        for (int x = 0; x < grid_size; x++)
        {
            for (int y = 0; y < grid_size; y++)
            {
                offsets.Add(new Vector2I(x - render_distance, y - render_distance));
            }
        }

        return offsets;
    }

    public void RerenderChunk(Vector2I chunk_pos)
    {
        if (rerender_chunks.Contains(chunk_pos) || !generated_chunks.ContainsKey(chunk_pos)) return;
        rerender_chunks.Add(chunk_pos);
    }

    private void SetTile(Vector2I pos, string type)
    {
        tiles_tm.SetCell(0, pos, tiles[type], Vector2I.Zero);
    }
}