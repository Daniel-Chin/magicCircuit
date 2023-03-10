using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

public abstract class Gem : MagicItem
{
    public PointInt Size { get; set; }
    static Gem()
    {
        // verify consistency
        FromID(N_IDS - 1);
        bool ok = true;
        try
        {
            FromID(N_IDS);
            ok = false;
        }
        catch (Shared.ValueError) { }
        Shared.Assert(ok);
    }
    public Gem()
    {
        Size = new PointInt(1, 1);
    }
    public abstract Particle Apply(Particle input);
    public abstract string Explain(bool inCG);

    public void ToJSON(StreamWriter writer, bool inCircuit)
    {
        writer.WriteLine("[");
        switch (this)
        {
            case Source g:
                JSON.Store("source", writer);
                g.Direction.ToJSON(writer);
                break;
            case Drain g:
                JSON.Store("drain", writer);
                break;
            case Wall g:
                JSON.Store("wall", writer);
                break;
            case AddOne g:
                JSON.Store("addOne", writer);
                break;
            case WeakMult g:
                JSON.Store("weakMult", writer);
                break;
            case StrongMult g:
                JSON.Store("strongMult", writer);
                break;
            case Focus g:
                JSON.Store("focus", writer);
                g.Direction.ToJSON(writer);
                break;
            case Stochastic g:
                JSON.Store("stochastic", writer);
                JSON.Store(g.Orientation, writer);
                break;
            case Mirror g:
                JSON.Store("mirror", writer);
                JSON.Store(g.Orientation, writer);
                break;
            case CustomGem g:
                JSON.Store("customGem", writer);
                g.MetaLevel.ToJSON(writer);
                if (!inCircuit)
                {
                    g.MyCircuit.ToJSON(writer);
                }
                break;
            default:
                throw new Shared.ValueError();
        }
        writer.WriteLine("],");
    }
    public static Gem FromJSON(
        StreamReader reader, bool inCircuit
    )
    {
        string line = reader.ReadLine();
        if (line.Equals("null,"))
            return null;
        Shared.Assert(line.Equals("["));
        string gemType = JSON.ParseString(reader);
        Gem gem = null;
        // Console.WriteLine("JSON has " + gemType);
        switch (gemType)
        {
            case "source":
                gem = new Source(PointInt.FromJSON(reader));
                break;
            case "drain":
                gem = new Drain();
                break;
            case "wall":
                gem = new Wall();
                break;
            case "addOne":
                gem = new AddOne();
                break;
            case "weakMult":
                gem = new WeakMult();
                break;
            case "strongMult":
                gem = new StrongMult();
                break;
            case "focus":
                gem = new Focus(PointInt.FromJSON(reader));
                break;
            case "stochastic":
                gem = new Stochastic(JSON.ParseBool(reader));
                break;
            case "mirror":
                gem = new Mirror(JSON.ParseBool(reader));
                break;
            case "customGem":
                Simplest metaLevel = Simplest.FromJSON(reader);
                if (!inCircuit)
                {
                    CustomGem cG = new CustomGem(metaLevel);
                    if (metaLevel.MyRank == Rank.FINITE)
                    {
                        cG.MyCircuit = Circuit.FromJSON(reader, null);
                    }
                    else
                    {
                        cG.MyCircuit = Circuit.FromJSON(reader, cG);
                    }
                    cG.Eval();
                    gem = cG;
                }
                else
                {
                    if (
                        metaLevel.MyRank == Rank.FINITE
                        && GameState.Persistent.HasCustomGems.TryGetValue(
                            (int)metaLevel.K, out (int, CustomGem) iC
                        )
                    ) {
                        gem = iC.Item2;
                        break;
                    }
                    Shared.Assert(metaLevel.MyRank != Rank.FINITE);
                    CustomGem typeless = new CustomGem(metaLevel);
                    gem = typeless;
                }
                break;
            default:
                throw new Shared.ValueError();
        }
        Shared.Assert(reader.ReadLine().Equals("],"));
        return gem;
    }

    public class Source : Gem
    {
        public PointInt Direction;
        public Source(PointInt direction) : base()
        {
            Direction = direction;
        }
        public override Particle Apply(Particle input)
        {
            input.Direction = Direction;
            return input;
        }
        public override string Name()
        {
            return "source";
        }
        public override string DisplayName()
        {
            return "Radiator Gem";
        }
        public override string Explain(bool inCG)
        {
            if (inCG)
                return "Mana always enters from here.";
            return "Mana radiator. Emit 1 mana every now and then.";
        }
    }
    public class Drain : Gem
    {
        public override Particle Apply(Particle input)
        {
            return null;
        }
        public override string Name()
        {
            return "drain";
        }
        public override string DisplayName()
        {
            return "Mana Crystalizer";
        }
        public override string Explain(bool inCG)
        {
            if (inCG)
                return "Mana can only exit from here.";
            return "Mana crystalizer. Absorb mana and store it. Very loyal.";
        }
    }
    public class Wall : Gem
    {
        public override Particle Apply(Particle input)
        {
            return null;
        }
        public override string Name()
        {
            return "wall";
        }
        public override string DisplayName()
        {
            return "Blockage";
        }
        public override string Explain(bool inCG)
        {
            return "Blockage.";
        }
    }
    public class RemoveGem : Gem
    {
        public override Particle Apply(Particle input)
        {
            return input;
        }
        public override string Name()
        {
            return "transparent";
        }
        public override string DisplayName()
        {
            return "RemoveGem";
        }
        public override string Explain(bool inCG)
        {
            return "Remove gem.";
        }
    }
    public class AddOne : Gem
    {
        public override Particle Apply(Particle input)
        {
            input.Mana[0] = Simplest.Eval(
                input.Mana[0], Operator.PLUS, Simplest.One()
            );
            return input;
        }
        public override string Name()
        {
            return "addOne";
        }
        public override string DisplayName()
        {
            return "+1 Gem";
        }
        public override string Explain(bool inCG)
        {
            return "+1 to any mana flowing through it.";
        }
    }
    public abstract class Mult : Gem
    {
        protected abstract double Multiplier();
        public override Particle Apply(Particle input)
        {
            input.Multiply(Multiplier());
            return input;
        }
        public override string DisplayName()
        {
            return $"x{Multiplier()} Gem";
        }
        public override string Explain(bool inCG)
        {
            return $"Multiply any mana flowing through it by {Multiplier():#.#}.";
        }
    }
    public class WeakMult : Mult
    {
        protected override double Multiplier() {
            return 1.4;
        }  // 1.4**2 < 2
        public override string Name()
        {
            return "weakMult";
        }
    }
    public class StrongMult : Mult
    {
        protected override double Multiplier() {
            return 1.5;
        }  // 1.4 * 1.5 > 2
        public override string Name()
        {
            return "strongMult";
        }
    }
    // public class Doubler : Gem
    // {
    //     public Doubler() : base()
    //     {
    //         Size = new PointInt(2, 2);
    //     }
    //     public override Particle Apply(Particle input)
    //     {
    //         input.Multiply(2);
    //         return input;
    //     }
    // }
    public class Focus : Gem
    {
        public PointInt Direction;
        public Focus(PointInt direction) : base()
        {
            Direction = direction;
        }
        public override Particle Apply(Particle input)
        {
            input.Direction = Direction;
            return input;
        }
        public override string Name()
        {
            return "focus";
        }
        public override string DisplayName()
        {
            return "Focus Gem";
        }
        public override string Explain(bool inCG)
        {
            return "Focus mana from different directions.";
        }
    }
    public class Mirror : Gem
    {
        public bool Orientation;
        private Matrix<double> _transform;
        public Mirror(bool orientation) : base()
        {
            Orientation = orientation;
            if (orientation)
            {
                _transform = Matrix<double>.Build.DenseOfArray(
                    new double[2, 2] {
                        {0, 1},
                        {1, 0},
                    }
                );
            }
            else
            {
                _transform = Matrix<double>.Build.DenseOfArray(
                    new double[2, 2] {
                        {0, -1},
                        {-1, 0},
                    }
                );
            }
        }
        public override Particle Apply(Particle input)
        {
            input.Direction = PointInt.FromVector(
                _transform * input.Direction.ToVector()
            );
            return input;
        }
        public override string Name()
        {
            return "mirror";
        }
        public override string DisplayName()
        {
            return "Mirror Gem";
        }
        public override string Explain(bool inCG)
        {
            return "Reflect mana.";
        }
    }
    public class Stochastic : Mirror
    {
        public Stochastic(bool orientation) : base(orientation) { }
        public Particle ApplyMirror(Particle input)
        {
            return base.Apply(input);
        }
        public override Particle Apply(Particle input)
        {
            if (Shared.Rand.Next() % 2 == 0)
            {
                return input;
            }
            else
            {
                return base.Apply(input);
            }
        }
        public Particle[] Superposition(Particle input)
        {
            Particle[] particles = new Particle[2];
            input.Multiply(.5);
            particles[0] = input.Copy();
            particles[1] = base.Apply(input);
            return particles;
        }
        public override string Name()
        {
            return "stochastic";
        }
        public override string DisplayName()
        {
            return "Stochastic Gem";
        }
        public override string Explain(bool inCG)
        {
            return "Reflect mana with probability = 50%.";
        }
    }

    public static readonly int N_IDS = 8;
    public static Gem FromID(int id)
    {
        switch (id)
        {
            case 0:
                return new Source(new PointInt(0, 1));
            case 1:
                return new Drain();
            case 2:
                return new AddOne();
            case 3:
                return new WeakMult();
            case 4:
                return new StrongMult();
            case 5:
                return new Focus(new PointInt(0, 1));
            case 6:
                return new Mirror(true);
            case 7:
                return new Stochastic(true);
            default:
                throw new Shared.ValueError();
        }
    }
}
