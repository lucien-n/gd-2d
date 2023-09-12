using System;
using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    private int max_speed = 200;
    [Export]
    private int acceleration = 800;
    [Export]
    private int friction = 800;

    private Vector2 direction = Vector2.Zero;


    public override void _Process(double delta)
    {
        direction.X = (Input.IsActionPressed("right") ? 1 : 0) - (Input.IsActionPressed("left") ? 1 : 0);
        direction.Y = (Input.IsActionPressed("down") ? 1 : 0) - (Input.IsActionPressed("up") ? 1 : 0);
        direction = direction.Normalized();

        float dt_friction = (float)(friction * delta);

        if (direction == Vector2.Zero)
        {
            if (Velocity.Length() > dt_friction)
                Velocity -= Velocity.Normalized() * dt_friction;
            else
                Velocity = Vector2.Zero;
        }
        else
        {
            Velocity += direction * acceleration * (float)delta;
            Velocity = Velocity.LimitLength(max_speed);
        }

        MoveAndSlide();
    }
}