using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;

namespace ExceptionBenchmark
{
    // Exception-based approach: using custom exception to control flow
    public class ExceptionBasedApproach
    {
        public class ResultException : Exception
        {
            public int Result { get; set; }
            public ResultException(int result) => Result = result;
        }

        public int Level5_Exception(int value)
        {
            if (value < 0)
                throw new ResultException(value);
            return value + 1;
        }

        public int Level4_Exception(int value)
        {
            var result = Level5_Exception(value);
            return result + 1;
        }

        public int Level3_Exception(int value)
        {
            var result = Level4_Exception(value);
            return result + 1;
        }

        public int Level2_Exception(int value)
        {
            var result = Level3_Exception(value);
            return result + 1;
        }

        public int Level1_Exception(int value)
        {
            try
            {
                var result = Level2_Exception(value);
                return result + 1;
            }
            catch (ResultException ex)
            {
                Console.WriteLine("=== Exception Caught ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Result Value: {ex.Result}");
                Console.WriteLine("=== Stack Trace ===");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("=====================");
                return ex.Result;
            }
        }

        public int Execute_Exception(int value)
        {
            return Level1_Exception(value);
        }
    }

    // Return-based approach: using normal return values through all levels
    public class ReturnBasedApproach
    {
        public class Result
        {
            public int Value { get; set; }
            public bool IsError { get; set; }
        }

        public Result Level5_Return(int value)
        {
            if (value < 0)
                return new Result { Value = value, IsError = true };
            return new Result { Value = value + 1, IsError = false };
        }

        public Result Level4_Return(Result input)
        {
            if (input.IsError)
                return input;
            var result = Level5_Return(input.Value);
            if (result.IsError)
                return result;
            return new Result { Value = result.Value + 1, IsError = false };
        }

        public Result Level3_Return(Result input)
        {
            if (input.IsError)
                return input;
            var result = Level4_Return(input);
            if (result.IsError)
                return result;
            return new Result { Value = result.Value + 1, IsError = false };
        }

        public Result Level2_Return(Result input)
        {
            if (input.IsError)
                return input;
            var result = Level3_Return(input);
            if (result.IsError)
                return result;
            return new Result { Value = result.Value + 1, IsError = false };
        }

        public Result Level1_Return(Result input)
        {
            if (input.IsError)
                return input;
            var result = Level2_Return(input);
            if (result.IsError)
                return result;
            return new Result { Value = result.Value + 1, IsError = false };
        }

        public int Execute_Return(int value)
        {
            var input = new Result { Value = value, IsError = false };
            var result = Level1_Return(input);
            return result.Value;
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class PerformanceBenchmark
    {
        private ExceptionBasedApproach _exceptionApproach = new();
        private ReturnBasedApproach _returnApproach = new();

        // Test normal case (no error)
        [Benchmark]
        public int NormalCase_Exception()
        {
            return _exceptionApproach.Execute_Exception(100);
        }

        [Benchmark]
        public int NormalCase_Return()
        {
            return _returnApproach.Execute_Return(100);
        }

        // Test error case (exception thrown)
        [Benchmark]
        public int ErrorCase_Exception()
        {
            return _exceptionApproach.Execute_Exception(-1);
        }

        [Benchmark]
        public int ErrorCase_Return()
        {
            return _returnApproach.Execute_Return(-1);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing Exception Stack Trace ===\n");
            
            // 先运行一次异常情况来看堆栈跟踪
            var exceptionApproach = new ExceptionBasedApproach();
            Console.WriteLine("Calling Execute_Exception(-1) to trigger exception:\n");
            var result = exceptionApproach.Execute_Exception(-1);
            Console.WriteLine($"\nResult: {result}\n");
            
            Console.WriteLine("\n=== Now Running Full Benchmark ===\n");
            var summary = BenchmarkRunner.Run<PerformanceBenchmark>();
        }
    }
}
