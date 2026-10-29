using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UMultiFont/Engine.MultiFont
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UMultiFont : UFont
    {
        public UArray<float> ResolutionTestTable;
        public UArray<FontKerningPair> KerningPairs;
        public UArray<int> ResolutionKerningOffsets;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (Package.Build == UnrealPackage.GameBuild.BuildName.MirrorsEdge)
            {
                // Mirror's Edge adds per-pair kerning and per-resolution array
                // slices after UFont's native character remap.
                _Buffer.ReadArray(out KerningPairs);
                Record(nameof(KerningPairs), KerningPairs);
                _Buffer.ReadArray(out ResolutionKerningOffsets);
                Record(nameof(ResolutionKerningOffsets), ResolutionKerningOffsets);
            }

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                _Buffer.ReadArray(out ResolutionTestTable);
                Record(nameof(ResolutionTestTable), ResolutionTestTable);
            }
        }

        public struct FontKerningPair : IUnrealSerializableClass
        {
            public uint Key;
            public float Adjustment;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Key);
                stream.Read(out Adjustment);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(Key);
                stream.Write(Adjustment);
            }
        }
    }
}
