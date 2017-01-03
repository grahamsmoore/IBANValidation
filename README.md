For a project I was working on, I needed to validate International Bank Account Numbers (IBAN) in C#, naturally rather than reinventing the wheel I took a look on Google to see what was already available.  There weren't many relevant results available, but the top result pointed to [CodeProject](http://www.codeproject.com/Tips/775696/IBAN-Validator).

The validation routine was a fairly short and simple method, great I thought, but then took a close look and alarm bells started ringing about it's potential performance issues.  Here is the code from the article:
```csharp
public static bool ValidateBankAccount(string bankAccount)  
{
    const int asciiShift = 55;

    bankAccount = bankAccount.ToUpper();
    if (string.IsNullOrEmpty(bankAccount))
        return false;

    if (!Regex.IsMatch(bankAccount, "^[A-Z0-9]"))
        return false;

    bankAccount = bankAccount.Replace(" ", string.Empty);
    var bank = bankAccount.Substring(4, bankAccount.Length - 4) + bankAccount.Substring(0, 4);
    var sb = new StringBuilder();
    foreach (var c in bank)
    {
        int v;
        if (char.IsLetter(c)) v = c - asciiShift;
        else v = int.Parse(c.ToString());
        sb.Append(v);
    }

    var checkSumString = sb.ToString();
    var checksum = int.Parse(checkSumString.Substring(0, 1));
    for (var i = 1; i < checkSumString.Length; i++)
    {
        var v = int.Parse(checkSumString.Substring(i, 1));
        checksum *= 10;
        checksum += v;
        checksum %= 97;
    }

    return checksum == 1;
}
```

As you can see there are some immediately apparent issues:
1. There is rather a lot of string manipulation taking place.
2. An expensive `StringBuilder` instantiation and subsequent operations.
3. Some [RegEx](https://en.wikipedia.org/wiki/Regular_expression) matching.

With these observations in mind, I set about attempting to write a more performant method of IBAN validation.

### Code
I will test the existing [CodeProject](http://www.codeproject.com/) method against my own on a known IBAN number.  The testing will be performed using the [BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet) library, also available on [NuGet](https://www.nuget.org/).

Here is the code I came up with:
```csharp
public static unsafe bool ValidateBankAccount2(string bankAccount)
{
    const int asciiShift = 55;
    var length = bankAccount.Length;

    fixed (char* t = bankAccount)
    {
        var checksum = int.MinValue;
        var startIndex = 4;
        for (var i = 0; i < length; i++)
        {
            var currentItem = t[startIndex];
            int currentItemNumber;
            if (char.IsLetter(currentItem))
            {
                currentItemNumber = currentItem - asciiShift;
            }
            else if (char.IsNumber(currentItem))
            {
                currentItemNumber = (int)char.GetNumericValue(currentItem);
            }
            else
            {
                return false;
            }

            if (i == 0)
            {
                checksum = currentItemNumber;
            }
            else
            {
                checksum *= 10;
                checksum += currentItemNumber;
                checksum %= 97;
            }

            if (++startIndex == length)
            {
                startIndex = 0;
            }
        }

        return checksum == 1;
    }
}
```

As you can see the code has been improved in a number of ways:
1. Removal of all RegEx matches.
2. Remove all `string` allocations.
3. Iterating through the string in a `for` loop rather than `foreach`.  This operation is done via pointer access to the array, hence the need for the `unsafe` keyword, but this can be omitted if unsafe code is not permitted.

The Visual Studio project for this benchmark can be found in my [GitHub](https://github.com/grahamsmoore/IBANValidation) repository.

### Results
The following results are produced by [BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet):
```ini
Host Process Environment Information:
BenchmarkDotNet.Core=v0.9.9.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700K CPU 4.00GHz, ProcessorCount=8
Frequency=3914062 ticks, Resolution=255.4891 ns, Timer=TSC
CLR=MS.NET 4.0.30319.42000, Arch=32-bit RELEASE
GC=Concurrent Workstation
JitModules=clrjit-v4.6.1586.0

Type=Benchmark  Mode=Throughput
```
<table border="1" style="width:auto">
  <tr>
    <th>Method</th>
    <th>Median</th> 
    <th>StdDev</th>
    <th>Gen 0</th>
    <th>Gen 1</th>
    <th>Gen 2</th>
    <th>Bytes Allocated/Op</th>
  </tr>
  <tr>
    <td>Original</td>
    <td>5,507.9331 ns</td> 
    <td>52.4881 ns</td>
    <td>491.00</td>
    <td>-</td>
    <td>-</td>
    <td>696.29</td>
  </tr>  
<tr>
    <td>New</td>
    <td>205.3180 ns</td> 
    <td>2.0443 ns</td>
    <td>-</td>
    <td>-</td>
    <td>-</td>
    <td>0.02</td>
  </tr>
</table>
### Conclusion
The results clearly show that there has been a significant reduction in both execution time and memory allocated, in this case the two go hand-in-hand to produce a much more satisfactory result.  As with a lot micro-optimisations, the original execution time is so small that time is only wisely spent optimising if the method is in a hot-code execution path (is was in my case).