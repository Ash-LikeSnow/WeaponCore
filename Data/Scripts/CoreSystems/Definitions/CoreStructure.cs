using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Utils;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
namespace CoreSystems.Support
{

    public class CoreStructure
    {
        internal Dictionary<MyStringHash, CoreSystem> PartSystems;
        internal Dictionary<int, int> HashToId;

        internal MyStringHash[] PartHashes;
        internal bool MultiParts;
        internal int ConstructPartCap;
        internal int PrimaryPart;
        internal string ModPath;
        internal string SubtypeId;
        internal ulong ModId;
        internal Session Session;
        internal StructureTypes StructureType;
        internal EnittyTypes EntityType;
        internal float ApproximatePeakPowerCombined;
        internal float ActualPeakPowerCombined;
        internal float MaxPowerMW; // not touching the above 2's calculations so new var
        internal float CombinedIdlePower;
        internal int PowerPriority;
        internal enum EnittyTypes
        {
            Invalid,
            Rifle,
            Phantom,
            Block,
        }

        internal enum StructureTypes
        {
            Invalid,
            Weapon,
            Upgrade,
            Support,
            Control,
        }
    }

    public class WeaponStructure : CoreStructure
    {
        internal bool TurretAttached;
        internal double MaxLockRange = 10000;
        internal int DefaultLeadGroup = 0;
        internal WeaponStructure(Session session, KeyValuePair<string, Dictionary<string, MyTuple<string, string, string, string>>> tDef, List<WeaponDefinition> wDefList, string modPath)
        {
            MaxPowerMW = 0;
            Session = session;
            SubtypeId = tDef.Key;
            var map = tDef.Value;
            var numOfParts = wDefList.Count;

            if (modPath == null || numOfParts == 0) {
                Log.Line($"invalid modpath:{modPath == null} for {SubtypeId} - numOfParts:{numOfParts}");
                return;
            }

            MultiParts = numOfParts > 1;
            ModPath = modPath;
            MyObjectBuilder_Checkpoint.ModItem modItem;
            if (Session.ModInfo.TryGetValue(modPath, out modItem))
                ModId = modItem.PublishedFileId;

            var partHashes = new MyStringHash[numOfParts];
            var muzzleHashes = new MyStringHash[numOfParts];
            var partId = 0;
            PartSystems = new Dictionary<MyStringHash, CoreSystem>(MyStringHash.Comparer);
            HashToId = new Dictionary<int, int>();
            PrimaryPart = -1;
            var partCap = 0;
            foreach (var w in map)
            {
                var typeName = w.Value.Item1;
                WeaponDefinition weaponDef = null;
                foreach (var def in wDefList)
                {
                    if (def.HardPoint.PartName == typeName)
                    {
                        weaponDef = def;

                        if (DefaultLeadGroup == 0 && (!def.HardPoint.Ai.TurretAttached || def.HardPoint.Ai.OverrideLeads) && def.HardPoint.Ai.DefaultLeadGroup > 0)
                        {
                            DefaultLeadGroup = def.HardPoint.Ai.DefaultLeadGroup;
                        }
                    }
                }

                if (weaponDef?.Ammos == null || weaponDef.Ammos.Length == 0)
                {
                    Log.Line("CoreStructure failed to match PartName to typeName or ammo invalid");
                    return;
                }
                Session.WeaponValuesMap[weaponDef] = null;
                var muzzletNameHash = MyStringHash.GetOrCompute(w.Key);
                muzzleHashes[partId] = muzzletNameHash;
                var azimuthNameHash = MyStringHash.GetOrCompute(w.Value.Item2);
                var elevationNameHash = MyStringHash.GetOrCompute(w.Value.Item3);
                var spinNameHash = MyStringHash.GetOrCompute(w.Value.Item4);
                var partNameIdHash = MyStringHash.GetOrCompute(weaponDef.HardPoint.PartName + $" {partId}");
                partHashes[partId] = partNameIdHash;

                var cap = weaponDef.HardPoint.Other.ConstructPartCap;
                if (partCap == 0 && cap > 0) partCap = cap;
                else if (cap > 0 && partCap > 0 && cap < partCap) partCap = cap;

                if (weaponDef.HardPoint.Ai.PrimaryTracking && PrimaryPart < 0)
                    PrimaryPart = partId;

                var shrapnelNames = new HashSet<string>();
                for (int i = 0; i < weaponDef.Ammos.Length; i++)
                {
                    var ammo = weaponDef.Ammos[i];
                    if (!shrapnelNames.Contains(ammo.Fragment.AmmoRound) && !string.IsNullOrEmpty(ammo.Fragment.AmmoRound))
                        shrapnelNames.Add(ammo.Fragment.AmmoRound);
                    if (ammo.Pattern.Mode == AmmoDef.PatternDef.PatternModes.Both || ammo.Pattern.Mode == AmmoDef.PatternDef.PatternModes.Fragment)
                    {
                        foreach (var name in ammo.Pattern.Patterns) 
                        {
                            if (!shrapnelNames.Contains(name) && !string.IsNullOrEmpty(name))
                                shrapnelNames.Add(name);
                        }
                    }

                }

                var weaponAmmo = new WeaponSystem.AmmoType[weaponDef.Ammos.Length];
                for (int i = 0; i < weaponDef.Ammos.Length; i++)
                {
                    var ammo = weaponDef.Ammos[i];

                    if (string.IsNullOrEmpty(ammo.AmmoRound))
                    {
                        var newName = tDef.Key + "-"+ i;
                        Log.Line($"[!!! MOD ERROR !!!] Invalid AmmoName for weapon [{tDef.Key}] --- Forcing ammo name to: {newName}");
                        ammo.AmmoRound = newName;
                    }
                    var ammoDefId = new MyDefinitionId();
                    var ejectionDefId = new MyDefinitionId();

                    var ammoEnergy = ammo.AmmoMagazine == string.Empty || ammo.AmmoMagazine == "Energy";
                    foreach (var def in Session.AllDefinitions)
                    {
                        if (ammoEnergy && def.Id.SubtypeId.String == "Energy" || def.Id.SubtypeId.String == ammo.AmmoMagazine)
                            ammoDefId = def.Id;

                        if (ammo.Ejection.Type == AmmoDef.EjectionDef.SpawnType.Item && !string.IsNullOrEmpty(ammo.Ejection.CompDef.ItemName) && def.Id.SubtypeId.String == ammo.Ejection.CompDef.ItemName)
                            ejectionDefId = def.Id;
                    }


                    Session.AmmoValuesMap[ammo] = null;
                    var ammoType = new WeaponSystem.AmmoType(ammo,  ammoDefId, ejectionDefId, "*" + ammo.AmmoRound, shrapnelNames.Contains(ammo.AmmoRound));
                    Session.AmmoDefIds[ammoDefId] = ammoType;

                    Session.AmmoMaps[tDef.Key][ammoType.AmmoDef.AmmoRound] = ammoType;
                    weaponAmmo[i] = ammoType;
                }

                var partHash = (tDef.Key + partNameIdHash + elevationNameHash + muzzletNameHash + azimuthNameHash).GetHashCode();
                HashToId.Add(partHash, partId);
                var coreSystem = new WeaponSystem(this, partNameIdHash, muzzletNameHash, azimuthNameHash, elevationNameHash, spinNameHash, weaponDef, typeName, weaponAmmo, partHash, partId);

                MyDefinitionId typeId;
                if (Session.CoreSystemsDefs.TryGetValue(SubtypeId, out typeId))
                {
                    List<Session.WeaponMagMap> list;
                    if (!Session.SubTypeIdToWeaponMagMap.TryGetValue(typeId, out list))
                    {
                        list = new List<Session.WeaponMagMap>();
                        Session.SubTypeIdToWeaponMagMap[typeId] = list;
                    }

                    for (int i = 0; i < weaponAmmo.Length; i++)
                    {
                        var ammo = weaponAmmo[i];
                        list.Add(new Session.WeaponMagMap { WeaponId = coreSystem.WeaponId, AmmoType = ammo });

                        if (ammo.AmmoDef.NpcSafe)
                        {
                            List<Session.WeaponMagMap> list2;
                            if (!Session.SubTypeIdToNpcSafeWeaponMagMap.TryGetValue(typeId, out list2))
                            {
                                list2 = new List<Session.WeaponMagMap>();
                                Session.SubTypeIdToNpcSafeWeaponMagMap[typeId] = list2;
                            }

                            list2.Add(new Session.WeaponMagMap {WeaponId = coreSystem.WeaponId, AmmoType = ammo});
                        }

                    }

                    if (coreSystem.Values.HardPoint.NpcSafe)
                        Session.NpcSafeWeaponDefs[SubtypeId] = typeId;
                }

                if (coreSystem.MaxLockRange > MaxLockRange)
                    MaxLockRange = coreSystem.MaxLockRange;

                if (coreSystem.Values.HardPoint.Ai.TurretAttached && !TurretAttached)
                    TurretAttached = true;
                ApproximatePeakPowerCombined += coreSystem.ApproximatePeakPower;
                MaxPowerMW += coreSystem.WeaponAmmoMaxPowerMW;
                CombinedIdlePower += coreSystem.WConst.IdlePower;

                PartSystems.Add(partNameIdHash, coreSystem);
                partId++;
            }
            ActualPeakPowerCombined = ApproximatePeakPowerCombined / 60;

            if (PrimaryPart == -1)
                PrimaryPart = 0;

            ConstructPartCap = partCap;
            PartHashes = partHashes;

            var system = PartSystems[PartHashes[PrimaryPart]];
            StructureType = StructureTypes.Weapon;
            EntityType = system.PartType == HardwareDef.HardwareType.BlockWeapon ? EnittyTypes.Block : system.PartType == HardwareDef.HardwareType.HandWeapon ? EnittyTypes.Rifle : EnittyTypes.Phantom;
        }
    }

    internal class UpgradeStructure : CoreStructure
    {
        internal UpgradeStructure(Session session, KeyValuePair<string, Dictionary<string, MyTuple<string, string, string, string>>> tDef, List<UpgradeDefinition> wDefList, string modPath)
        {
            Session = session;
            var map = tDef.Value;
            var numOfParts = wDefList.Count;
            MultiParts = numOfParts > 1;
            ModPath = modPath;
            
            MyObjectBuilder_Checkpoint.ModItem modItem;
            if (Session.ModInfo.TryGetValue(modPath, out modItem))
                ModId = modItem.PublishedFileId;

            var partHashes = new MyStringHash[numOfParts];
            var partId = 0;
            PartSystems = new Dictionary<MyStringHash, CoreSystem>(MyStringHash.Comparer);
            HashToId = new Dictionary<int, int>();
            PrimaryPart = -1;
            var partCap = 0;
            foreach (var w in map)
            {
                var typeName = w.Value.Item1;
                UpgradeDefinition upgradeDef = null;
                foreach (var def in wDefList)
                    if (def.HardPoint.PartName == typeName) upgradeDef = def;

                if (upgradeDef == null)
                {
                    Log.Line("CoreStructure failed to match PartName to typeName");
                    return;
                }

                var partNameIdHash = MyStringHash.GetOrCompute(upgradeDef.HardPoint.PartName + $" {partId}");
                partHashes[partId] = partNameIdHash;

                var cap = upgradeDef.HardPoint.Other.ConstructPartCap;
                if (partCap == 0 && cap > 0) partCap = cap;
                else if (cap > 0 && partCap > 0 && cap < partCap) partCap = cap;

                if (PrimaryPart < 0)
                    PrimaryPart = partId;

                var partHash = (tDef.Key + partNameIdHash).GetHashCode();
                HashToId.Add(partHash, partId);
                var coreSystem = new UpgradeSystem(partNameIdHash, upgradeDef, typeName, partHash, partId);

                CombinedIdlePower += coreSystem.IdlePower;

                PartSystems.Add(partNameIdHash, coreSystem);
                partId++;
            }

            if (PrimaryPart == -1)
                PrimaryPart = 0;

            ConstructPartCap = partCap;
            PartHashes = partHashes;

            StructureType = StructureTypes.Upgrade;
            EntityType = EnittyTypes.Block;
        }
    }

    internal class SupportStructure : CoreStructure
    {
        internal bool CommonBlockRange;
        internal SupportStructure(Session session, KeyValuePair<string, Dictionary<string, MyTuple<string, string, string, string>>> tDef, List<SupportDefinition> wDefList, string modPath)
        {
            Session = session;
            var map = tDef.Value;
            var numOfParts = wDefList.Count;
            MultiParts = numOfParts > 1;
            ModPath = modPath;

            MyObjectBuilder_Checkpoint.ModItem modItem;
            if (Session.ModInfo.TryGetValue(modPath, out modItem))
                ModId = modItem.PublishedFileId;

            var partHashes = new MyStringHash[numOfParts];
            var partId = 0;
            PartSystems = new Dictionary<MyStringHash, CoreSystem>(MyStringHash.Comparer);
            HashToId = new Dictionary<int, int>();
            PrimaryPart = -1;
            var partCap = 0;
            var blockDistance = - 1;
            var commonBlockRange = true;
            foreach (var s in map)
            {
                var typeName = s.Value.Item1;
                SupportDefinition supportDef = null;
                foreach (var def in wDefList)
                    if (def.HardPoint.PartName == typeName) supportDef = def;

                if (supportDef == null)
                {
                    Log.Line("CoreStructure failed to match PartName to typeName");
                    return;
                }
                if (blockDistance < 0)
                    blockDistance = supportDef.Effect.BlockRange;

                if (blockDistance != supportDef.Effect.BlockRange)
                    commonBlockRange = false;

                var partNameIdHash = MyStringHash.GetOrCompute(supportDef.HardPoint.PartName + $" {partId}");
                partHashes[partId] = partNameIdHash;

                var cap = supportDef.HardPoint.Other.ConstructPartCap;
                if (partCap == 0 && cap > 0) partCap = cap;
                else if (cap > 0 && partCap > 0 && cap < partCap) partCap = cap;

                if (PrimaryPart < 0)
                    PrimaryPart = partId;

                var partHash = (tDef.Key + partNameIdHash).GetHashCode();
                HashToId.Add(partHash, partId);
                var coreSystem = new SupportSystem(partNameIdHash, supportDef, typeName, partHash, partId);

                CombinedIdlePower += coreSystem.IdlePower;

                PartSystems.Add(partNameIdHash, coreSystem);
                partId++;
            }

            CommonBlockRange = commonBlockRange;

            if (PrimaryPart == -1)
                PrimaryPart = 0;

            ConstructPartCap = partCap;
            PartHashes = partHashes;

            StructureType = StructureTypes.Support;
            EntityType = EnittyTypes.Block;
        }
    }

    internal class ControlStructure : CoreStructure
    {
        internal ControlStructure(Session session, MyStringHash idHash)
        {
            Session = session;
            StructureType = StructureTypes.Control;
            EntityType = EnittyTypes.Block;
            PartHashes = new MyStringHash[1] { idHash };
            var coreSystem = new ControlSystem();
            PartSystems = new Dictionary<MyStringHash, CoreSystem>(MyStringHash.Comparer);
            PartSystems.Add(idHash, coreSystem);
        }
    }

}
