using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UModelProvenanceTests
{
    private static void Equal<T>(T expected, T actual) => Assert.AreEqual(expected, actual);

    [TestMethod]
    [DataRow(-7)]
    [DataRow(0)]
    [DataRow(13)]
    public void PreservesSerializedEvidenceBeforeRuntimeNormalization(int owner)
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(file, BitConverter.GetBytes(UnrealPackage.Signature));
            using var packageStream = new UPackageStream(file, FileMode.Open, FileAccess.ReadWrite);
            using var package = new UnrealPackage(packageStream);
            package.Summary = new UnrealPackage.PackageFileSummary { Version = 536, LicenseeVersion = 43 };
            package.Build = new UnrealPackage.GameBuild(UnrealPackage.GameBuild.BuildName.MirrorsEdge);
            using var data = new MemoryStream();
            using var writer = new BinaryWriter(data);
            writer.Write(0); // UObject NetIndex, class-zero synthetic export has no tags.
            writer.Write(new byte[28]);
            void Bulk(int stride, byte[] bytes) { writer.Write(stride); writer.Write(bytes.Length / stride); writer.Write(bytes); }
            Bulk(12, []); Bulk(12, []);
            var node = new byte[64]; node[55] = 0xe7; Bulk(64, node);
            writer.Write(owner); writer.Write(0); // Exact signed TTransArray owner, zero surfaces.
            var vert = new byte[24];
            uint[] witness = [0x7fc01234, 0x80000000, 0xff801234, 0x3f800000];
            for (int i = 0; i < witness.Length; i++) BitConverter.GetBytes(witness[i]).CopyTo(vert, 8 + i * 4);
            long vertexHeader = data.Position;
            Bulk(24, vert);
            writer.Write(0); writer.Write(0); writer.Write(0); // Sides, zones, polys.
            long leafHullHeader = data.Position;
            Bulk(4, []); Bulk(4, []);
            long rootOffset = data.Position;
            writer.Write(1); writer.Write(0); Bulk(4, []); Bulk(16, []); writer.Write(0); Bulk(36, []);
            using var model = new UModel();
            typeof(UObject).GetProperty(nameof(UObject.Package))!.SetValue(model, package);
            typeof(UObject).GetProperty(nameof(UObject.Table))!.SetValue(model, new UExportTableItem());
            using var buffer = new UObjectStream(packageStream, data.ToArray(), 123456);
            model.Deserialize(buffer);
            Equal(owner, model.SurfsOwnerRef); Equal((byte)0xe7, model.SerializedNodeFlags![0]);
            Equal((byte)7, model.Nodes![0].NodeFlags); Equal(rootOffset, model.RootOutsideRelativeOffset);
            Equal(1, model.RootOutside); Equal(0, model.Linked);
            Equal(data.Length, buffer.Position); Equal(data.Length + 123456, buffer.AbsolutePosition);
            var v = model.Verts![0];
            uint[] decoded = [BitConverter.SingleToUInt32Bits(v.ShadowTexCoord.X), BitConverter.SingleToUInt32Bits(v.ShadowTexCoord.Y),
                BitConverter.SingleToUInt32Bits(v.BackfaceShadowTexCoord.X), BitConverter.SingleToUInt32Bits(v.BackfaceShadowTexCoord.Y)];
            Assert.IsTrue(witness.SequenceEqual(decoded), "IEEE754 witness bits changed");
            foreach (var (offset, stride) in new[] { (vertexHeader, 16), (leafHullHeader, -1) })
            {
                var malformed = data.ToArray();
                BitConverter.GetBytes(stride).CopyTo(malformed, offset);
                using var invalid = new UObjectStream(packageStream, malformed, 123456);
                Assert.ThrowsExactly<NotSupportedException>(() => model.Deserialize(invalid));
            }
        }
        finally
        {
            File.Delete(file);
        }
    }
}
