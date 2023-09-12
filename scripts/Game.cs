using System;
using Godot;

public partial class Game : Node
{

    private Player player;
    private Camera2D camera;
    public override void _Ready()
    {
        if (!Global.DEV) return;

        player = GetNode<Player>("player");
        camera = player.GetNode<Camera2D>("camera");

        camera.Zoom = new Vector2(.5F, .5F);
    }
}