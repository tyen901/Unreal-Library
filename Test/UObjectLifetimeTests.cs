using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UObjectLifetimeTests
{
    private sealed class CountingObject : UObject
    {
        internal static int DisposeCalls;

        protected override void Dispose(bool disposing)
        {
            Interlocked.Increment(ref DisposeCalls);
            base.Dispose(disposing);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedObject()
    {
        using var value = new CountingObject();
        return new WeakReference(value);
    }

    [TestMethod]
    public void ExplicitDisposalDoesNotRepeatInFinalizer()
    {
        CountingObject.DisposeCalls = 0;
        var reference = CreateDisposedObject();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.IsFalse(reference.IsAlive);
        Assert.AreEqual(1, CountingObject.DisposeCalls);
    }
}
