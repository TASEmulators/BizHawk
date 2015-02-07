using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Cores.Calculators
{
	public class TI83LinkPort
	{
		// Emulates TI linking software.
		// See http://www.ticalc.org/archives/files/fileinfo/294/29418.html for documentation

		// Note: Each hardware read/write to the link port calls tthe update method.
		readonly TI83 Parent;

		private FileStream CurrentFile;
		//private int FileBytesLeft;
		private byte[] VariableData;

		private Action NextStep;
		private Queue<byte> CurrentData = new Queue<byte>();
		private ushort BytesToSend;
		private byte BitsLeft;
		private byte CurrentByte;
		private byte StepsLeft;

		private Status CurrentStatus = Status.Inactive;

		private enum Status
		{
			Inactive,
			PrepareReceive,
			PrepareSend,
			Receive,
			Send
		}

		public TI83LinkPort(TI83 Parent)
		{
			this.Parent = Parent;
		}

		public void Update()
		{
			if (CurrentStatus == Status.PrepareReceive)
			{
				//Get the first byte, and start sending it.
				CurrentByte = CurrentData.Dequeue();
				CurrentStatus = Status.Receive;
				BitsLeft = 8;
				StepsLeft = 5;
			}

			if (CurrentStatus == Status.PrepareSend && Parent.LinkState != 3)
			{
				CurrentStatus = Status.Send;
				BitsLeft = 8;
				StepsLeft = 5;
				CurrentByte = 0;
			}

			if (CurrentStatus == Status.Receive)
			{
				switch (StepsLeft)
				{
					case 5:
						//Receive step 1: Lower the other device's line.
						Parent.LinkInput = ((CurrentByte & 1) == 1) ? 2 : 1;
						CurrentByte >>= 1;
						StepsLeft--;
						break;

					case 4:
						//Receive step 2: Wait for the calc to lower the other line.
						if ((Parent.LinkState & 3) == 0)
							StepsLeft--;
						break;

					case 3:
						//Receive step 3: Raise the other device's line back up.
						Parent.LinkInput = 0;
						StepsLeft--;
						break;

					case 2:
						//Receive step 4: Wait for the calc to raise its line back up.
						if ((Parent.LinkState & 3) == 3)
							StepsLeft--;
						break;

					case 1:
						//Receive step 5: Finish.   
						BitsLeft--;

						if (BitsLeft == 0)
						{
							if (CurrentData.Count > 0)
								CurrentStatus = Status.PrepareReceive;
							else
							{
								CurrentStatus = Status.Inactive;
								if (NextStep != null)
									NextStep();
							}
						}
						else
							//next bit in the current byte.
							StepsLeft = 5;
						break;
				}
			}
			else if (CurrentStatus == Status.Send)
			{
				switch (StepsLeft)
				{
					case 5:
						//Send step 1: Calc lowers a line.
						if (Parent.LinkState != 3)
						{
							int Bit = Parent.LinkState & 1;
							int Shift = 8 - BitsLeft;
							CurrentByte |= (byte)(Bit << Shift);
							StepsLeft--;
						}
						break;

					case 4:
						//send step 2: Lower our line.
						Parent.LinkInput = Parent.LinkOutput ^ 3;
						StepsLeft--;
						break;

					case 3:
						//Send step 3: wait for the calc to raise its line.
						if ((Parent.LinkOutput & 3) == 0)
							StepsLeft--;
						break;

					case 2:
						//Send step 4: raise the other devices lines.
						Parent.LinkInput = 0;
						StepsLeft--;
						break;

					case 1:
						//Send step 5: Finish
						BitsLeft--;

						if (BitsLeft == 0)
						{
							BytesToSend--;
							CurrentData.Enqueue(CurrentByte);

							if (BytesToSend > 0)
								CurrentStatus = Status.PrepareSend;
							else
							{
								CurrentStatus = Status.Inactive;
								if (NextStep != null)
									NextStep();
							}
						}
						else
						{
							//next bit in the current byte.
							StepsLeft = 5;
						}
						break;
				}
			}
		}

		public void SendFileToCalc(FileStream FS, bool Verify)
		{
			if (Verify)
				VerifyFile(FS);

			FS.Seek(55, SeekOrigin.Begin);
			CurrentFile = FS;
			SendNextFile();
		}

		private void VerifyFile(FileStream FS)
		{
			//Verify the file format.
			byte[] Expected = new byte[] { 0x2a, 0x2a, 0x54, 0x49, 0x38, 0x33, 0x2a, 0x2a, 0x1a, 0x0a, 0x00 };
			byte[] Actual = new byte[11];

			FS.Seek(0, SeekOrigin.Begin);
			FS.Read(Actual, 0, 11);

			//Check the header.
			for (int n = 0; n < 11; n++)
				if (Expected[n] != Actual[n])
				{
					FS.Close();
					throw new IOException("Invalid Header.");
				}

			//Seek to the end of the comment.
			FS.Seek(53, SeekOrigin.Begin);

			int Size = FS.ReadByte() + FS.ReadByte() * 256;

			if (FS.Length != Size + 57)
			{
				FS.Close();
				throw new IOException("Invalid file length.");
			}

			//Verify the checksum.
			ushort Checksum = 0;
			for (int n = 0; n < Size; n++)
				Checksum += (ushort)FS.ReadByte();

			ushort ActualChecksum = (ushort)(FS.ReadByte() + FS.ReadByte() * 256);

			if (Checksum != ActualChecksum)
			{
				FS.Close();
				throw new IOException("Invalid Checksum.");
			}
		}

		private void SendNextFile()
		{
			byte[] Header = new byte[13];
			if (!CurrentFile.CanRead || CurrentFile.Read(Header, 0, 13) != 13)
			{
				//End of file.
				CurrentFile.Close();
				return;
			}

			int Size = Header[2] + Header[3] * 256;
			VariableData = new byte[Size + 2];
			CurrentFile.Read(VariableData, 0, Size + 2);

			//Request to send the file.
			CurrentData.Clear();

			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0xC9);
			foreach (byte B in Header)
				CurrentData.Enqueue(B);

			//Calculate the checksum for the command.
			ushort Checksum = 0;
			for (int n = 2; n < Header.Length; n++)
				Checksum += Header[n];

			CurrentData.Enqueue((byte)(Checksum % 256));
			CurrentData.Enqueue((byte)(Checksum / 256));

			//Finalize the command.
			CurrentStatus = Status.PrepareReceive;
			NextStep = ReceiveReqAck;
			Parent.LinkActive = true;
		}

		private void ReceiveReqAck()
		{
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			BytesToSend = 8;
			CurrentStatus = Status.PrepareSend;
			NextStep = SendVariableData;
		}

		private void SendVariableData()
		{
			// Check to see if out of memory first.
			CurrentData.Dequeue();
			CurrentData.Dequeue();
			CurrentData.Dequeue();
			CurrentData.Dequeue();
			CurrentData.Dequeue();

			if (CurrentData.Dequeue() == 0x36)
			{
				OutOfMemory();
			}
			else
			{
				CurrentData.Clear();

				CurrentData.Enqueue(0x03);
				CurrentData.Enqueue(0x56);
				CurrentData.Enqueue(0x00);
				CurrentData.Enqueue(0x00);

				CurrentData.Enqueue(0x03);
				CurrentData.Enqueue(0x15);

				//Add variable data.
				foreach (byte B in VariableData)
					CurrentData.Enqueue(B);

				//Calculate the checksum.
				ushort Checksum = 0;
				for (int n = 2; n < VariableData.Length; n++)
					Checksum += VariableData[n];

				CurrentData.Enqueue((byte)(Checksum % 256));
				CurrentData.Enqueue((byte)(Checksum / 256));

				CurrentStatus = Status.PrepareReceive;
				NextStep = ReceiveDataAck;
				Parent.LinkActive = true;
			}
		}

		private void ReceiveDataAck()
		{
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			BytesToSend = 4;
			CurrentStatus = Status.PrepareSend;
			NextStep = EndTransmission;
		}

		private void EndTransmission()
		{
			CurrentData.Clear();

			// Send the end transmission command.
			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0x92);
			CurrentData.Enqueue(0x00);
			CurrentData.Enqueue(0x00);

			CurrentStatus = Status.PrepareReceive;
			NextStep = FinalizeFile;
			Parent.LinkActive = true;
		}

		private void OutOfMemory()
		{
			CurrentFile.Close();
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			BytesToSend = 3;
			CurrentStatus = Status.PrepareSend;
			NextStep = EndOutOfMemory;
		}

		private void EndOutOfMemory()
		{
			CurrentData.Clear();

			// Send the end transmission command.
			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0x56);
			CurrentData.Enqueue(0x01);
			CurrentData.Enqueue(0x00);

			CurrentStatus = Status.PrepareReceive;
			NextStep = FinalizeFile;
			Parent.LinkActive = true;
		}

		private void FinalizeFile()
		{
			// Resets the link software, and checks to see if there is an additional file to send.
			CurrentData.Clear();
			Parent.LinkActive = false;
			NextStep = null;
			SendNextFile();
		}
	}
}
