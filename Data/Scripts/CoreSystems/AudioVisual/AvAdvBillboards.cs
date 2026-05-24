using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using static VRageRender.MyBillboard;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static CoreSystems.Projectiles.Projectile;
using static CoreSystems.Support.AdvBillboards;

namespace CoreSystems.Support
{
    internal class AvAdvBillboards
    {
        // REFERENCES TO AMMO DEF!
        public LineConstants[] LineDefs;
        public TrailConstants[] TrailDefs;
        public BillboardConstants[] BillboardDefs;

        public List<AdvBLineCache> DrawnLines = new List<AdvBLineCache>();
        public List<MyQueue<AdvBLineCache>> Trails = new List<MyQueue<AdvBLineCache>>();
        public List<BillboardInfo> Billboards = new List<BillboardInfo>();
        public AvShot Av;
        public MatrixD ProjectileMatrix;
        public Vector3 Velocity;
        public Vector3 PrevVelocity;
        public Vector3D PrevPosition;
        public Vector3 Grav;
        public int ClientAVLevel;
        public int CurrentLifetime;
        public bool ModelRotation;

        const int LONG_TRAIL_THRESHOLD = 100; // kinda arbitrary number, if another number gives better perf change it
        internal void Init(Projectile p, ProInfo info, AvShot av)
        {
            var session = Session.I;
            var avr = session.Av;
            Av = av;

            ClientAVLevel = session.ClientAvLevel;

            ModelRotation = info.AmmoDef.Const.AdvBillboardSettings.UseModelRotation;
            LineDefs = info.AmmoDef.Const.AdvBillboardSettings.Lines;
            TrailDefs = info.AmmoDef.Const.AdvBillboardSettings.Trails;
            BillboardDefs = info.AmmoDef.Const.AdvBillboardSettings.Billboards;

            Grav = p.Gravity;

            if (LineDefs.Length > 0)
            {
                int maxLines = 0;
                foreach (var linedef in LineDefs)
                {
                    maxLines += (int)Math.Ceiling((float)linedef.TimeRendered / (linedef.DelayBetweenSpawns + 1));
                }
                DrawnLines.EnsureCapacity(maxLines);
            }

            if (TrailDefs.Length > 0)
            {
                Trails.EnsureCapacity(TrailDefs.Length);
                for (int i = 0; i < TrailDefs.Length; i++)
                {
                    var trailLineCount = (int)Math.Ceiling((float)TrailDefs[i].TimeRendered / (TrailDefs[i].DelayBetweenSpawns + ClientAVLevel + 1));
                    var cache = trailLineCount > LONG_TRAIL_THRESHOLD
                        ? avr.AdvBLongTrailCacheLists.Count > 0 ? avr.AdvBLongTrailCacheLists.Pop() : new MyQueue<AdvBLineCache>(trailLineCount)
                        : avr.AdvBTrailCacheLists.Count > 0 ? avr.AdvBTrailCacheLists.Pop() : new MyQueue<AdvBLineCache>(trailLineCount);

                    Trails.Add(cache);
                }
            }

            if (BillboardDefs.Length > 0)
            {
                Billboards.EnsureCapacity(BillboardDefs.Length);
                for (int i = 0; i < BillboardDefs.Length; i++)
                {
                    var binfo = avr.BillboardCache.Count > 0 ? avr.BillboardCache.Pop() : new BillboardInfo();
                    binfo.IsTri = BillboardDefs[i].IsTri;
                    binfo.Render = false;
                    Billboards.Add(binfo);
                }
            }
        }

        internal void Clean()
        {
            var avr = Session.I.Av;

            avr.AdvBLines.AddRange(DrawnLines);
            DrawnLines.Clear();

            for (int i = TrailDefs.Length - 1; i >= 0; i--)
            {
                var trailLineCount = (int)Math.Ceiling((float)TrailDefs[i].TimeRendered / (TrailDefs[i].DelayBetweenSpawns + ClientAVLevel + 1));
                var longTrail = trailLineCount > LONG_TRAIL_THRESHOLD;

                for (int j = Trails[i].Count - 1; j >= 0; j--)
                    avr.AdvBLines.Add(Trails[i][j]);

                Trails[i].Clear();

                if (longTrail)
                    avr.AdvBLongTrailCacheLists.Push(Trails[i]);
                else
                    avr.AdvBTrailCacheLists.Push(Trails[i]);
            }
            Trails.Clear();
            ClientAVLevel = 0;
            CurrentLifetime = 0;

            foreach (var billboard in Billboards)
            {
                billboard.IsTri = false;
                billboard.Render = false;
                avr.BillboardCache.Push(billboard);
            }
            Billboards.Clear();

            LineDefs = null;
            TrailDefs = null;
            BillboardDefs = null;

            Av = null;
            ProjectileMatrix = MatrixD.Identity;
            Velocity = Vector3D.Zero;
            PrevVelocity = Vector3D.Zero;
            PrevPosition = Vector3D.Zero;
            Grav = Vector3.Zero;
            ClientAVLevel = 0;
            CurrentLifetime = 0;
            ModelRotation = false;
    }
    }
    internal class AdvBLineCache
    {
        public bool LerpColor;
        public bool LerpWidth;
        public bool AlwaysDraw;
        public uint EndTick;
        public uint StartTick;
        public float StartWidth;
        public float MaxDistSq;
        public MyStringId Material;
        public MyBillboard Billboard = new MyBillboard();
        public Vector3 Velocity;
        public Vector4 StartColor;
        public Vector3D Start;
        public Vector3D End;
    }
    // if only this could be value type
    internal class BillboardInfo
    {
        public MyBillboard Billboard;
        public bool Render;
        public bool IsTri;
    }
}
