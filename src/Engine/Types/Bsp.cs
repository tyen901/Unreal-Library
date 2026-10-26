using System;
using System.Runtime.InteropServices;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FBspNode as bulk-serialized by Mirror's Edge (536/43), 64 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BspNode
    {
        public float PlaneX, PlaneY, PlaneZ, PlaneW;
        public int iVertPool;
        public int iSurf;
        public int iVertexIndex;
        public ushort ComponentIndex;
        public ushort ComponentNodeIndex;
        public int ComponentElementIndex;
        public int iBack;
        public int iFront;
        public int iPlane;
        public int iCollisionBound;
        public byte iZone0, iZone1;
        public byte NumVertices;
        public byte NodeFlags;
        public int iLeaf0, iLeaf1;
    }

    /// <summary>
    ///     Implements FVert, 24 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BspVert
    {
        public int pVertex;
        public int iSide;
        public UVector2D ShadowTexCoord;
        public UVector2D BackfaceShadowTexCoord;
    }

    /// <summary>
    ///     Implements FModelVertex, 36 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ModelVertex
    {
        public UVector Position;
        public uint TangentX;
        public uint TangentZ;
        public UVector2D TexCoord;
        public UVector2D ShadowTexCoord;
    }

    /// <summary>
    ///     Implements FMeshEdge, 16 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshEdge
    {
        public int Vertex0, Vertex1;
        public int Face0, Face1;
    }

    /// <summary>
    ///     Implements FZoneProperties.
    /// </summary>
    public struct ZoneProperties : IUnrealSerializableClass
    {
        public UObject? ZoneActor;
        public ulong Connectivity;
        public ulong Visibility;
        public float LastRenderTime;

        public void Deserialize(IUnrealStream stream)
        {
            ZoneActor = stream.ReadObject<UObject>();
            stream.Read(out Connectivity);
            stream.Read(out Visibility);
            stream.Read(out LastRenderTime);
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Implements FBspSurf, 56 bytes.
    /// </summary>
    public class BspSurf : IUnrealSerializableClass
    {
        public UObject? Material;
        public uint PolyFlags;
        public int pBase;
        public int vNormal;
        public int vTextureU;
        public int vTextureV;
        public int iBrushPoly;
        public UObject? Actor;
        public UPlane Plane;
        public float ShadowMapScale;
        public uint LightingChannels;

        public void Deserialize(IUnrealStream stream)
        {
            Material = stream.ReadObject<UObject>();
            stream.Read(out PolyFlags);
            stream.Read(out pBase);
            stream.Read(out vNormal);
            stream.Read(out vTextureU);
            stream.Read(out vTextureV);
            stream.Read(out iBrushPoly);
            Actor = stream.ReadObject<UObject>();
            stream.ReadStruct(out Plane);
            stream.Read(out ShadowMapScale);
            stream.Read(out LightingChannels);
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    public static class BulkArray
    {
        /// <summary>
        ///     Reads a TArray written with BulkSerialize: the element size, the count, then the raw elements.
        /// </summary>
        public static unsafe T[] Read<T>(IUnrealStream stream) where T : unmanaged
        {
            stream.Read(out int elementSize);
            int count = stream.ReadLength();
            if (elementSize != sizeof(T) && count > 0)
            {
                throw new NotSupportedException(
                    $"Bulk element size {elementSize} does not match {typeof(T).Name} ({sizeof(T)})");
            }

            var bytes = new byte[count * sizeof(T)];
            stream.UR.Read(bytes, 0, bytes.Length);
            var items = new T[count];
            MemoryMarshal.Cast<byte, T>(bytes).CopyTo(items);
            return items;
        }
    }
}
