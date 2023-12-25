namespace MahoTrans.Tests.Cldc;

public class MultiArrayTests : JarTestBase
{
    [SetUp]
    public void Setup() => DetachHeap();

    [Test]
    public void TestIterating() => Load("TestIterating").Run();

    [Test]
    public void TestFull() => Load("TestFull").Run();

    [Test]
    public void TestPartial() => Load("TestPartial").Run();

    public override string ClassName => "MultiArrayTests";
}