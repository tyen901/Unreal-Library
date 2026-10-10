using System;
using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FStaticMeshElement.
    /// </summary>
    public class StaticMeshSection : IUnrealSerializableClass
    {
        public UObject? Material;
        public int EnableCollision;
        public int OldEnableCollision;
        public int bEnableShadowCasting;
        public int FirstIndex;
        public int NumTriangles;
        public int MinVertexIndex;
        public int MaxVertexIndex;
        public int MaterialIndex;
        public (int, int)[] Fragments = [];

        public void Deserialize(IUnrealStream stream)
        {
            Material = stream.ReadObject<UObject>();
            stream.Read(out EnableCollision);
            stream.Read(out OldEnableCollision);
            stream.Read(out bEnableShadowCasting);
            stream.Read(out FirstIndex);
            stream.Read(out NumTriangles);
            stream.Read(out MinVertexIndex);
            stream.Read(out MaxVertexIndex);
            stream.Read(out MaterialIndex);
            int count = stream.ReadLength();
            Fragments = new (int, int)[count];
            for (int i = 0; i < count; i++)
            {
                stream.Read(out int first);
                stream.Read(out int second);
                Fragments[i] = (first, second);
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Implements FStaticMeshRenderData as stored by Mirror's Edge (536/43).
    /// </summary>
    public class StaticMeshLOD : IUnrealSerializableClass
    {
        public UBulkData<byte> RawTriangles;
        public UArray<StaticMeshSection> Sections;

        public UVector[] Positions = [];

        public int NumTexCoords;
        public bool bUseFullPrecisionUVs;

        /// <summary>FPackedNormal TangentX per vertex.</summary>
        public uint[] TangentX = [];

        /// <summary>FPackedNormal TangentZ per vertex; W carries the binormal sign.</summary>
        public uint[] TangentZ = [];

        public UColor[] Colors = [];

        /// <summary>NumTexCoords UV pairs per vertex.</summary>
        public UVector2D[] UVs = [];

        public float[] ShadowVolumeData = [];
        public int NumVertices;
        public ushort[] Indices = [];
        public ushort[] WireframeIndices = [];
        public MeshEdge[] Edges = [];
        public UArray<byte> ShadowTriangleDoubleSided;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out RawTriangles);
            stream.ReadArray(out Sections);

            stream.Read(out int _); // stride
            stream.Read(out int positionCount);
            Positions = BulkArray.Read<UVector>(stream);
            if (Positions.Length != positionCount)
            {
                throw new NotSupportedException($"Position stream holds {Positions.Length} of {positionCount} vertices");
            }

            stream.Read(out NumTexCoords);
            stream.Read(out int itemSize);
            stream.Read(out int itemCount);
            stream.Read(out int fullPrecision);
            bUseFullPrecisionUVs = fullPrecision != 0;
            DeserializeVertexItems(stream, itemSize, itemCount);

            stream.Read(out int _); // shadow volume stride
            stream.Read(out int _); // shadow volume vertex count
            ShadowVolumeData = BulkArray.Read<float>(stream);

            stream.Read(out NumVertices);
            Indices = BulkArray.Read<ushort>(stream);
            WireframeIndices = BulkArray.Read<ushort>(stream);
            Edges = BulkArray.Read<MeshEdge>(stream);
            stream.ReadArray(out ShadowTriangleDoubleSided);
        }

        private void DeserializeVertexItems(IUnrealStream stream, int itemSize, int itemCount)
        {
            stream.Read(out int bulkElementSize);
            int count = stream.ReadLength();
            int uvSize = bUseFullPrecisionUVs ? 8 : 4;
            int expected = 12 + NumTexCoords * uvSize;
            if (count != itemCount || bulkElementSize != itemSize || itemSize != expected)
            {
                throw new NotSupportedException(
                    $"Vertex item layout {bulkElementSize}/{itemSize} x {count}/{itemCount} does not match {expected}");
            }

            TangentX = new uint[count];
            TangentZ = new uint[count];
            Colors = new UColor[count];
            UVs = new UVector2D[count * NumTexCoords];
            for (int i = 0; i < count; i++)
            {
                stream.Read(out TangentX[i]);
                stream.Read(out TangentZ[i]);
                stream.ReadStruct(out Colors[i]);
                for (int uv = 0; uv < NumTexCoords; uv++)
                {
                    if (bUseFullPrecisionUVs)
                    {
                        stream.ReadStruct(out UVs[i * NumTexCoords + uv]);
                    }
                    else
                    {
                        stream.Read(out ushort u);
                        stream.Read(out ushort v);
                        UVs[i * NumTexCoords + uv] = new UVector2D(HalfToFloat(u), HalfToFloat(v));
                    }
                }
            }
        }

        private static float HalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 1;
            int exponent = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;
            float value;
            if (exponent == 0)
            {
                value = mantissa * (1.0f / (1 << 24));
            }
            else if (exponent == 31)
            {
                value = mantissa == 0 ? float.PositiveInfinity : float.NaN;
            }
            else
            {
                value = (1.0f + mantissa / 1024.0f) * (float)Math.Pow(2, exponent - 15);
            }

            return sign != 0 ? -value : value;
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Implements UStaticMesh/Engine.StaticMesh
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UStaticMesh : UObject
    {
        public UVector BoundsOrigin;
        public UVector BoundsBoxExtent;
        public float BoundsSphereRadius;
        public UObject? BodySetup;
        public int InternalVersion;
        public UArray<StaticMeshLOD>? LODs;
        public int TrailingLODCount;
        public URotator ThumbnailAngle;
        public float ThumbnailDistance;

        public UStaticMesh()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
#if MIRRORSEDGE
            if (Package.Build != UnrealPackage.GameBuild.BuildName.MirrorsEdge)
            {
                return;
            }

            _Buffer.ReadStruct(out BoundsOrigin);
            _Buffer.ReadStruct(out BoundsBoxExtent);
            _Buffer.Read(out BoundsSphereRadius);
            BodySetup = _Buffer.ReadObject<UObject>();
            Record(nameof(BodySetup), BodySetup);

            SkipBulkArray(_Buffer); // kDOP nodes
            SkipBulkArray(_Buffer); // kDOP triangles

            _Buffer.Read(out InternalVersion);
            Record(nameof(InternalVersion), InternalVersion);
            if (InternalVersion >= 17)
            {
                _Buffer.ReadArray(out UArray<UName> _);
            }

            _Buffer.ReadArray(out UArray<StaticMeshLOD> lods);
            LODs = lods;
            Record(nameof(LODs), LODs.Count);

            // Equal to the LOD count in every retail mesh.
            _Buffer.Read(out TrailingLODCount);
            _Buffer.ReadStruct(out ThumbnailAngle);
            _Buffer.Read(out ThumbnailDistance);
#endif
        }

        private static void SkipBulkArray(IUnrealStream stream)
        {
            stream.Read(out int elementSize);
            int count = stream.ReadLength();
            stream.Position += (long)elementSize * count;
        }
    }
}
