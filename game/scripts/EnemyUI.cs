using Godot;
using System;

public class EnemyUI : SpawnableSpecialUI
{
    public RichTextLabel Label;
    private static readonly Texture TEXTURE = GD.Load<Texture>("res://texture/npc/enemy.png");
    public EnemyUI() : base(null) { }
    public EnemyUI(Enemy e) : base(e)
    {
        base._Ready();
        MySprite.Texture = TEXTURE;
        Label = new RichTextLabel();
        AddChild(Label);
        Label.BbcodeEnabled = true;
        Label.ScrollActive = false;
        Label.GrowHorizontal = Control.GrowDirection.Both;
        Label.GrowVertical = Control.GrowDirection.Both;
        Label.RectMinSize = new Vector2(300, 34);
        Label.RectSize = Label.RectMinSize;
        Label.RectPivotOffset = Label.RectMinSize / 2;
        Label.RectPosition = -Label.RectPivotOffset;
        Label.Theme = Shared.THEME;
        UpdateHP();
    }

    public void UpdateHP()
    {
        Label.BbcodeText = $"[center]{MathBB.Build(((Enemy)MySpawnable).HP)}[/center]";
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
