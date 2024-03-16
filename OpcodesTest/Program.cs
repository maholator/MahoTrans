using MahoTrans;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.ToolkitImpls.Stub;
using MahoTrans.Utils;

Console.WriteLine("Drop a folder here:");
foreach (var fileName in Directory.EnumerateFiles(Console.ReadLine()!))
{
    if (!fileName.EndsWith("jar", StringComparison.OrdinalIgnoreCase)) continue;

    Console.WriteLine();
    Console.WriteLine(fileName.Split('/')[^1]);
    Console.WriteLine();

    var jarPackage = (new ClassLoader(new StubLogger())).ReadJarFile(File.OpenRead(fileName), false);

    var j = new JvmState(StubToolkit.Create(), ExecutionManner.Unlocked);
    j.AddMahoTransLibrary();
    j.AddJvmClasses(jarPackage, "MLGuest");
    j.LinkAndLock();

    Console.WriteLine($"Virt: {j.CountOpcodes(MTOpcode.invoke_virtual)}");
    Console.WriteLine($"VBS: {j.CountOpcodes(MTOpcode.invoke_virtual_void_no_args_bysig)}");
    Console.WriteLine($"Static: {j.CountOpcodes(MTOpcode.invoke_static)}");
    Console.WriteLine($"SS: {j.CountOpcodes(MTOpcode.invoke_static_simple)}");
    Console.WriteLine($"Inst: {j.CountOpcodes(MTOpcode.invoke_instance)}");
    Console.WriteLine($"Bridge: {j.CountOpcodes(MTOpcode.bridge)}");
    Console.WriteLine($"InitBr: {j.CountOpcodes(MTOpcode.bridge_init)}");
}
