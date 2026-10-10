using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UModelComponent/Engine.ModelComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UModelComponent : UPrimitiveComponent
    {
        public UObject Model;
        public int ZoneIndex;
        public UArray<ModelElement>? Elements;
        public ushort ComponentIndex;
        public UArray<ushort>? Nodes;
        
        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.Read(out Model);
            Record(nameof(Model), Model);
            
            _Buffer.Read(out ZoneIndex);
            Record(nameof(ZoneIndex), ZoneIndex);
            
#if MIRRORSEDGE
            if (Package.Build == UnrealPackage.GameBuild.BuildName.MirrorsEdge)
            {
                _Buffer.Read(out Elements);
                Record(nameof(Elements), Elements);

                _Buffer.Read(out ComponentIndex);
                Record(nameof(ComponentIndex), ComponentIndex);

                _Buffer.Read(out Nodes);
                Record(nameof(Nodes), Nodes);
            }
#endif
        }
    }
}
