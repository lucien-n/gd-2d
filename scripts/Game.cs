using System;
using Godot;

public partial class Game : Node
{
    public override void _Ready()
    {
        if (!Global.DEV) return;

        GetWindow().ContentScaleFactor = .5F;
    }
}