using VRage.Game.Components;

namespace CoreSystems.Support
{
    public partial class AiComponent : MyEntityComponentBase
    {
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
        }
        
        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
        }


        internal void OnAddedToSceneTasks()
        {
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
        }

        public override bool IsSerialized()
        {
            if (Session.I.IsServer)
            {
                Ai.Data.Save();
                Ai.Construct.Data.Save();
            }
            return false;
        }

        public override string ComponentTypeDebugString => "CoreSystems";
    }
}
