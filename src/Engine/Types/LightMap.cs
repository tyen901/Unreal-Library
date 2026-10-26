using System;
using System.Runtime.InteropServices;
using UELib.Core;

namespace UELib.Engine
{
    public enum LightMapType : uint
    {
        None = 0,
        LightMap1D = 1,
        LightMap2D = 2
    }

    /// <summary>
    ///     Implements FQuantizedDirectionalLightSample: one color per directional lightmap coefficient.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QuantizedDirectionalLightSample
    {
        public UColor Coefficient0, Coefficient1, Coefficient2;
    }

    /// <summary>
    ///     Implements FQuantizedSimpleLightSample.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QuantizedSimpleLightSample
    {
        public UColor Coefficient;
    }

    /// <summary>
    ///     Implements the FLightMapRef serialization (FLightMap, FLightMap1D and FLightMap2D).
    ///
    ///     The layout is the one stored by Mirror's Edge (536/43): three directional coefficients
    ///     followed by one simple coefficient, each with an FVector scale.
    /// </summary>
    public class LightMapRef : IUnrealSerializableClass
    {
        public const int NumStoredCoefficients = 4;

        public LightMapType Type;
        public UArray<UGuid> LightGuids;

        // FLightMap1D
        public UObject? Owner;
        public UBulkData<QuantizedDirectionalLightSample> DirectionalSamples;
        public UBulkData<QuantizedSimpleLightSample> SimpleSamples;

        // FLightMap2D
        public UObject?[] Textures = new UObject?[NumStoredCoefficients];
        public UVector2D CoordinateScale;
        public UVector2D CoordinateBias;

        public UVector[] ScaleVectors = new UVector[NumStoredCoefficients];

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out uint type);
            Type = (LightMapType)type;
            if (Type == LightMapType.None)
            {
                return;
            }

            stream.ReadArray(out LightGuids);
            switch (Type)
            {
                case LightMapType.LightMap1D:
                    Owner = stream.ReadObject<UObject>();
                    stream.Read(out DirectionalSamples);
                    for (int i = 0; i < NumStoredCoefficients; i++)
                    {
                        stream.ReadStruct(out ScaleVectors[i]);
                    }

                    stream.Read(out SimpleSamples);

                    break;

                case LightMapType.LightMap2D:
                    for (int i = 0; i < NumStoredCoefficients; i++)
                    {
                        Textures[i] = stream.ReadObject<UObject>();
                        stream.ReadStruct(out ScaleVectors[i]);
                    }

                    stream.ReadStruct(out CoordinateScale);
                    stream.ReadStruct(out CoordinateBias);
                    break;

                default:
                    throw new NotSupportedException($"Unknown light-map type {type}");
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Implements FStaticMeshComponentLODInfo.
    /// </summary>
    public class StaticMeshComponentLODInfo : IUnrealSerializableClass
    {
        public UArray<UObject> ShadowMaps;
        public UArray<UObject> ShadowVertexBuffers;
        public LightMapRef LightMap;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out ShadowMaps);
            stream.Read(out ShadowVertexBuffers);
            LightMap = new LightMapRef();
            LightMap.Deserialize(stream);
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Implements FModelElement.
    /// </summary>
    public class ModelElement : IUnrealSerializableClass
    {
        public LightMapRef LightMap;
        public UObject? Component;
        public UObject? Material;
        public UArray<ushort> Nodes;
        public UArray<UObject> ShadowMaps;
        public UArray<UGuid> IrrelevantLights;

        public void Deserialize(IUnrealStream stream)
        {
            LightMap = new LightMapRef();
            LightMap.Deserialize(stream);
            Component = stream.ReadObject<UObject>();
            Material = stream.ReadObject<UObject>();
            stream.Read(out Nodes);
            stream.Read(out ShadowMaps);
            stream.ReadArray(out IrrelevantLights);
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
