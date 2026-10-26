using System;
using System.Runtime.InteropServices;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FBspNode as bulk-serialized by Mirror's Edge (536/43), 64 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BspNode : IUnrealDeserializableClass
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

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out PlaneX); stream.Read(out PlaneY); stream.Read(out PlaneZ); stream.Read(out PlaneW);
            stream.Read(out iVertPool); stream.Read(out iSurf); stream.Read(out iVertexIndex);
            stream.Read(out ComponentIndex); stream.Read(out ComponentNodeIndex); stream.Read(out ComponentElementIndex);
            stream.Read(out iBack); stream.Read(out iFront); stream.Read(out iPlane); stream.Read(out iCollisionBound);
            stream.Read(out iZone0); stream.Read(out iZone1); stream.Read(out NumVertices); stream.Read(out NodeFlags);
            stream.Read(out iLeaf0); stream.Read(out iLeaf1);
        }
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
    public struct ModelVertex : IUnrealDeserializableClass
    {
        public UVector Position;
        public uint TangentX;
        public uint TangentZ;
        public UVector2D TexCoord;
        public UVector2D ShadowTexCoord;

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out Position); stream.Read(out TangentX); stream.Read(out TangentZ);
            stream.ReadStruct(out TexCoord); stream.ReadStruct(out ShadowTexCoord);
        }
    }

    /// <summary>
    ///     Implements FMeshEdge, 16 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshEdge : IUnrealDeserializableClass
    {
        public int Vertex0, Vertex1;
        public int Face0, Face1;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Vertex0); stream.Read(out Vertex1); stream.Read(out Face0); stream.Read(out Face1);
        }
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
            bool consoleVert = typeof(T) == typeof(BspVert) && elementSize == 16;
            if (count < 0 || (count > 0 && elementSize != sizeof(T) && !consoleVert))
                throw new NotSupportedException($"Bulk element size {elementSize} does not match {typeof(T).Name} ({sizeof(T)})");

            var items = new T[count];
            for (int i = 0; i < count; i++)
            {
                long start = stream.Position;
                object value;
                if (typeof(T) == typeof(int)) { stream.Read(out int x); value = x; }
                else if (typeof(T) == typeof(float)) { stream.Read(out float x); value = x; }
                else if (typeof(T) == typeof(ushort)) { stream.Read(out ushort x); value = x; }
                else if (typeof(T) == typeof(UVector)) { stream.ReadStruct(out UVector x); value = x; }
                else if (typeof(T) == typeof(BspNode)) { stream.ReadStruct(out BspNode x); value = x; }
                else if (typeof(T) == typeof(MeshEdge)) { stream.ReadStruct(out MeshEdge x); value = x; }
                else if (typeof(T) == typeof(ModelVertex)) { stream.ReadStruct(out ModelVertex x); value = x; }
                else if (typeof(T) == typeof(BspVert))
                {
                    var x = new BspVert();
                    stream.Read(out x.pVertex); stream.Read(out x.iSide); stream.ReadStruct(out x.ShadowTexCoord);
                    if (!consoleVert) stream.ReadStruct(out x.BackfaceShadowTexCoord);
                    value = x;
                }
                else throw new NotSupportedException("Unsupported bulk element: " + typeof(T).Name);
                if (stream.Position - start != elementSize)
                    throw new InvalidOperationException($"{typeof(T).Name} consumed {stream.Position - start} bytes, expected {elementSize}");
                items[i] = (T)value;
            }
            return items;
        }

    }
}
