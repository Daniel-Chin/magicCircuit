using Godot;
using System;

public class LowPanel : PanelContainer
{
    public TextureRect Face;
    public RichTextLabel Label;
    public HBoxContainer ButtonsHBox;
    public Button Button0;
    public Button Button1;
    public TextureButton Mask;
    public float FontHeight;
    private float _timeElasped;
    private bool _hasButtons;
    public override void _Ready()
    {
        _hasButtons = false;

        Face = GetNode<TextureRect>("HBox/Face");
        Label = GetNode<RichTextLabel>("HBox/VBox/Label");
        ButtonsHBox = GetNode<HBoxContainer>("HBox/VBox/Buttons");
        Button0 = GetNode<Button>("HBox/VBox/Buttons/Centerer0/Button0");
        Button1 = GetNode<Button>("HBox/VBox/Buttons/Centerer1/Button1");
        Mask = GetNode<TextureButton>("Mask");
        ButtonsHBox.Visible = false;

        Button0.Connect(
            "pressed", this, "Button0Clicked"
        );
        Button1.Connect(
            "pressed", this, "Button1Clicked"
        );

        DynamicFont font = Shared.NewFont(60);
        Label.AddFontOverride("normal_font", font);
        FontHeight = font.GetHeight();
    }

    public void SetFace(NPC npc)
    {
        if (npc == null)
        {
            Face.Visible = false;
            return;
        }
        Face.Texture = npc.Texture();
        Face.Visible = true;
    }

    public void Display(string message) {
        Display(message, false);
    }
    public void Display(string message, bool center)
    {
        bool inCyan = false;
        Label.Clear();
        if (center) {
            Label.PushAlign(RichTextLabel.Align.Center);
            Label.AppendBbcode("\n");
        }
        foreach (var part in message.Split('*'))
        {
            Label.AppendBbcode(part);
            if (inCyan)
            {
                Label.Pop();
            }
            else
            {
                Label.PushColor(Colors.Cyan);
            }
            inCyan = !inCyan;
        }
        if (inCyan)
            Label.Pop();
        if (center) {
            Label.Pop();
        }
        Label.PercentVisible = 0;
        _timeElasped = 0;
        Main.Singleton.VBoxLowPanel.Visible = true;
    }

    public void SetButtons(string text0, string text1)
    {
        _hasButtons = true;
        Button0.Text = $" {text0} ";
        Button1.Text = $" {text1} ";
    }
    public void NoButtons()
    {
        _hasButtons = false;
        ButtonsHBox.Visible = false;
        Mask.Visible = true;
    }

    public override void _Process(float delta)
    {
        _timeElasped += delta;
        float rollProgress = Params.TEXT_ROLL_SPEED * _timeElasped / Label.Text.Length;
        if (rollProgress >= 1) {
            Label.PercentVisible = 1;
            if (_hasButtons) {
                ButtonsHBox.Visible = true;
                Mask.Visible = false;
            }
        } else {
            Label.PercentVisible = rollProgress;
        }
    }

    public void SkipRoll()
    {
        _timeElasped = Label.Text.Length / Params.TEXT_ROLL_SPEED;
    }

    public void _on_Mask_pressed() { }
    public void Clicked()
    {
        if (!Main.Singleton.VBoxLowPanel.Visible) return;
        if (!Mask.Visible) return;
        if (_timeElasped < Label.Text.Length / Params.TEXT_ROLL_SPEED)
        {
            SkipRoll();
            return;
        }
        if (_hasButtons)
            return;
        Main.Singleton.VBoxLowPanel.Visible = false;
        Director.OnEventStepComplete();
    }

    public void Button0Clicked() {
        Main.Singleton.VBoxLowPanel.Visible = false;
        Director.NowEvent.ButtonClicked(0);
    }
    public void Button1Clicked() {
        Main.Singleton.VBoxLowPanel.Visible = false;
        Director.NowEvent.ButtonClicked(1);
    }
}
