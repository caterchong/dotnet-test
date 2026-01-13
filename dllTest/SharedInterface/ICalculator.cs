namespace SharedInterface;

/// <summary>
/// Interface for calculator operations that will be implemented in the DLL
/// </summary>
public interface ICalculator
{
    /// <summary>
    /// Adds two numbers
    /// </summary>
    int Add(int a, int b);
    
    /// <summary>
    /// Multiplies two numbers
    /// </summary>
    int Multiply(int a, int b);
    
    /// <summary>
    /// Performs a complex calculation
    /// </summary>
    double ComplexCalculation(double value);
}
