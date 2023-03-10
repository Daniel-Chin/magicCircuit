using System;
using System.IO;
using System.Threading;
using System.Linq;

using System.Collections.Generic;
using Godot;

public class GameState
{
    public static PersistentClass Persistent;
    public static TransientClass Transient;
    static GameState()
    {
        Persistent = new PersistentClass();
        Transient = new TransientClass();
    }

    public class PersistentClass : JSONable
    {
        public System.Threading.Semaphore Sema;
        public Simplest Money { get; set; }
        public double Location_theta { get; set; }
        public Simplest Location_dist { get; set; }
        public Dictionary<string, int> HasGems { get; set; }
        public Dictionary<int, (int, CustomGem)> HasCustomGems { get; set; }
        public CustomGem MyTypelessGem { get; set; }
        public Wand MyWand { get; set; }

        // events. true means completed.
        public bool Event_Intro { get; set; }
        public bool Event_Staff { get; set; }
        public int Event_JumperStage { get; set; }

        public int Loneliness_Shop { get; set; }
        public int Loneliness_GemExpert { get; set; }
        public int Loneliness_WandSmith { get; set; }
        public int ShopTip { get; set; }
        public int SmithTip { get; set; }
        public int KillsSinceStrongMult { get; set; }
        public int KillsSinceStochastic { get; set; }
        public bool MadeInf { get; set; }
        public bool MadeInfMeanWand { get; set; }
        public PersistentClass()
        {
            Sema = new System.Threading.Semaphore(1, 1);
            Money = Simplest.Zero();
            Location_theta = 0;
            Location_dist = Simplest.Zero();
            MyWand = null;
            HasGems = new Dictionary<string, int>();
            HasCustomGems = new Dictionary<int, (int, CustomGem)>();
            MyTypelessGem = null;

            for (int i = 0; i < Gem.N_IDS; i++) {
                HasGems[Gem.FromID(i).Name()] = 0;
            }

            Event_Intro = false;
            Event_Staff = false;
            Event_JumperStage = 0;
            Loneliness_Shop = 0;
            Loneliness_GemExpert = 0;
            Loneliness_WandSmith = 0;

            ShopTip = 0;
            SmithTip = 0;
            KillsSinceStrongMult = 0;
            KillsSinceStochastic = 0;
            MadeInf = false;
            MadeInfMeanWand = false;
        }

        public void ToJSON(StreamWriter writer)
        {
            writer.WriteLine("[");
            Money.ToJSON(writer);
            writer.Write(Location_theta);
            writer.WriteLine(',');
            Location_dist.ToJSON(writer);

            writer.WriteLine("[");
            foreach (var entry in HasGems)
            {
                JSON.Store(entry.Key, writer);
                writer.Write(entry.Value);
                writer.WriteLine(',');
            }
            writer.WriteLine("],");

            writer.WriteLine("[");
            foreach (var entry in HasCustomGems.OrderBy(
                x => x.Key
            )) {
                writer.Write(entry.Key);
                writer.WriteLine(',');
                writer.Write(entry.Value.Item1);
                writer.WriteLine(',');
                entry.Value.Item2.ToJSON(writer, false);
            }
            writer.WriteLine("],");

            if (MyTypelessGem == null)
            {
                writer.WriteLine("null,");
            }
            else
            {
                MyTypelessGem.ToJSON(writer, false);
            }
            if (MyWand == null)
            {
                writer.WriteLine("null,");
            }
            else
            {
                MyWand.ToJSON(writer);
            }

            JSON.Store(Event_Intro, writer);
            JSON.Store(Event_Staff, writer);
            writer.Write(Event_JumperStage);
            writer.WriteLine(',');
            writer.Write(Loneliness_Shop);
            writer.WriteLine(',');
            writer.Write(Loneliness_GemExpert);
            writer.WriteLine(',');
            writer.Write(Loneliness_WandSmith);
            writer.WriteLine(',');
            writer.Write(ShopTip);
            writer.WriteLine(',');
            writer.Write(SmithTip);
            writer.WriteLine(',');
            writer.Write(KillsSinceStrongMult);
            writer.WriteLine(',');
            writer.Write(KillsSinceStochastic);
            writer.WriteLine(',');
            JSON.Store(MadeInf, writer);
            JSON.Store(MadeInfMeanWand, writer);

            writer.WriteLine("],");
        }
        public void FromJSON(StreamReader reader)
        {
            Shared.Assert(reader.ReadLine().Equals("["));

            Money = Simplest.FromJSON(reader);
            Location_theta = Double.Parse(JSON.NoLast(reader));
            Location_dist = Simplest.FromJSON(reader);

            Shared.Assert(reader.ReadLine().Equals("["));
            while (!JSON.DidArrayEnd(reader))
            {
                string key = JSON.ParseString(reader);
                int value = Int32.Parse(JSON.NoLast(reader));
                HasGems[key] = value;
            }

            Shared.Assert(reader.ReadLine().Equals("["));
            while (!JSON.DidArrayEnd(reader))
            {
                int key = Int32.Parse(JSON.NoLast(reader));
                int n_owned = Int32.Parse(JSON.NoLast(reader));
                CustomGem cG = (CustomGem)Gem.FromJSON(reader, false);
                HasCustomGems[key] = (n_owned, cG);
            }

            MyTypelessGem = (CustomGem)Gem.FromJSON(reader, false);
            MyWand = Wand.FromJSON(reader);

            Event_Intro = JSON.ParseBool(reader);
            Event_Staff = JSON.ParseBool(reader);
            Event_JumperStage = Int32.Parse(JSON.NoLast(reader));
            Loneliness_Shop = Int32.Parse(JSON.NoLast(reader));
            Loneliness_GemExpert = Int32.Parse(JSON.NoLast(reader));
            Loneliness_WandSmith = Int32.Parse(JSON.NoLast(reader));
            ShopTip = Int32.Parse(JSON.NoLast(reader));
            SmithTip = Int32.Parse(JSON.NoLast(reader));
            KillsSinceStrongMult = Int32.Parse(JSON.NoLast(reader));
            KillsSinceStochastic = Int32.Parse(JSON.NoLast(reader));
            MadeInf = JSON.ParseBool(reader);
            MadeInfMeanWand = JSON.ParseBool(reader);

            Shared.Assert(reader.ReadLine().Equals("],"));

            Persistent.Ready();
        }
        public void Ready()
        {
            foreach (var item in HasCustomGems)
            {
                item.Value.Item2.Eval();
            }
            if (MyTypelessGem != null)
            {
                MyTypelessGem.Eval();
            }

        }

        public bool HasAnyNonCGem() {
            foreach (var item in HasGems)
            {
                if (item.Value != 0) return true;
            }
            return false;
        }
        public bool unlockedAnyMetaCustomGem() {
            foreach (var entry in HasCustomGems)
            {
                if (entry.Key == 0)
                    continue;
                return true;
            }
            return false;
        }

        public Simplest CountGemsOwned(Gem gem)
        {
            if (gem is CustomGem cG)
            {
                (int, CustomGem) HasCG = (0, null);
                if (cG.MetaLevel.MyRank == Rank.FINITE)
                {
                    if (HasCustomGems.TryGetValue(
                        (int)cG.MetaLevel.K, out HasCG
                    ))
                        return new Simplest(Rank.FINITE, HasCG.Item1);
                    return Simplest.Zero();
                }
                else
                {
                    // typeless
                    if (MyTypelessGem == null)
                        return Simplest.Zero();
                    return Simplest.W();
                }
            }
            int n = 0;
            if (!HasGems.TryGetValue(gem.Name(), out n))
                n = 0;
            return new Simplest(Rank.FINITE, n);
        }
    }

    public class TransientClass
    {
        public Simplest Mana;
        public bool WorldPaused;
        public Vector2 LocationOffset;
        public SpawnableSpecial NextSpawn;
        public int EnemiesTillNextSpawn;
        public Vector2 LastLocationNoneventSpawn;
        public bool Jumping;
        public TransientClass()
        {
            Mana = Simplest.Zero();
            WorldPaused = false;
            LocationOffset = new Vector2(0, 0);
            NextSpawn = null;
            EnemiesTillNextSpawn = 0;
            LastLocationNoneventSpawn = new Vector2(0, 0);
            Jumping = false;
        }

        public void Update()
        {
            double dist;
            if (Persistent.Location_dist.MyRank == Rank.FINITE)
            {
                dist = Persistent.Location_dist.K;
            } else {
                dist = Params.INF_DISTANCE;
            }
            LocationOffset = new Vector2(
                (float)(dist * Math.Cos(Persistent.Location_theta)),
                (float)(dist * Math.Sin(Persistent.Location_theta))
            );
        }

        public void EventRejected() {
            EnemiesTillNextSpawn = Params.N_ENEMIES_AFTER_REJECT_EVENT;
        }
    }
}
