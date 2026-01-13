using SharedInterface;

namespace CalculatorDll;

/// <summary>
/// Implementation of ICalculator interface in the DLL
/// </summary>
public class Calculator : ICalculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }

    public double ComplexCalculation(double value)
    {
        // Simulate a more complex calculation
        double result = value;
        for (int i = 0; i < 1000; i++)
        {
            result = Math.Sqrt(result * 1.1) + Math.Sin(result * 0.01);
        }
        return result;
    }
}
