using BenchmarkDotNet.Attributes;

namespace IBANValidation
{
    public class Benchmark
    {
        private const string ExampleIBAN = "GB29NWBK60161331926819";

        [Benchmark]
        public bool Original()
        {
            return Validation.ValidateBankAccount(ExampleIBAN);
        }

        [Benchmark]
        public bool New()
        {
            return Validation.ValidateBankAccount2(ExampleIBAN);
        }
    }
}