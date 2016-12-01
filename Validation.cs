using System;
using System.Diagnostics;
using System.Text;

namespace IBANValidation
{
    public class Validation
    {
        public static bool ValidateBankAccount(string bankAccount)
        {
            bankAccount = bankAccount.ToUpper(); //IN ORDER TO COPE WITH THE REGEX BELOW
            if (string.IsNullOrEmpty(bankAccount))
                return false;
            else if (System.Text.RegularExpressions.Regex.IsMatch(bankAccount, "^[A-Z0-9]"))
            {
                bankAccount = bankAccount.Replace(" ", string.Empty);
                string bank =
                bankAccount.Substring(4, bankAccount.Length - 4) + bankAccount.Substring(0, 4);
                int asciiShift = 55;
                StringBuilder sb = new StringBuilder();
                foreach (char c in bank)
                {
                    int v;
                    if (char.IsLetter(c)) v = c - asciiShift;
                    else v = int.Parse(c.ToString());
                    sb.Append(v);
                }
                string checkSumString = sb.ToString();
                int checksum = int.Parse(checkSumString.Substring(0, 1));
                for (int i = 1; i < checkSumString.Length; i++)
                {
                    int v = int.Parse(checkSumString.Substring(i, 1));
                    checksum *= 10;
                    checksum += v;
                    checksum %= 97;
                }
                return checksum == 1;
            }
            else
                return false;
        }

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
    }
}