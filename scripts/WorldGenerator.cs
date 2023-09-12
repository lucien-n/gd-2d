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

    private FastNoiseLite noise = new();
    private FastNoiseLite moisture_noise = new();
    private FastNoiseLite altitude_noise = new();
    private FastNoiseLite temperature_noise = new();

    private Dictionary<Vector2I, Chunk> generated_chunks = new();

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

    private async void GenerateChunks()
    {
        Dictionary<Vector2I, Chunk> new_chunks = new();
        Task chunk_generation = Task.Run(() =>
        {
            foreach (Vector2I offset in render_distance_offsets)
            {
                Vector2I chunk_pos = player_current_chunk + offset;
                if (generated_chunks.ContainsKey(chunk_pos) || unready_chunks.Contains(chunk_pos)) continue;
                new_chunks[chunk_pos] = new Chunk(chunk_pos, moisture_noise, altitude_noise, temperature_noise);
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


    private void RenderChunk(Vector2I chunk_pos)
    {
        for (int x = 0; x < chunk_size; x++)
        {
            for (int y = 0; y < chunk_size; y++)
            {
                SetTile(new Vector2I(
                    chunk_pos.X * chunk_size + x,
                    chunk_pos.Y * chunk_size + y),
                    generated_chunks[chunk_pos].tiles[x * Global.CHUNK_SIZE + y]);
                rerender_chunks.Remove(chunk_pos);
            }
        }
        rerender_chunks.Remove(player_current_chunk);
    }

    public void RerenderChunk(Vector2I chunk_pos)
    {
        if (rerender_chunks.Contains(chunk_pos) || !generated_chunks.ContainsKey(chunk_pos)) return;
        rerender_chunks.Add(chunk_pos);
    }


    public static int GetTileType(float moisture, float altitude, float temperature)
    {
        if (altitude < .2) // ocean
        {
            return TileMaterial.SHALLOW_WATER;
        }
        else if (Global.IsBetween(altitude, .2, .3))
        {
            if (temperature < .3)
                return TileMaterial.STONE; // stone beach
            else
                return TileMaterial.SAND; // sand beach
        }
        else if (Global.IsBetween(altitude, .3, .8))
        {
            if (Global.IsBetween(moisture, .4, .9) && temperature > .6) // jungle
                return TileMaterial.LUSH_GRASS;
            else if (temperature > .7 && moisture < .4) // desert
                return TileMaterial.SAND;
            else if (temperature < .1 && moisture < .9) // snow
                return TileMaterial.SNOW;
        }
        else
        {
            if (altitude < 1) // taiga
                return TileMaterial.COLD_GRASS;
            else if (altitude > 1.2) // mountain
            {
                if (temperature < .1) return TileMaterial.SNOW;
                else return TileMaterial.STONE;
            }

        }

        return TileMaterial.GRASS;
    }

    private void SetTile(Vector2I pos, int type)
    {
        tiles_tm.SetCell(0, pos, type, Vector2I.Zero);
    }
}