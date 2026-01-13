using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SharedInterface;

namespace DllTest;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class DllPerformanceBenchmarks
{
    private ICalculator? _dllCalculator;
    private LocalCalculator? _localCalculator;
    private AssemblyLoadContext? _loadContext;
    
    // Local implementation for comparison
    private class LocalCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        
        public int Multiply(int a, int b) => a * b;
        
        public double ComplexCalculation(double value)
        {
            double result = value;
            for (int i = 0; i < 1000; i++)
            {
                result = Math.Sqrt(result * 1.1) + Math.Sin(result * 0.01);
            }
            return result;
        }
    }
    
    [GlobalSetup]
    public void Setup()
    {
        // Setup local calculator
        _localCalculator = new LocalCalculator();
        
        // Setup DLL calculator - find path relative to executing assembly location
        // This works even when BenchmarkDotNet changes the working directory
        string? dllPath = null;
        
        // Get the directory where the executing assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
        
        // Search for CalculatorDll.dll by walking up the directory tree
        // Start from the assembly directory and go up until we find the DLL
        var searchDir = assemblyDir;
        var maxDepth = 10; // Prevent infinite loops
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
        
        if (dllPath != null && File.Exists(dllPath))
        {
            _loadContext = new AssemblyLoadContext("BenchmarkContext", isCollectible: true);
            var assembly = _loadContext.LoadFromAssemblyPath(dllPath);
            var calculatorType = assembly.GetTypes()
                .FirstOrDefault(t => t.GetInterface(nameof(ICalculator)) != null);
            
            if (calculatorType != null)
            {
                _dllCalculator = (ICalculator?)Activator.CreateInstance(calculatorType);
            }
        }
        
        if (_dllCalculator == null)
        {
            var message = dllPath != null 
                ? $"Found DLL at {dllPath} but failed to load calculator type."
                : $"Could not find CalculatorDll.dll. Searched from: {assemblyDir}";
            throw new InvalidOperationException(
                $"Failed to load DLL calculator. Please build CalculatorDll project first.\n" +
                $"{message}");
        }
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        if (_loadContext != null)
        {
            _loadContext.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
    
    [Benchmark(Baseline = true)]
    public int LocalAdd() => _localCalculator!.Add(100, 200);
    
    [Benchmark]
    public int DllAdd() => _dllCalculator!.Add(100, 200);
    
    [Benchmark]
    public int LocalMultiply() => _localCalculator!.Multiply(50, 30);
    
    [Benchmark]
    public int DllMultiply() => _dllCalculator!.Multiply(50, 30);
    
    [Benchmark]
    public double LocalComplexCalculation() => _localCalculator!.ComplexCalculation(1000.0);
    
    [Benchmark]
    public double DllComplexCalculation() => _dllCalculator!.ComplexCalculation(1000.0);
}
