using NUnit.Framework;

namespace Obelune.DataViewer.Editor.Tests
{
    // 埋め込みパッケージの中のテストが Test Runner に見つかることを示すためのテスト（docs/testing.md）
    public class PackageDiscoveryTests
    {
        [Test]
        public void SpikeAssemblyIsReachable() => Assert.AreEqual("Spike1", Obelune.DataViewer.Editor.Spike1.SpikeMarker.Name);
    }
}
