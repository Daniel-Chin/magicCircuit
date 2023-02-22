using Godot;
using System;

public class SidePanel : PanelContainer
{
    CircuitUI MyCircuitUI;
    VBoxContainer VBox;
    public override void _Ready()
    {
        VBox = GetNode<VBoxContainer>("VBox");
        VBox.Visible = false;
    }

    public void Hold(Wand wand)
    {
        MyCircuitUI = new CircuitUI(wand, 0);
        VBox.AddChild(MyCircuitUI);
        VBox.Visible = true;
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}