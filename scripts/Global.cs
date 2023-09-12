using Godot;

public partial class Global : Node
{
    [Export]
    public static bool DEV = false;

    [Export]
    public static int TILE_SIZE = 16;
    [Export]
    public static int CHUNK_SIZE = 16;
    [Export]
    public static int RENDER_DISTANCE = 3;


    public static bool IsBetween(float value, float min_value, float max_value)
    {
        return value >= min_value && value <= max_value;
    }

    public static bool IsBetween(float value, double min_value, double max_value)
    {
        return value > min_value && value < max_value;
    }
}