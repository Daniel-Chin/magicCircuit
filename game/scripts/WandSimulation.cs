using System;
using System.Collections.Generic;
using Godot;

public class WandSimulation
{
    private static readonly float BLING = .2f;
    private static readonly float BLING_DECAY = 5f;

    public SidePanel TheSidePanel { get; set; }
    public Wand MyWand { get; set; }
    public List<Particle> Particles { get; set; }
    public float TimeSinceLastEmit { get; set; }
    public float TimeSinceLastAdvect { get; set; }
    public float TimeSinceLastCrystalize { get; set; }

    public WandSimulation(SidePanel sidePanel)
    {
        TheSidePanel = sidePanel;
        MyWand = null;

        Particles = new List<Particle>();
        TimeSinceLastEmit = 0;
        TimeSinceLastAdvect = 0;
    }

    public void Process(float dt)
    {
        if (MyWand == null)
            return;

        TimeSinceLastEmit += dt;
        while (TimeSinceLastEmit >= Params.WAND_EMIT_INTERVAL)
        {
            TimeSinceLastEmit -= Params.WAND_EMIT_INTERVAL;
            if (Particles.Count < Params.WAND_MAX_PARTICLES)
            {
                List<(PointInt, Gem.Source)> sources = MyWand
                    .MyCircuit.FindAll<Gem.Source>();
                foreach (var sourceWithLocation in sources)
                {
                    var (location, source) = sourceWithLocation;
                    Particles.Add(new Particle(
                        location, source.Direction, Simplest.Ones(1)
                    ));
                }
            }
        }

        foreach (GemUI gemUI in TheSidePanel.MyCircuitUI.GemUIs)
        {
            gemUI.Tinter.Color = new Color(
                0, 1, 1,
                gemUI.Tinter.Color.a * (float)Math.Exp(-dt * BLING_DECAY)
            );
        }

        TimeSinceLastAdvect += dt;
        while (TimeSinceLastAdvect >= Params.WAND_ADVECT_INTERVAL)
        {
            TimeSinceLastAdvect -= Params.WAND_ADVECT_INTERVAL;
            List<Particle> newParticles = new List<Particle>();
            foreach (Particle p in Particles)
            {
                Particle newP = ApplyGem(p);
                if (newP == null)
                    continue;
                newParticles.Add(newP);
                newP.Location += newP.Direction;
            }
            Particles = newParticles;
        }

        TimeSinceLastCrystalize += dt;
    }

    private Particle ApplyGem(Particle p)
    {
        int x = p.Location.IntX;
        int y = p.Location.IntY;
        Gem gem = MyWand.MyCircuit.Field[x, y];
        var tinter = TheSidePanel.MyCircuitUI.GemUIs[x, y].Tinter;
        float tintBy = BLING;
        if (gem == null || gem is Gem.Wall)
            tintBy *= .4f;
        tinter.Color = new Color(
            0, 1, 1,
            Math.Min(1f, tinter.Color.a + tintBy)
        );
        if (gem == null)
        {
            return p;
        }
        if (gem is Gem.Drain)
        {
            TheSidePanel.ManaCrystalized(p.Mana[0]);
            TimeSinceLastCrystalize = 0;
        }
        return gem.Apply(p);
    }
}
