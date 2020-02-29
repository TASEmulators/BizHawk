using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.Common.cheats
{
	public class GbaCodeBreakerDecoder
	{
	}

	/*
	public void GBACodeBreaker()
		{
			// These checks are done on the Decrypted code, not the Encrypted one.
			if (_ramAddress.StartsWith("0000") && _ramValue.StartsWith("0008") || _ramAddress.StartsWith("0000") && _ramValue.StartsWith("0002"))
			{
				// Master Code #1
				// 0000xxxx yyyy

				// xxxx is the CRC value (the "Game ID" converted to hex)
				// Flags("yyyy"):
				// 0008 - CRC Exists(CRC is used to autodetect the inserted game)
				// 0002 - Disable Interrupts
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
				return;
			}

			if (_ramAddress.StartsWith("1") && _ramValue.StartsWith("1000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("2000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("3000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("4000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("0020"))
			{
				// Master Code #2
				// 1aaaaaaa xxxy
				// 'y' is the CBA Code Handler Store Address(0 - 7)[address = ((d << 0x16) + 0x08000100)]

				// 1000 - 32 - bit Long - Branch Type(Thumb)
				// 2000 - 32 - bit Long - Branch Type(ARM)
				// 3000 - 8 - bit(?) Long - Branch Type(Thumb)
				// 4000 - 8 - bit(?) Long - Branch Type(ARM)
				// 0020 - Unknown(Odd Effect)
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
				return;
			}
			
			if (_ramAddress.StartsWith("3"))
			{
				// 8 - Bit Constant RAM Write
				// 3aaaaaaa 00yy
				// Continuosly writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 16;
			}
			else if (_ramAddress.StartsWith("4"))
			{
				// Slide Code
				// 4aaaaaaa yyyy
				// xxxxxxxx iiii
				// This is one of those two - line codes.The "yyyy" set is the data to store at the address (aaaaaaa), with xxxxxxxx being the number of addresses to store to, and iiii being the value to increment the addresses by.  The codetype is usually use to fill memory with a certain value.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 32;
				MessageBox.Show("Sorry, this tool does not support 4 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("6"))
			{
				// 16 - Bit Logical AND
				// 6aaaaaaa yyyy
				// Performs the AND function on the address provided with the value provided. I'm not going to explain what AND does, so if you'd like to know I suggest you see the instruction manual for a graphing calculator.
				// This is another advanced code type you'll probably never need to use.  

				// Ocean Prince's note:
				// AND means "If ALL conditions are True then Do"
				// I don't understand how this would be applied/works.  Samples are requested.
				MessageBox.Show("Sorry, this tool does not support 6 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("7"))
			{
				// 16 - Bit 'If Equal To' Activator
				// 7aaaaaaa yyyy
				// If the value at the specified RAM address(aaaaaaa) is equal to yyyy value, active the code on the next line.
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("8"))
			{
				// 16 - Bit Constant RAM Write
				// 8aaaaaaa yyyy
				// Continuously writes yyyy values to the specified RAM address(aaaaaaa).
				// Continuously writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 32;
			}
			else if (_ramAddress.StartsWith("9"))
			{
				// Change Encryption Seeds
				// 9yyyyyyy yyyy
				// (When 1st Code Only!)
				// Works like the DEADFACE on GSA.Changes the encryption seeds used for the rest of the codes.
				MessageBox.Show("Sorry, this tool does not support 9 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_byteSize = 32;
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A"))
			{
				// 16 - Bit 'If Not Equal' Activator
				// Axxxxxxx yyyy
				// Basically the opposite of an 'If Equal To' Activator.Activates the code on the next line if address xxxxxxx is NOT equal to yyyy
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
				_byteSize = 32;

			}
			else if (_ramAddress.StartsWith("D00000"))
			{
				// 16 - Bit Conditional RAM Write
				// D00000xx yyyy
				// No Description available at this time.
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
				_byteSize = 32;
			}
		}
	 */
}
