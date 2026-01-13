# DLL Load/Unload and Performance Test

This project demonstrates loading and unloading DLLs in .NET 9.0, and benchmarks the performance difference between local function calls and DLL function calls.

## Project Structure

- **SharedInterface**: Contains the `ICalculator` interface that both local and DLL implementations use
- **CalculatorDll**: A DLL project that implements `ICalculator`
- **DllTest**: Main project that loads/unloads the DLL and runs performance benchmarks

## Features

1. **DLL Load/Unload Testing**: Uses `AssemblyLoadContext` with `isCollectible: true` to properly load and unload DLLs
2. **Interface-based DLL Calls**: Demonstrates calling methods through an interface from a loaded DLL
3. **Performance Benchmarking**: Uses BenchmarkDotNet to compare performance between:
   - Local function calls
   - DLL function calls

## Building the Projects

```bash
# Build all projects from the dllTest directory (uses solution file)
dotnet build

# Or build in Release mode for better performance benchmarks
dotnet build -c Release

# Or build individually
dotnet build SharedInterface/SharedInterface.csproj
dotnet build CalculatorDll/CalculatorDll.csproj
dotnet build DllTest/DllTest.csproj
```

## Running the Tests

```bash
cd DllTest
dotnet run -c Release
```

The program will:
1. Load the DLL, call interface methods, and then unload it
2. Run performance benchmarks comparing local vs DLL calls

## Benchmark Results

The benchmarks compare:
- `Add`: Simple addition operation
- `Multiply`: Simple multiplication operation  
- `ComplexCalculation`: A more complex calculation with loops and math operations

Results will be saved in `BenchmarkDotNet.Artifacts` directory.

## Notes

- The DLL must be built before running the tests
- For accurate performance measurements, run in Release mode
- The `AssemblyLoadContext` with `isCollectible: true` allows proper DLL unloading, which requires .NET Core 3.0+ or .NET 5+
