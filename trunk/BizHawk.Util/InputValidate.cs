using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
    /// <summary>
    /// Includes helper functions to validate user input
    /// </summary>
    public static class InputValidate
    {
        public static bool IsValidUnsignedNumber(char[] input)
        {
            ASCIIEncoding AE = new ASCIIEncoding();
            // Check each character in the new label to determine if it is a number.
            for (int x = 0; x < input.Length; x++)
            {
                // Encode the character from the character array to its ASCII code.
                byte[] bc = AE.GetBytes(input[x].ToString());

                // Determine if the ASCII code is within the valid range of numerical values.
                if (bc[0] < 47 || bc[0] > 58)
                    return false;
            }
            return true;
        }
    }
}
