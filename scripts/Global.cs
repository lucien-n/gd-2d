using Godot;

public partial class Global : Node
{
    public const int TILE_SIZE = 16;
    public const int CHUNK_SIZE = 16;
    public const int RENDER_DISTANCE = 3;


    public static bool IsBetween(float value, float min_value, float max_value)
    {
        return value >= min_value && value <= max_value;
    }

    public static bool IsBetween(float value, double min_value, double max_value)
    {
        return value > min_value && value < max_value;
    }
}