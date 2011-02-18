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
        public static bool IsValidUnsignedNumber(string Str)
        {
            char[] input = (Str.ToCharArray());
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

        /// <summary>
        /// validates is a Hex number 0-9, A-F (must be capital letters)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsValidHexNumber(string Str)
        {
            char[] input = (Str.ToCharArray());
            ASCIIEncoding AE = new ASCIIEncoding();
            // Check each character in the new label to determine if it is a number.
            for (int x = 0; x < input.Length; x++)
            {
                // Encode the character from the character array to its ASCII code.
                byte[] bc = AE.GetBytes(input[x].ToString());

                // Determine if the ASCII code is within the valid range of numerical values.
                if (bc[0] < 47)
                    return false;
                if (bc[0] > 58)
                {
                    if (bc[0] < 65 || bc[0] > 70) //A-F capital letters only!
                        return false;
                }
            }
            return true;
        }
    }
}
