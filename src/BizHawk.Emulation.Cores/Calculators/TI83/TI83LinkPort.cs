using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	public class TI83LinkPort
	{
		// Emulates TI linking software.
		// See http://www.ticalc.org/archives/files/fileinfo/294/29418.html for documentation

		// Note: Each hardware read/write to the link port calls tthe update method.
		private readonly TI83 Parent;
		private readonly Queue<byte> CurrentData = new Queue<byte>();

		private Stream _currentFile;
		private byte[] _variableData;

		private Action _nextStep;
		
		private ushort _bytesToSend;
		private byte _bitsLeft;
		private byte _currentByte;
		private byte _stepsLeft;

		private Status _currentStatus = Status.Inactive;

		private enum Status
		{
			Inactive,
			PrepareReceive,
			PrepareSend,
			Receive,
			Send
		}

		public TI83LinkPort(TI83 parent)
		{
			Parent = parent;
		}

		public void Update()
		{
			if (_currentStatus == Status.PrepareReceive)
			{
				// Get the first byte, and start sending it.
				_currentByte = CurrentData.Dequeue();
				_currentStatus = Status.Receive;
				_bitsLeft = 8;
				_stepsLeft = 5;
			}

			if (_currentStatus == Status.PrepareSend && Parent.LinkState != 3)
			{
				_currentStatus = Status.Send;
				_bitsLeft = 8;
				_stepsLeft = 5;
				_currentByte = 0;
			}

			if (_currentStatus == Status.Receive)
			{
				switch (_stepsLeft)
				{
					case 5:
						// Receive step 1: Lower the other device's line.
						Parent.LinkInput = ((_currentByte & 1) == 1) ? 2 : 1;
						_currentByte >>= 1;
						_stepsLeft--;
						break;

					case 4:
						// Receive step 2: Wait for the calc to lower the other line.
						if ((Parent.LinkState & 3) == 0)
						{
							_stepsLeft--;
						}

						break;

					case 3:
						// Receive step 3: Raise the other device's line back up.
						Parent.LinkInput = 0;
						_stepsLeft--;
						break;

					case 2:
						// Receive step 4: Wait for the calc to raise its line back up.
						if ((Parent.LinkState & 3) == 3)
						{
							_stepsLeft--;
						}

						break;

					case 1:
						// Receive step 5: Finish.
						_bitsLeft--;

						if (_bitsLeft == 0)
						{
							if (CurrentData.Count > 0)
							{
								_currentStatus = Status.PrepareReceive;
							}
							else
							{
								_currentStatus = Status.Inactive;
								_nextStep?.Invoke();
							}
						}
						else
						{
							// Next bit in the current byte.
							_stepsLeft = 5;
						}

						break;
				}
			}
			else if (_currentStatus == Status.Send)
			{
				switch (_stepsLeft)
				{
					case 5:
						// Send step 1: Calc lowers a line.
						if (Parent.LinkState != 3)
						{
							int bit = Parent.LinkState & 1;
							int shift = 8 - _bitsLeft;
							_currentByte |= (byte)(bit << shift);
							_stepsLeft--;
						}

						break;

					case 4:
						// Send step 2: Lower our line.
						Parent.LinkInput = Parent.LinkOutput ^ 3;
						_stepsLeft--;
						break;

					case 3:
						// Send step 3: wait for the calc to raise its line.
						if ((Parent.LinkOutput & 3) == 0)
						{
							_stepsLeft--;
						}

						break;

					case 2:
						// Send step 4: raise the other devices lines.
						Parent.LinkInput = 0;
						_stepsLeft--;
						break;

					case 1:
						// Send step 5: Finish
						_bitsLeft--;

						if (_bitsLeft == 0)
						{
							_bytesToSend--;
							CurrentData.Enqueue(_currentByte);

							if (_bytesToSend > 0)
							{
								_currentStatus = Status.PrepareSend;
							}
							else
							{
								_currentStatus = Status.Inactive;
								_nextStep?.Invoke();
							}
						}
						else
						{
							// Next bit in the current byte.
							_stepsLeft = 5;
						}

						break;
				}
			}
		}

		public void SendFileToCalc(Stream fs, bool verify)
		{
			if (verify)
			{
				VerifyFile(fs);
			}

			fs.Seek(55, SeekOrigin.Begin);
			_currentFile = fs;
			SendNextFile();
		}

		private void VerifyFile(Stream fs)
		{
			// Verify the file format.
			byte[] expected = { 0x2a, 0x2a, 0x54, 0x49, 0x38, 0x33, 0x2a, 0x2a, 0x1a, 0x0a, 0x00 };
			byte[] actual = new byte[11];

			fs.Seek(0, SeekOrigin.Begin);
			fs.Read(actual, 0, 11);

			// Check the header.
			for (int n = 0; n < 11; n++)
			{
				if (expected[n] != actual[n])
				{
					fs.Close();
					throw new IOException("Invalid Header.");
				}
			}

			// Seek to the end of the comment.
			fs.Seek(53, SeekOrigin.Begin);

			int size = fs.ReadByte() + (fs.ReadByte() * 256);

			if (fs.Length != size + 57)
			{
				fs.Close();
				throw new IOException("Invalid file length.");
			}

			// Verify the checksum.
			ushort checksum = 0;
			for (int n = 0; n < size; n++)
			{
				checksum += (ushort)fs.ReadByte();
			}

			ushort actualChecksum = (ushort)(fs.ReadByte() + (fs.ReadByte() * 256));

			if (checksum != actualChecksum)
			{
				fs.Close();
				throw new IOException("Invalid Checksum.");
			}
		}

		private void SendNextFile()
		{
			byte[] header = new byte[13];
			if (!_currentFile.CanRead || _currentFile.Read(header, 0, 13) != 13)
			{
				// End of file.
				_currentFile.Close();
				return;
			}

			int size = header[2] + (header[3] * 256);
			_variableData = new byte[size + 2];
			_currentFile.Read(_variableData, 0, size + 2);

			// Request to send the file.
			CurrentData.Clear();

			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0xC9);
			foreach (byte b in header)
			{
				CurrentData.Enqueue(b);
			}

			// Calculate the checksum for the command.
			ushort checksum = 0;
			for (int n = 2; n < header.Length; n++)
			{
				checksum += header[n];
			}

			CurrentData.Enqueue((byte)(checksum % 256));
			CurrentData.Enqueue((byte)(checksum / 256));

			// Finalize the command.
			_currentStatus = Status.PrepareReceive;
			_nextStep = ReceiveReqAck;
			Parent.LinkActive = true;
		}

		private void ReceiveReqAck()
		{
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			_bytesToSend = 8;
			_currentStatus = Status.PrepareSend;
			_nextStep = SendVariableData;
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

				// Add variable data.
				foreach (byte b in _variableData)
				{
					CurrentData.Enqueue(b);
				}

				// Calculate the checksum.
				ushort checksum = 0;
				for (int n = 2; n < _variableData.Length; n++)
				{
					checksum += _variableData[n];
				}

				CurrentData.Enqueue((byte)(checksum % 256));
				CurrentData.Enqueue((byte)(checksum / 256));

				_currentStatus = Status.PrepareReceive;
				_nextStep = ReceiveDataAck;
				Parent.LinkActive = true;
			}
		}

		private void ReceiveDataAck()
		{
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			_bytesToSend = 4;
			_currentStatus = Status.PrepareSend;
			_nextStep = EndTransmission;
		}

		private void EndTransmission()
		{
			CurrentData.Clear();

			// Send the end transmission command.
			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0x92);
			CurrentData.Enqueue(0x00);
			CurrentData.Enqueue(0x00);

			_currentStatus = Status.PrepareReceive;
			_nextStep = FinalizeFile;
			Parent.LinkActive = true;
		}

		private void OutOfMemory()
		{
			_currentFile.Close();
			Parent.LinkActive = false;
			CurrentData.Clear();

			// Prepare to receive the Aknowledgement response from the calculator.
			_bytesToSend = 3;
			_currentStatus = Status.PrepareSend;
			_nextStep = EndOutOfMemory;
		}

		private void EndOutOfMemory()
		{
			CurrentData.Clear();

			// Send the end transmission command.
			CurrentData.Enqueue(0x03);
			CurrentData.Enqueue(0x56);
			CurrentData.Enqueue(0x01);
			CurrentData.Enqueue(0x00);

			_currentStatus = Status.PrepareReceive;
			_nextStep = FinalizeFile;
			Parent.LinkActive = true;
		}

		private void FinalizeFile()
		{
			// Resets the link software, and checks to see if there is an additional file to send.
			CurrentData.Clear();
			Parent.LinkActive = false;
			_nextStep = null;
			SendNextFile();
		}
	}
}
