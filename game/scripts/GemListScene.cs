using Godot;
using System;
using System.Collections.Generic;

public class GemListScene : WindowDialog
{
    // code-defined
    [Signal] public delegate void finished();
    private static readonly Vector2 SIZE = new Vector2(800, 500);
    public VBoxContainer OuterVBox;
    public Label Title;
    public ScrollContainer MyScroll;
    public VBoxContainer VBox;
    private List<Gem> _gems;
    public Simplest MetaLevel;
    public MagicItem Selected;
    private bool _rotated;
    private GemEntry _header;
    private GemEntry _lastEntry;
    public enum Mode
    {
        PLACE, EDIT, BUY,
    }
    private Mode _mode;
    private MagicItem _allowUninstallFrom;

    public GemListScene() : base()
    {
        _gems = new List<Gem>();
        _header = null;
        _allowUninstallFrom = null;
        Connect("popup_hide", this, "OnPopupHide");

        Theme = Shared.THEME;
        RectMinSize = SIZE;
        OuterVBox = new VBoxContainer();
        AddChild(OuterVBox);
        OuterVBox.RectMinSize = SIZE;
        Title = new Label();
        OuterVBox.AddChild(Title);
        Title.Align = Label.AlignEnum.Center;
        Title.Valign = Label.VAlign.Center;
        Title.RectMinSize = new Vector2(0, 50);
        MyScroll = new ScrollContainer();
        OuterVBox.AddChild(MyScroll);
        MyScroll.SizeFlagsVertical = (int)Container.SizeFlags.ExpandFill;
        VBox = new VBoxContainer();
        MyScroll.AddChild(VBox);
        VBox.SizeFlagsHorizontal = (int)Container.SizeFlags.ExpandFill;
    }

    private GemEntry Add(Gem gem)
    {
        int gemI = _gems.Count;
        _gems.Add(gem);
        GemEntry gemEntry = new GemEntry(gem);
        ContainerButton cButton = new ContainerButton(gemEntry);
        VBox.AddChild(cButton);
        cButton.MyButton.Connect(
            "pressed", this, "GemClicked",
            new Godot.Collections.Array() { gemI, cButton }
        );
        _lastEntry = gemEntry;
        return gemEntry;
    }

    private void FillEntryRemove(GemEntry gemEntry, Gem gem)
    {
        gemEntry.PresetFiveSmallOneBig();
        gemEntry.Labels[5].BbcodeText = gem.Explain(false);
    }
    private void FillEntryThisWay(GemEntry gemEntry, Gem gem)
    {
        gemEntry.PresetOneBig();
        gemEntry.Pad();
        gemEntry.Labels[0].BbcodeText = "[center]This way![/center]";
    }
    private void FillEntryInStock(GemEntry gemEntry, Gem gem)
    {
        gemEntry.PresetFiveSmallOneBig();
        var (nInWand, nInCGs, nAvailable) = CountGems(gem);
        gemEntry.Labels[0].BbcodeText = $"[color=lime][center]{MathBB.Build(nAvailable)}[/center][/color]";
        gemEntry.Labels[1].BbcodeText = "[center]/[/center]";
        gemEntry.Labels[2].BbcodeText = $"[color=aqua][center]{nInWand}[/center][/color]";
        gemEntry.Labels[3].BbcodeText = "[center]/[/center]";
        gemEntry.Labels[4].BbcodeText = $"[color=yellow][center]{nInCGs}[/center][/color]";
        gemEntry.Labels[5].BbcodeText = gem.Explain(false);
    }
    private void AddAndThisWay(Gem gem)
    {
        FillEntryThisWay(Add(gem), gem);
    }
    private void FillEntryShop(GemEntry gemEntry, Gem gem)
    {
        if (
            gem is Gem.Focus
            && GameState.Persistent.HasGems[gem.Name()] == 1
            && !(NPC.Shop.PriceOf(gem) <= GameState.Persistent.Money)
        ) {
            gemEntry.PresetOneBig();
            gemEntry.Pad();
            gemEntry.Labels[0].BbcodeText = "--- Out of stock ---";
            return;
        }
        
        gemEntry.PresetBigMidMoney();
        gemEntry.Pad();
        Simplest nOwned = GameState.Persistent.CountGemsOwned(gem);
        Simplest price = NPC.Shop.PriceOf(gem);
        string priceTag;
        if (price <= GameState.Persistent.Money) {
            priceTag = MathBB.Build(price);
        } else {
            priceTag = "can't\nafford";
        }
        gemEntry.Labels[0].BbcodeText = gem.Explain(false);
        gemEntry.Labels[1].BbcodeText = $"[color=#00ff00][center]{MathBB.Build(nOwned)}[/center][/color]";
        gemEntry.Labels[2].BbcodeText = $"[color=yellow][center]{priceTag}[/center][/color]";
    }
    private void FillEntryEdit(GemEntry gemEntry, Gem gem)
    {
        gemEntry.PresetOneBig();
        gemEntry.Pad();
        gemEntry.Labels[0].BbcodeText = gem.Explain(false);
    }

    public void ListPlacable(Simplest metaLevel)
    {
        _mode = Mode.PLACE;
        MetaLevel = metaLevel;
        _rotated = false;
        Title.Text = "Which gem?";
        _lastEntry = null;

        // header
        _header = new GemEntry(null);
        OuterVBox.AddChild(_header);
        OuterVBox.MoveChild(_header, 1);
        _header.PresetFiveSmallMerged();
        _header.MyGemUI.Empty();
        string wandName = GameState.Persistent.MyWand.DisplayName();
        _header.Labels[0].BbcodeText = (
            "[center][color=lime]Available[/color] /\n"
            + $"[color=aqua]In {wandName}[/color] /\n"
            + "[color=yellow]In Custom Gems[/color][/center]"
        );

        Gem gem = new Gem.RemoveGem();
        FillEntryRemove(Add(gem), gem);

        ListGems(AllPlacables());
    }
    public static List<Gem> AllBuyableGems()
    {
        List<Gem> gems = new List<Gem> {
            new Gem.AddOne(),
            new Gem.WeakMult(),
            new Gem.Mirror(true),
            new Gem.Focus(new PointInt(0, 1)),
            new Gem.Stochastic(true),
        };
        return gems;
    }
    public static (List<Gem>, List<Gem>) AllBuyableCGs()
    {
        List<Gem> gems = new List<Gem>();
        List<Gem> gemsMore = new List<Gem>();
        if (GameState.Persistent.MyTypelessGem is CustomGem typeless)
        {
            gems.Add(typeless);
        }
        foreach (var entry in GameState.Persistent.HasCustomGems)
        {
            if (entry.Key < 5) {
                gems.Add(entry.Value.Item2);
            } else {
                gemsMore.Add(entry.Value.Item2);
            }
        }
        return (gems, gemsMore);
    }
    public static List<Gem> AllPlacables() {
        List<Gem> gems = new List<Gem>();
        var (cgLow, cgHigh) = AllBuyableCGs();
        gems.AddRange(cgLow);
        gems.AddRange(AllBuyableGems());
        gems.Insert(gems.Count - 4, new Gem.StrongMult());
        gems.AddRange(cgHigh);
        return gems;
    }

    public void ListGems(List<Gem> gems)
    {
        foreach (Gem gem in gems)
        {
            if (
                _mode == Mode.PLACE
                && !(gem is CustomGem)
                && GameState.Persistent.CountGemsOwned(gem).Equals(Simplest.Zero())
            )
                continue;
            {
                GemEntry gemEntry = Add(gem);
                switch (_mode)
                {
                    case Mode.BUY:
                        FillEntryShop(gemEntry, gem);
                        break;
                    case Mode.PLACE:
                        FillEntryInStock(gemEntry, gem);
                        break;
                    case Mode.EDIT:
                        FillEntryEdit(gemEntry, gem);
                        break;
                    default:
                        throw new Shared.ValueError();
                }
            }
        }
    }

    private bool AskRotate()
    {
        Gem gem = (Gem)Selected;
        _gems.Clear();
        Shared.QFreeChildren(VBox);
        _lastEntry = null;
        _header = null;
        Title.Text = "Which way?";
        switch (gem)
        {
            case Gem.Focus _:
                AddAndThisWay(new Gem.Focus(new PointInt(1, 0)));
                AddAndThisWay(new Gem.Focus(new PointInt(-1, 0)));
                AddAndThisWay(new Gem.Focus(new PointInt(0, 1)));
                AddAndThisWay(new Gem.Focus(new PointInt(0, -1)));
                break;
            case Gem.Stochastic _:
                AddAndThisWay(new Gem.Stochastic(true));
                AddAndThisWay(new Gem.Stochastic(false));
                break;
            case Gem.Mirror _:
                AddAndThisWay(new Gem.Mirror(true));
                AddAndThisWay(new Gem.Mirror(false));
                break;
            default:
                return false;
        }
        return true;
    }

    public void GemClicked(int gemI, ContainerButton cButton)
    {
        Gem gem = _gems[gemI];
        CustomGem cG = gem as CustomGem;
        if (cButton.Confirming) {
            if (_allowUninstallFrom == null)
                return;
            cButton.FreeText();
            Circuit circuit;
            switch (_allowUninstallFrom) {
                case Wand w:
                    circuit = w.MyCircuit;
                    break;
                case CustomGem cGem:
                    circuit = cGem.MyCircuit;
                    break;
                default:
                    throw new Shared.TypeError();
            }
            circuit.RemoveAny(gem);
            _allowUninstallFrom = null;
            GemClicked(gemI, cButton);
            return;
        }
        if (!(gem is Gem.RemoveGem))
        {
            if (_mode == Mode.PLACE)
            {
                if (
                    cG != null
                    && cG.MetaLevel >= MetaLevel
                    && cG.MetaLevel.MyRank == Rank.FINITE
                )
                {
                    RichTextLabel label = cButton.TextInstead();
                    cButton.RectMinSize = new Vector2(0, GemEntry.SIZE);
                    label.PushColor(Colors.Red);
                    label.PushAlign(RichTextLabel.Align.Center);
                    label.AppendBbcode("Type error: ");
                    label.AppendBbcode(new CustomGem(MetaLevel).DisplayName());
                    label.AppendBbcode(" cannot embed ");
                    label.AppendBbcode(cG.DisplayName());
                    label.AppendBbcode(". ");
                    label.Pop();
                    label.Pop();
                    _allowUninstallFrom = null;
                    return;
                }
                var (nInWand, nInCGs, nAvailable) = CountGems(gem);
                if (nAvailable <= Simplest.Zero())
                {
                    RichTextLabel label = cButton.TextInstead();
                    cButton.RectMinSize = new Vector2(0, GemEntry.SIZE);
                    label.PushAlign(RichTextLabel.Align.Center);
                    if (nInWand + nInCGs == 0) {
                        label.PushColor(Colors.Red);
                        label.AppendBbcode("You own zero copies. Shop some!");
                        label.Pop();
                        _allowUninstallFrom = null;
                    } else {
                        label.AppendBbcode("Already in use. Uninstall one from ");
                        label.PushColor(Colors.Cyan);
                        _allowUninstallFrom = AnyEmbedderOf(gem);
                        label.AppendBbcode(_allowUninstallFrom.DisplayName());
                        label.Pop();
                        label.AppendBbcode("?\n");
                        label.AppendBbcode("Click again to confirm.");
                    }
                    label.Pop();
                    return;
                }
            }
        }
        Selected = (_gems[gemI]);
        if (_mode == Mode.PLACE && cG == null && !_rotated)
        {
            _rotated = true;
            if (AskRotate())
                return;
        }
        if (_mode == Mode.BUY) {
            if (BuyGem((Gem)Selected)) {
                Shared.QFreeChildren(VBox);
                _header.QueueFree();
                ListBuyable();
                Main.Singleton.MySidePanel.Update();
            } else {
                RichTextLabel label = cButton.TextInstead();
                cButton.RectMinSize = new Vector2(0, GemEntry.SIZE);
                label.PushAlign(RichTextLabel.Align.Center);
                label.PushColor(Colors.Red);
                label.AppendBbcode("Not enough ");
                label.PushColor(Colors.Yellow);
                label.AppendBbcode("$");
                label.Pop();
                label.AppendBbcode(".");
                label.Pop();
                label.Pop();
                _allowUninstallFrom = null;
            }
        } else {
            _rotated = false;
            Hide();
        }
    }

    private int CountGemsInCircuit(Gem gem, Circuit circuit)
    {
        int acc = 0;
        foreach (Gem g in circuit.Field)
        {
            if (g == null) continue;
            if (gem is CustomGem customGem)
            {
                if (g is CustomGem cG)
                {
                    if (cG.MetaLevel.Equals(customGem.MetaLevel))
                        acc++;
                }
            }
            else
            {
                if (g.GetType() == gem.GetType())
                    acc++;
            }
        }
        return acc;
    }
    private int CountGemsInWand(Gem gem)
    {
        return CountGemsInCircuit(gem, GameState.Persistent.MyWand.MyCircuit);
    }
    private int CountGemsInCGs(Gem gem)
    {
        int acc = 0;
        CustomGem cG;
        foreach (var entry in GameState.Persistent.HasCustomGems)
        {
            cG = entry.Value.Item2;
            if (cG != null)
            {
                acc += CountGemsInCircuit(gem, cG.MyCircuit);
            }
        }
        cG = GameState.Persistent.MyTypelessGem;
        if (cG != null)
        {
            acc += CountGemsInCircuit(gem, cG.MyCircuit);
        }
        return acc;
    }

    public (int, int, Simplest) CountGems(Gem gem)
    {
        int nInWand = CountGemsInWand(gem);
        int nInCGs = CountGemsInCGs(gem);
        Simplest nOwned = GameState.Persistent.CountGemsOwned(gem);
        Simplest nAvailable;
        if (nOwned.MyRank == Rank.FINITE)
        {
            nAvailable = new Simplest(
                Rank.FINITE, nOwned.K - nInWand - nInCGs
            );
        }
        else
        {
            nAvailable = nOwned;
        }
        return (nInWand, nInCGs, nAvailable);
    }

    public void ListEditable()
    {
        _mode = Mode.EDIT;
        Title.Text = "Whose circuit to alter?";
        _lastEntry = null;

        Wand wand = GameState.Persistent.MyWand;
        GemEntry wandEntry = new GemEntry(null);
        wandEntry.PresetOneBig();
        wandEntry.Pad();
        ContainerButton cButton = new ContainerButton(wandEntry);
        VBox.AddChild(cButton);
        cButton.MyButton.Connect(
            "pressed", this, "OnClickWand"
        );
        wandEntry.MyGemUI.Button.TextureNormal = wand.Texture();
        wandEntry.Labels[0].BbcodeText = wand.DisplayName();

        var (cgLow, cgHigh) = AllBuyableCGs();
        ListGems(cgLow);
        ListGems(cgHigh);
    }

    public void OnClickWand()
    {
        Selected = GameState.Persistent.MyWand;
        Hide();
    }

    public void ListBuyable()
    {
        _mode = Mode.BUY;
        Title.Text = "Gem shop";
        _lastEntry = null;

        // header
        _header = new GemEntry(null);
        OuterVBox.AddChild(_header);
        OuterVBox.MoveChild(_header, 1);
        _header.PresetBigMidMoney();
        _header.Pad();
        _header.MyGemUI.Empty();
        _header.Labels[1].BbcodeText = (
            "[center][color=#00ff00]Owned[/color][/center]"
        );
        _header.Labels[2].BbcodeText = (
            "[center][color=yellow]Price[/color][/center]"
        );

        var (cgLow, cgHigh) = AllBuyableCGs();
        ListGems(cgLow);
        ListGems(AllBuyableGems());
        ListGems(cgHigh);
    }

    private void Finish()
    {
        if (_rotated) {
            Selected = null;
        }
        EmitSignal("finished");
        QueueFree();
    }
    public void OnPopupHide()
    {
        Finish();
    }

    public override void _Process(float delta)
    {
        if (_header == null) return;
        if (_lastEntry == null) return;
        _header.RectSize = _lastEntry.RectSize;
    }

    private bool BuyGem(Gem gem) {
        Simplest price = NPC.Shop.PriceOf(gem);
        if (price <= GameState.Persistent.Money) {
            GameState.Persistent.Sema.WaitOne();
            GameState.Persistent.Money = Simplest.Eval(
                GameState.Persistent.Money, 
                Operator.MINUS, price
            );
            GameState.Persistent.Sema.Release();
            if (gem is CustomGem cG) {
                if (cG.MetaLevel.MyRank == Rank.FINITE) {
                    int metaLevel = (int)cG.MetaLevel.K;
                    var (nOwned, _cG) = GameState.Persistent.HasCustomGems[metaLevel];
                    Shared.Assert(cG == _cG);
                    GameState.Persistent.Sema.WaitOne();
                    GameState.Persistent.HasCustomGems[metaLevel] = (
                        nOwned + 1, cG
                    );
                    GameState.Persistent.Sema.Release();
                }
            } else {
                GameState.Persistent.Sema.WaitOne();
                GameState.Persistent.HasGems[gem.Name()] ++;
                GameState.Persistent.Sema.Release();
            }
            return true;
        }
        return false;
    }

    private MagicItem AnyEmbedderOf(Gem gem) {
        int nInWand = CountGemsInWand(gem);
        if (nInWand != 0)
            return GameState.Persistent.MyWand;
        CustomGem cG;
        foreach (var entry in GameState.Persistent.HasCustomGems)
        {
            cG = entry.Value.Item2;
            if (cG != null)
            {
                if (CountGemsInCircuit(gem, cG.MyCircuit) != 0) {
                    return cG;
                }
            }
        }
        cG = GameState.Persistent.MyTypelessGem;
        if (cG != null)
        {
            if (CountGemsInCircuit(gem, cG.MyCircuit) != 0) {
                return cG;
            }
        }
        throw new Shared.ObjectStateIllegal();
    }
}
