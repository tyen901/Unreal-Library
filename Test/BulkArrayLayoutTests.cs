using System;
using System.Buffers.Binary;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;
using UELib.Engine;

namespace Eliot.UELib.Test;

[TestClass]
public class BulkArrayLayoutTests
{
    [TestMethod]
    [DataRow(false, 43u, 24, 1, true)]
    [DataRow(false, 43u, 24, 0, true)]
    [DataRow(false, 43u, 16, 1, false)]
    [DataRow(false, 43u, 16, 0, false)]
    [DataRow(true, 41u, 16, 1, true)]
    [DataRow(true, 41u, 16, 0, true)]
    [DataRow(false, 41u, 16, 1, false)]
    [DataRow(true, 43u, 16, 1, false)]
    [DataRow(false, 43u, -1, 0, false)]
    public void ValidatesPlatformAndEmptyArrayStride(bool bigEndian, uint licensee, int stride, int count, bool valid)
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(file, BitConverter.GetBytes(bigEndian ? UnrealPackage.Signature_BigEndian : UnrealPackage.Signature));
            using var packageStream = new UPackageStream(file, FileMode.Open, FileAccess.ReadWrite);
            using var package = new UnrealPackage(packageStream);
            package.Summary = new UnrealPackage.PackageFileSummary { Version = 536, LicenseeVersion = checked((ushort)licensee) };
            package.Build = new UnrealPackage.GameBuild(UnrealPackage.GameBuild.BuildName.MirrorsEdge);
            var bytes = new byte[8 + Math.Max(stride, 0) * count];
            void Integer(int offset, int value)
            {
                if (bigEndian) BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(offset), value);
                else BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value);
            }
            Integer(0, stride); Integer(4, count);
            if (count != 0) Integer(8, 7);
            using var stream = new UObjectStream(packageStream, bytes);
            if (!valid)
            {
                Assert.ThrowsExactly<NotSupportedException>(() => BulkArray.Read<BspVert>(stream));
                return;
            }
            var values = BulkArray.Read<BspVert>(stream);
            Assert.AreEqual(count, values.Length);
            Assert.AreEqual(bytes.Length, stream.Position);
            if (count != 0) Assert.AreEqual(7, values[0].pVertex);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
