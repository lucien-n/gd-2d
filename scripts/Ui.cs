using Godot;

public partial class Ui : CanvasLayer
{
    private Label fps_label;

    public override void _Ready()
    {
        fps_label = GetNode<Label>("debug/fps");
    }

    public override void _Process(double _delta)
    {
        fps_label.Text = "FPS: " + Engine.GetFramesPerSecond().ToString();
    }
}