using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// IPortIODevice
	/// </summary>
	/*
        Implementation based on the information contained here:
        http://www.cpcwiki.eu/index.php/765_FDC
        and here:
        http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
    */
	public partial class NECUPD765 : IPortIODevice
	{
		public string outputfile = @"D:\Dropbox\Dropbox\_Programming\TASVideos\BizHawk\output\zxhawkio-" + DateTime.Now.ToString("yyyyMMdd_HHmmss", DateTimeFormatInfo.InvariantInfo) + ".csv";
		public string outputString = "STATUS,WRITE,READ,CODE,MT,MF,SK,CMDCNT,RESCNT,EXECCNT,EXECLEN\r\n";
		public bool writeDebug = false;

		public List<string> dLog = new List<string>
		{
			"STATUS,WRITE,READ,CODE,MT,MF,SK,CMDCNT,RESCNT,EXECCNT,EXECLEN"
		};


		/*
         * Status read
         * Data write
         * Data read
         * CMD code
         * CMD string
         * MT flag
         * MK flag
         * SK flag
         * */
		private readonly string[] workingArr = new string[3];

		private void BuildCSVLine()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 3; i++)
			{
				sb.Append(workingArr[i]);
				sb.Append(',');
				workingArr[i] = "";
			}

			sb.Append(ActiveCommand.CommandCode).Append(',');

			sb.Append(CMD_FLAG_MT).Append(',');
			sb.Append(CMD_FLAG_MF).Append(',');
			sb.Append(CMD_FLAG_SK).Append(',');

			sb.Append(CommCounter).Append(',');
			sb.Append(ResCounter).Append(',');
			sb.Append(ExecCounter).Append(',');
			sb.Append(ExecLength);

			//sb.Append("\r\n");

			//outputString += sb.ToString();
			dLog.Add(sb.ToString());
		}

		public void ReadStatus(ref int data)
		{
			// read main status register
			// this can happen at any time
			data = ReadMainStatus();
			if (writeDebug)
			{
				//outputString += data + ",,," + ActiveCommand.CommandCode + "\r\n";
				workingArr[0] = data.ToString();
				BuildCSVLine();
				//System.IO.File.WriteAllText(outputfile, outputString);
			}
		}

		public void ReadData(ref int data)
		{
			// Z80 is trying to read from the data register
			data = ReadDataRegister();
			if (writeDebug)
			{
				workingArr[2] = data.ToString();
				//outputString += ",," + data + "," + ActiveCommand.CommandCode + "\r\n";
				BuildCSVLine();
			}
		}

		public void WriteData(int data)
		{
			// Z80 is attempting to write to the data register
			WriteDataRegister((byte)data);
			if (writeDebug)
			{
				//outputString += "," + data + ",," + ActiveCommand.CommandCode + "\r\n";
				workingArr[1] = data.ToString();
				BuildCSVLine();
				//System.IO.File.WriteAllText(outputfile, outputString);
			}
		}

		public void Motor(int data)
		{
			// set disk motor on/off
			if (data > 0)
				FDD_FLAG_MOTOR = true;
			else
				FDD_FLAG_MOTOR = false;
		}

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort(ushort port, ref int data)
		{
			BitArray bits = new BitArray(new byte[] { (byte)data });

			if (port == 0x3ffd)
			{
				// Z80 is trying to read from the data register
				data = ReadDataRegister();
				if (writeDebug)
				{
					workingArr[2] = data.ToString();
					//outputString += ",," + data + "," + ActiveCommand.CommandCode + "\r\n";
					BuildCSVLine();
				}

				return true;
			}

			if (port == 0x2ffd)
			{
				// read main status register
				// this can happen at any time
				data = ReadMainStatus();
				if (writeDebug)
				{
					//outputString += data + ",,," + ActiveCommand.CommandCode + "\r\n";
					workingArr[0] = data.ToString();
					BuildCSVLine();
					//System.IO.File.WriteAllText(outputfile, outputString);
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public bool WritePort(ushort port, int data)
		{
			BitArray bits = new BitArray(new byte[] { (byte)data });

			if (port == 0x3ffd)
			{
				// Z80 is attempting to write to the data register
				WriteDataRegister((byte)data);
				if (writeDebug)
				{
					//outputString += "," + data + ",," + ActiveCommand.CommandCode + "\r\n";
					workingArr[1] = data.ToString();
					BuildCSVLine();
					//System.IO.File.WriteAllText(outputfile, outputString);
				}

				return true;
			}

			if (port == 0x1ffd)
			{
				// set disk motor on/off
				FDD_FLAG_MOTOR = bits[3];
				return true;
			}
			return false;
		}
	}
}
