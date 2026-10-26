using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UModel/Engine.Model
    /// </summary>
    [Output("Brush")]
    [UnrealRegisterClass]
    public class UModel : UPrimitive
    {
        [Output] public UPolys? Polys;

        public UVector BoundsOrigin;
        public UVector BoundsBoxExtent;
        public float BoundsSphereRadius;

        public UVector[]? Vectors;
        public UVector[]? Points;
        public BspNode[]? Nodes;
        public UArray<BspSurf>? Surfs;
        public BspVert[]? Verts;
        public int NumSharedSides;
        public ZoneProperties[]? Zones;
        public int[]? LeafHulls;
        public int[]? Leaves;
        public int RootOutside;
        public int Linked;
        public int[]? PortalNodes;
        public MeshEdge[]? ShadowVolume;
        public int NumVertices;
        public ModelVertex[]? VertexBuffer;

        private bool IsMirrorsEdge =>
#if MIRRORSEDGE
            Package.Build == UnrealPackage.GameBuild.BuildName.MirrorsEdge;
#else
            false;
#endif

        protected override bool HasPrimitiveBounds => !IsMirrorsEdge;

        protected override void Deserialize()
        {
            base.Deserialize();
            if (!IsMirrorsEdge)
            {
                return;
            }

            _Buffer.ReadStruct(out BoundsOrigin);
            _Buffer.ReadStruct(out BoundsBoxExtent);
            _Buffer.Read(out BoundsSphereRadius);
            Record(nameof(BoundsSphereRadius), BoundsSphereRadius);

            Vectors = BulkArray.Read<UVector>(_Buffer);
            Record(nameof(Vectors), Vectors.Length);
            Points = BulkArray.Read<UVector>(_Buffer);
            Record(nameof(Points), Points.Length);
            Nodes = BulkArray.Read<BspNode>(_Buffer);
            Record(nameof(Nodes), Nodes.Length);

            // TTransArray: the owner precedes the elements.
            _Buffer.ReadObject<UObject>();
            _Buffer.ReadArray(out UArray<BspSurf> surfs);
            Surfs = surfs;
            Record(nameof(Surfs), Surfs.Count);

            Verts = BulkArray.Read<BspVert>(_Buffer);
            Record(nameof(Verts), Verts.Length);

            _Buffer.Read(out NumSharedSides);
            _Buffer.Read(out int numZones);
            Zones = new ZoneProperties[numZones];
            for (int i = 0; i < numZones; i++)
            {
                Zones[i].Deserialize(_Buffer);
            }

            Polys = _Buffer.ReadObject<UPolys>();
            Record(nameof(Polys), Polys);

            LeafHulls = BulkArray.Read<int>(_Buffer);
            Leaves = BulkArray.Read<int>(_Buffer);
            _Buffer.Read(out RootOutside);
            _Buffer.Read(out Linked);
            PortalNodes = BulkArray.Read<int>(_Buffer);
            ShadowVolume = BulkArray.Read<MeshEdge>(_Buffer);

            _Buffer.Read(out NumVertices);
            VertexBuffer = BulkArray.Read<ModelVertex>(_Buffer);
            Record(nameof(VertexBuffer), VertexBuffer.Length);
        }
    }
}
