namespace MahoTrans.Tests.Cldc;

public class HashtableTests : JarTestBase
{
    [SetUp]
    public void Setup() => DetachHeap();

    [Test]
    public void TestThrow()
    {
        var test = Load("TestThrow");
        Assert.Throws<JavaRuntimeError>(() => test.Run());
    }

    [Test]
    public void TestSimple() => Load("TestSimple").Run();

    [Test]
    public void TestOldValues() => Load("TestOldValues").Run();

    [Test]
    public void TestHas() => Load("TestHas").Run();

    [Test]
    public void TestRemove() => Load("TestRemove").Run();

    public override string ClassName => "HashtableTests";
}