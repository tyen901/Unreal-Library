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
        // Serialized evidence, before UModel clears transient editor flags.
        public byte[]? SerializedNodeFlags;
        public int SurfsOwnerRef;
        public UArray<BspSurf>? Surfs;
        public BspVert[]? Verts;
        public int NumSharedSides;
        public ZoneProperties[]? Zones;
        public int[]? LeafHulls;
        public int[]? Leaves;
        // Offset within the serialized UObject buffer, not the package file.
        public long RootOutsideRelativeOffset = -1;
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
            SerializedNodeFlags = new byte[Nodes.Length];
            // UModel::Serialize clears the transient editor flags after bulk loading.
            for (int i = 0; i < Nodes.Length; i++)
            {
                SerializedNodeFlags[i] = Nodes[i].NodeFlags;
                Nodes[i].NodeFlags &= 0x1f;
            }
            Record(nameof(Nodes), Nodes.Length);

            // TTransArray: the owner precedes the elements.
            SurfsOwnerRef = _Buffer.ReadIndex();
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
            RootOutsideRelativeOffset = _Buffer.Position;
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
