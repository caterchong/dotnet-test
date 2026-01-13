using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Running;
using SharedInterface;
using DllTest;

Console.WriteLine("=== DLL Load/Unload and Performance Test ===\n");

// Test DLL loading and unloading
TestDllLoadUnload();

// Run performance benchmarks
Console.WriteLine("\n=== Running Performance Benchmarks ===\n");
var summary = BenchmarkRunner.Run<DllPerformanceBenchmarks>();

Console.WriteLine("\n=== Test Complete ===");
Console.WriteLine("Detailed results saved in BenchmarkDotNet.Artifacts directory");

static void TestDllLoadUnload()
{
    Console.WriteLine("Testing DLL Load/Unload...");
    
    // Find DLL by searching up the directory tree from the executing assembly
    string? dllPath = null;
    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
    var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
    
    // Search for CalculatorDll.dll by walking up the directory tree
    var searchDir = assemblyDir;
    var maxDepth = 10;
    var depth = 0;
    
    while (searchDir != null && depth < maxDepth)
    {
        // Try Release first
        var releasePath = Path.Combine(searchDir, "CalculatorDll", "bin", "Release", "net9.0", "CalculatorDll.dll");
        if (File.Exists(releasePath))
        {
            dllPath = releasePath;
            break;
        }
        
        // Try Debug
        var debugPath = Path.Combine(searchDir, "CalculatorDll", "bin", "Debug", "net9.0", "CalculatorDll.dll");
        if (File.Exists(debugPath))
        {
            dllPath = debugPath;
            break;
        }
        
        // Go up one level
        var parent = Directory.GetParent(searchDir);
        if (parent == null) break;
        searchDir = parent.FullName;
        depth++;
    }
    
    if (dllPath == null || !File.Exists(dllPath))
    {
        Console.WriteLine($"DLL not found. Searched from: {assemblyDir}");
        Console.WriteLine("Please build the CalculatorDll project first.");
        return;
    }
    
    // Load DLL using AssemblyLoadContext for proper unloading
    var loadContext = new AssemblyLoadContext("TestContext", isCollectible: true);
    
    try
    {
        Console.WriteLine($"Loading DLL from: {dllPath}");
        var assembly = loadContext.LoadFromAssemblyPath(dllPath);
        
        // Find the Calculator type
        var calculatorType = assembly.GetTypes()
            .FirstOrDefault(t => t.GetInterface(nameof(ICalculator)) != null);
        
        if (calculatorType == null)
        {
            Console.WriteLine("Could not find ICalculator implementation in DLL");
            return;
        }
        
        // Create instance and cast to interface
        var calculator = (ICalculator?)Activator.CreateInstance(calculatorType);
        
        if (calculator == null)
        {
            Console.WriteLine("Failed to create calculator instance");
            return;
        }
        
        // Test the interface methods
        Console.WriteLine("\nTesting DLL interface calls:");
        Console.WriteLine($"Add(5, 3) = {calculator.Add(5, 3)}");
        Console.WriteLine($"Multiply(4, 7) = {calculator.Multiply(4, 7)}");
        Console.WriteLine($"ComplexCalculation(100.0) = {calculator.ComplexCalculation(100.0):F2}");
        
        Console.WriteLine("\nDLL loaded and tested successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading DLL: {ex.Message}");
    }
    finally
    {
        // Unload the DLL
        Console.WriteLine("\nUnloading DLL...");
        loadContext.Unload();
        
        // Force garbage collection to ensure unloading
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        Console.WriteLine("DLL unloaded successfully!");
    }
}
