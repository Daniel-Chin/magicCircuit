using System;

public class Director
{
    public static Main MainUI;
    public static MagicEvent NowEvent;
    static Director()
    {
        NowEvent = null;
    }

    public static void CheckEvent()
    {
        if (NowEvent != null) return;
        if (!GameState.Persistent.Event_Intro)
        {
            StartEvent(new MagicEvent.Intro());
            return;
        }
        if (!GameState.Persistent.Event_Staff)
        {
            Wand staff = new Wand.Staff();
            staff.Init();
            GameState.Transient.NextSpawn = (SpawnableSpecial)staff;
            GameState.Transient.EnemiesTillNextSpawn = 0;
            return;
        }
        if (
            GameState.Persistent.MyWand is Wand.Staff
            && SpawnShopIf(Simplest.Finite(2), (new Gem.AddOne(), 0))
        ) {
            Console.WriteLine("hinting to buy +1");
            return;
        }
        if (
            GameState.Persistent.HasGems[new Gem.AddOne().Name()] <= 1
            && SpawnShopIf(Simplest.Finite(6), (new Gem.WeakMult(), 0))
        ) {
            Console.WriteLine("hinting to buy x1.4");
            return;
        }
        if (
            GameState.Persistent.MyWand is Wand.Staff
            && SpawnShopIf(Simplest.Finite(9), (new Gem.AddOne(), 1))
        ) {
            Console.WriteLine("hinting to buy 2nd +1");
            return;
        }
        if (
            GameState.Persistent.MyWand is Wand.Staff
            && SpawnShopIf(Simplest.Finite(13))
        ) {
            Console.WriteLine("hinting to buy guitar");
            return;
        }
        if (FillGuitar())
            return;
        if (JumperMk1())
            return;
        if (
            GameState.Persistent.MyWand is Wand.Guitar
            && SpawnShopIf(Simplest.Finite(25))
        ) {
            Console.WriteLine("hinting to buy ricecooker");
            return;
        }
        if (
            GameState.Persistent.MyWand is Wand.Ricecooker
        ) {
            if (
                GameState.Persistent.HasGems[new Gem.Stochastic(false).Name()] == 0
                && SpawnShopIf(NPC.Shop.PriceOf(new Gem.Stochastic(false)))
            ) {
                Console.WriteLine("hint to buy stochastic");
                return;
            }
            if (
                GameState.Persistent.HasGems[new Gem.Stochastic(false).Name()] == 0
                && GameState.Persistent.Money >= Simplest.Eval(
                    NPC.Shop.PriceOf(new Gem.Stochastic(false)), 
                    Operator.TIMES, Simplest.Finite(2.5)
                )
            ) {
                Console.WriteLine("drop stochastic");
                GameState.Transient.NextSpawn = new DroppedGem(){
                    MyGem = new Gem.Stochastic(false), 
                };
            }
            if (
                !GameState.Persistent.HasCustomGems.ContainsKey(0)
                && GameState.Persistent.Event_JumperStage >= 1
                && GameState.Persistent.SmithTip == MagicEvent.Smithing.SMITH_TIP_TOP
            ) {
                SetNPCToSpawn(new NPC.GemExpert());
                return;
            }
            if (
                GameState.Persistent.Event_JumperStage == 1
                && GameState.Persistent.unlockedAnyMetaCustomGem()
            ) {
                SetNPCToSpawn(new NPC.Inventor());
                return;
            }
        }
    }

    public static void StartEvent(MagicEvent e)
    {
        if (NowEvent is MagicEvent.Experting) {
            NowEvent = null;
        }
        Shared.Assert(NowEvent == null);
        NowEvent = e;
        NowEvent.NextStep();
    }

    public static void SpecialSpawned(SpawnableSpecial s)
    {
        if (s is NPC.WandSmith) {
            GameState.Transient.NextSpawn = null;
        }
        GameState.Persistent.Sema.WaitOne();
        GameState.Persistent.Loneliness_GemExpert ++;
        GameState.Persistent.Loneliness_Shop ++;
        GameState.Persistent.Loneliness_WandSmith ++;
        GameState.Persistent.Sema.Release();
    }
    public static void SpecialDespawned(
        SpawnableSpecial s, bool exposed
    )
    {
        if (! exposed)
            return;
        if (GameState.Transient.NextSpawn == null)
            return;
        if (GameState.Transient.NextSpawn is Wand.Staff)
            return;
        if (GameState.Transient.NextSpawn == s)
            GameState.Transient.EventRejected();
    }

    public static void OnEventStepComplete()
    {
        NowEvent.NextStep();
    }

    public static void Process(float dt)
    {
        if (NowEvent != null) {
            NowEvent.Process(dt);
            return;
        }
    }

    public static void EventFinished()
    {
        NowEvent = null;
        CheckEvent();
    }

    public static bool CanSpawnNonevent()
    {
        if (
            GameState.Transient.NextSpawn != null
            && GameState.Transient.EnemiesTillNextSpawn == 0
        ) return false;
        if (
            !GameState.Persistent.Event_Intro
            || !GameState.Persistent.Event_Staff
        ) return false;
        return true;
    }

    public static void WandAttacked(Attack attack)
    {
        if (NowEvent is MagicEvent.Staff e)
        {
            e.Attacked = true;
            e.NextStep();
        }
        if (attack.Mana.MyRank != Rank.FINITE) {
            GameState.Persistent.Sema.WaitOne();
            GameState.Persistent.MadeInf = true;
            GameState.Persistent.Sema.Release();
        }
    }

    public static void PauseWorld() {
        MainUI.MyWorld.MyMageUI.Resting();
        GameState.Transient.WorldPaused = true;
    }
    public static void UnpauseWorld() {
        GameState.Transient.WorldPaused = false;
    }

    private static bool SpawnShopIf(
        Simplest money, Tuple<Gem, int> maxGems
    ) {
        if (!(GameState.Persistent.Money >= money))
            return false;
        if (maxGems is Tuple<Gem, int> _maxGems) {
            var (gem, num) = _maxGems;
            if (GameState.Persistent.HasGems[gem.Name()] > num)
                return false;
        }
        SetNPCToSpawn(new NPC.Shop());
        return true;
    }
    private static bool SpawnShopIf(
        Simplest money
    ) {
        return SpawnShopIf(money, null);
    }
    private static bool SpawnShopIf(
        Simplest money, (Gem, int) maxGems
    ) {
        return SpawnShopIf(money, new Tuple<Gem, int>(
            maxGems.Item1, maxGems.Item2
        ));
    }

    private static bool FillGuitar() {
        if (! (GameState.Persistent.MyWand is Wand.Guitar))
            return false;
        double needMoney = 0;
        Gem gem;
        gem = new Gem.Focus(null);
        if (GameState.Persistent.CountGemsOwned(gem).Equals(Simplest.Zero()))
            needMoney += NPC.Shop.PriceOf(gem).K;
        gem = new Gem.Mirror(false);
        if (GameState.Persistent.CountGemsOwned(gem) <= Simplest.One())
            needMoney += NPC.Shop.PriceOf(gem, 1).K;
        if (GameState.Persistent.CountGemsOwned(gem).Equals(Simplest.Zero()))
            needMoney += NPC.Shop.PriceOf(gem, 0).K;
        if (needMoney == 0)
            return false;
        return SpawnShopIf(Simplest.Finite(needMoney));
    }

    private static bool JumperMk1() {
        if (GameState.Persistent.Event_JumperStage != 0)
            return false;
        if (GameState.Persistent.MyWand is Wand.Staff)
            return false;
        if (GameState.Persistent.Money <= Simplest.Finite(18))
            return false;
        SetNPCToSpawn(new NPC.Inventor());
        return true;
    }

    public static void JumpBegan() {
        if (NowEvent is MagicEvent.Jumping e) {
            e.JumpBegan();
        }
    }
    public static void JumpFinished(Simplest jumpMana) {
        if (NowEvent is MagicEvent.Jumping e) {
            e.JumpFinished(jumpMana);
        }
    }

    private static void SetNPCToSpawn(NPC npc) {
        if (
            GameState.Transient.NextSpawn != null &&
            GameState.Transient.NextSpawn.GetType() == npc.GetType()
        )
            return; 
        if (GameState.Transient.NextSpawn == null) {
            Console.WriteLine("Setting " + npc + " to spawn next");
            GameState.Transient.NextSpawn = npc;
            GameState.Transient.EnemiesTillNextSpawn = 0;
        }
    }

    public static void EnemyDied() {
        if (GameState.Persistent.HasGems[new Gem.StrongMult().Name()] != 0) {
            GameState.Persistent.Sema.WaitOne();
            GameState.Persistent.KillsSinceStrongMult ++;
            GameState.Persistent.Sema.Release();
        }
        if (GameState.Persistent.HasGems[new Gem.Stochastic(false).Name()] != 0) {
            GameState.Persistent.Sema.WaitOne();
            GameState.Persistent.KillsSinceStochastic ++;
            GameState.Persistent.Sema.Release();
        }
        CheckEvent();
    }
}
