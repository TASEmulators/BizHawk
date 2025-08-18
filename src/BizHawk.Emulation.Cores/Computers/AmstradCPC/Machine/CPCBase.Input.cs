﻿using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// The abstract class that all emulated models will inherit from
	/// * Input *
	/// </summary>
	public abstract partial class CPCBase
	{
		private readonly string Play = "Play Tape";
		private readonly string Stop = "Stop Tape";
		private readonly string RTZ = "RTZ Tape";
		private readonly string Record = "Record Tape";
		private readonly string NextTape = "Insert Next Tape";
		private readonly string PrevTape = "Insert Previous Tape";
		private readonly string NextBlock = "Next Tape Block";
		private readonly string PrevBlock = "Prev Tape Block";
		private readonly string TapeStatus = "Get Tape Status";

		private readonly string NextDisk = "Insert Next Disk";
		private readonly string PrevDisk = "Insert Previous Disk";
		private readonly string EjectDisk = "Eject Current Disk";
		private readonly string DiskStatus = "Get Disk Status";

		private readonly string HardResetStr = "Power";
		private readonly string SoftResetStr = "Reset";

		private bool pressed_Play = false;
		private bool pressed_Stop = false;
		private bool pressed_RTZ = false;
		private bool pressed_NextTape = false;
		private bool pressed_PrevTape = false;
		private bool pressed_NextBlock = false;
		private bool pressed_PrevBlock = false;
		private bool pressed_TapeStatus = false;
		private bool pressed_NextDisk = false;
		private bool pressed_PrevDisk = false;
		private bool pressed_EjectDisk = false;
		private bool pressed_DiskStatus = false;
		private bool pressed_HardReset = false;
		private bool pressed_SoftReset = false;

		/// <summary>
		/// Cycles through all the input callbacks
		/// This should be done once per frame
		/// </summary>
		public void PollInput()
		{
			CPC.InputCallbacks.Call();

			lock (this)
			{
				// parse single keyboard matrix keys.
				// J1 and J2 are scanned as part of the keyboard
				for (var i = 0; i < KeyboardDevice.KeyboardMatrix.Length; i++)
				{
					string key = KeyboardDevice.KeyboardMatrix[i];
					bool prevState = KeyboardDevice.GetKeyStatus(key);
					bool currState = CPC._controller.IsPressed(key);

					if (currState != prevState)
						KeyboardDevice.SetKeyStatus(key, currState);
				}

				// non matrix keys (J2)
				foreach (string k in KeyboardDevice.NonMatrixKeys)
				{
					if (!k.StartsWithOrdinal("P2"))
						continue;

					bool currState = CPC._controller.IsPressed(k);

					switch (k)
					{
						case "P2 Up":
							if (currState)
								KeyboardDevice.SetKeyStatus("Key 6", true);
							else if (!KeyboardDevice.GetKeyStatus("Key 6"))
								KeyboardDevice.SetKeyStatus("Key 6", false);
							break;
						case "P2 Down":
							if (currState)
								KeyboardDevice.SetKeyStatus("Key 5", true);
							else if (!KeyboardDevice.GetKeyStatus("Key 5"))
								KeyboardDevice.SetKeyStatus("Key 5", false);
							break;
						case "P2 Left":
							if (currState)
								KeyboardDevice.SetKeyStatus("Key R", true);
							else if (!KeyboardDevice.GetKeyStatus("Key R"))
								KeyboardDevice.SetKeyStatus("Key R", false);
							break;
						case "P2 Right":
							if (currState)
								KeyboardDevice.SetKeyStatus("Key T", true);
							else if (!KeyboardDevice.GetKeyStatus("Key T"))
								KeyboardDevice.SetKeyStatus("Key T", false);
							break;
						case "P2 Fire":
							if (currState)
								KeyboardDevice.SetKeyStatus("Key G", true);
							else if (!KeyboardDevice.GetKeyStatus("Key G"))
								KeyboardDevice.SetKeyStatus("Key G", false);
							break;
					}
				}
			}

			// Tape control
			if (CPC._controller.IsPressed(Play))
			{
				if (!pressed_Play)
				{
					CPC.OSD_FireInputMessage(Play);
					TapeDevice.Play();
					pressed_Play = true;
				}
			}
			else
				pressed_Play = false;

			if (CPC._controller.IsPressed(Stop))
			{
				if (!pressed_Stop)
				{
					CPC.OSD_FireInputMessage(Stop);
					TapeDevice.Stop();
					pressed_Stop = true;
				}
			}
			else
				pressed_Stop = false;

			if (CPC._controller.IsPressed(RTZ))
			{
				if (!pressed_RTZ)
				{
					CPC.OSD_FireInputMessage(RTZ);
					TapeDevice.RTZ();
					pressed_RTZ = true;
				}
			}
			else
				pressed_RTZ = false;

			if (CPC._controller.IsPressed(Record))
			{
				//TODO
			}
			if (CPC._controller.IsPressed(NextTape))
			{
				if (!pressed_NextTape)
				{
					CPC.OSD_FireInputMessage(NextTape);
					TapeMediaIndex++;
					pressed_NextTape = true;
				}
			}
			else
				pressed_NextTape = false;

			if (CPC._controller.IsPressed(PrevTape))
			{
				if (!pressed_PrevTape)
				{
					CPC.OSD_FireInputMessage(PrevTape);
					TapeMediaIndex--;
					pressed_PrevTape = true;
				}
			}
			else
				pressed_PrevTape = false;

			if (CPC._controller.IsPressed(NextBlock))
			{
				if (!pressed_NextBlock)
				{
					CPC.OSD_FireInputMessage(NextBlock);
					TapeDevice.SkipBlock(true);
					pressed_NextBlock = true;
				}
			}
			else
				pressed_NextBlock = false;

			if (CPC._controller.IsPressed(PrevBlock))
			{
				if (!pressed_PrevBlock)
				{
					CPC.OSD_FireInputMessage(PrevBlock);
					TapeDevice.SkipBlock(false);
					pressed_PrevBlock = true;
				}
			}
			else
				pressed_PrevBlock = false;

			if (CPC._controller.IsPressed(TapeStatus))
			{
				if (!pressed_TapeStatus)
				{
					//Spectrum.OSD_FireInputMessage(TapeStatus);
					CPC.OSD_ShowTapeStatus();
					pressed_TapeStatus = true;
				}
			}
			else
				pressed_TapeStatus = false;

			if (CPC._controller.IsPressed(HardResetStr))
			{
				if (!pressed_HardReset)
				{
					HardReset();
					pressed_HardReset = true;
				}
			}
			else
				pressed_HardReset = false;

			if (CPC._controller.IsPressed(SoftResetStr))
			{
				if (!pressed_SoftReset)
				{
					SoftReset();
					pressed_SoftReset = true;
				}
			}
			else
				pressed_SoftReset = false;

			// disk control
			if (CPC._controller.IsPressed(NextDisk))
			{
				if (!pressed_NextDisk)
				{
					CPC.OSD_FireInputMessage(NextDisk);
					DiskMediaIndex++;
					pressed_NextDisk = true;
				}
			}
			else
				pressed_NextDisk = false;

			if (CPC._controller.IsPressed(PrevDisk))
			{
				if (!pressed_PrevDisk)
				{
					CPC.OSD_FireInputMessage(PrevDisk);
					DiskMediaIndex--;
					pressed_PrevDisk = true;
				}
			}
			else
				pressed_PrevDisk = false;

			if (CPC._controller.IsPressed(EjectDisk))
			{
				if (!pressed_EjectDisk)
				{
					CPC.OSD_FireInputMessage(EjectDisk);
					//if (UPDDiskDevice != null)
					//  UPDDiskDevice.FDD_EjectDisk();
				}
			}
			else
				pressed_EjectDisk = false;

			if (CPC._controller.IsPressed(DiskStatus))
			{
				if (!pressed_DiskStatus)
				{
					//Spectrum.OSD_FireInputMessage(TapeStatus);
					CPC.OSD_ShowDiskStatus();
					pressed_DiskStatus = true;
				}
			}
			else
				pressed_DiskStatus = false;
		}

		/// <summary>
		/// Signs whether input read has been requested
		/// This forms part of the IEmulator LagFrame implementation
		/// </summary>
		private bool inputRead;
		public bool InputRead
		{
			get => inputRead;
			set => inputRead = value;
		}
	}
}
