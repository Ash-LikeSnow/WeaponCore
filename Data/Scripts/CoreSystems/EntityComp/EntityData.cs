using System;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;

namespace CoreSystems
{
    public class CompData
    {
        public CoreComponent BaseComp;
        public ProtoRepo ProtoRepoBase;

        public void Init (CoreComponent comp)
        {
            BaseComp = comp;
        }

        public void StorageInit()
        {
            if (BaseComp.CoreEntity.Storage == null) 
            {
                BaseComp.CoreEntity.Storage = new MyModStorageComponent { [Session.I.CompDataGuid] = "" };
            }
        }

        public void Save()
        {
            if (BaseComp.CoreEntity.Storage == null) return;
            if (ProtoRepoBase != null)
            {
                switch (BaseComp.Type)
                {
                    case CoreComponent.CompType.Weapon:
                        BaseComp.CoreEntity.Storage[Session.I.CompDataGuid] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary((ProtoWeaponRepo)ProtoRepoBase));
                        break;
                    case CoreComponent.CompType.Upgrade:
                        BaseComp.CoreEntity.Storage[Session.I.CompDataGuid] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary((ProtoUpgradeRepo)ProtoRepoBase));
                        break;
                    case CoreComponent.CompType.Support:
                        BaseComp.CoreEntity.Storage[Session.I.CompDataGuid] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary((ProtoSupportRepo)ProtoRepoBase));
                        break;
                    case CoreComponent.CompType.Control:
                        BaseComp.CoreEntity.Storage[Session.I.CompDataGuid] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary((ProtoControlRepo)ProtoRepoBase));
                        break;
                }
            }
        }

        public enum DataState
        {
            Load,
            Reset,
        }

        public void DataManager (DataState change)
        {
            switch (BaseComp.Type)
            {
                case CoreComponent.CompType.Upgrade:
                    ((Upgrade.UpgradeComponent)BaseComp).Data.Change(change);
                    break;
                case CoreComponent.CompType.Support:
                    ((SupportSys.SupportComponent)BaseComp).Data.Change(change);
                    break;
                case CoreComponent.CompType.Weapon:
                    ((Weapon.WeaponComponent)BaseComp).Data.Change(change);
                    break;
                case CoreComponent.CompType.Control:
                    ((ControlSys.ControlComponent)BaseComp).Data.Change(change);
                    break;
            }
        }
    }
}
