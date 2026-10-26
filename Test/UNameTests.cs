using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UNameTests
{
    [TestMethod]
    public void UnnumberedAndZeroNumberedNamesRemainDistinct()
    {
        var entry = new UNameTableItem { Name = "Moving_Crane_Seq" };
        Assert.AreEqual("Moving_Crane_Seq", new UName(entry).ToString());
        Assert.AreEqual("Moving_Crane_Seq_0", new UName(entry, 0).ToString());
        Assert.AreEqual("Moving_Crane_Seq_1", new UName(entry, 1).ToString());
        Assert.AreEqual(-1, new UName("None").Number);
        Assert.AreEqual(0, new UName(0, 0).Number);
    }
}
