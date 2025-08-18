﻿using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// The abstract class that all emulated models will inherit from
	/// * Input *
	/// </summary>
	public abstract partial class SpectrumBase
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

		private bool pressed_Play;
		private bool pressed_Stop;
		private bool pressed_RTZ;
		private bool pressed_NextTape;
		private bool pressed_PrevTape;
		private bool pressed_NextBlock;
		private bool pressed_PrevBlock;
		private bool pressed_TapeStatus;
		private bool pressed_NextDisk;
		private bool pressed_PrevDisk;
		private bool pressed_EjectDisk;
		private bool pressed_DiskStatus;
		private bool pressed_HardReset;
		private bool pressed_SoftReset;

		/// <summary>
		/// Cycles through all the input callbacks
		/// This should be done once per frame
		/// </summary>
		public void PollInput()
		{
			Spectrum.InputCallbacks.Call();

			lock (this)
			{
				// parse single keyboard matrix keys
				for (var i = 0; i < KeyboardDevice.KeyboardMatrix.Length; i++)
				{
					string key = KeyboardDevice.KeyboardMatrix[i];
					bool prevState = KeyboardDevice.GetKeyStatus(key);
					bool currState = Spectrum._controller.IsPressed(key);

					if (currState != prevState)
						KeyboardDevice.SetKeyStatus(key, currState);
				}

				// non matrix keys
				foreach (string k in KeyboardDevice.NonMatrixKeys)
				{
					if (!k.StartsWithOrdinal("Key"))
						continue;

					bool currState = Spectrum._controller.IsPressed(k);

					KeyboardDevice.SetKeyStatus(k, currState);
				}

				// J1
				foreach (string j in JoystickCollection[0].ButtonCollection)
				{
					bool prevState = JoystickCollection[0].GetJoyInput(j);
					bool currState = Spectrum._controller.IsPressed(j);

					if (currState != prevState)
						JoystickCollection[0].SetJoyInput(j, currState);
				}

				// J2
				foreach (string j in JoystickCollection[1].ButtonCollection)
				{
					bool prevState = JoystickCollection[1].GetJoyInput(j);
					bool currState = Spectrum._controller.IsPressed(j);

					if (currState != prevState)
						JoystickCollection[1].SetJoyInput(j, currState);
				}

				// J3
				foreach (string j in JoystickCollection[2].ButtonCollection)
				{
					bool prevState = JoystickCollection[2].GetJoyInput(j);
					bool currState = Spectrum._controller.IsPressed(j);

					if (currState != prevState)
						JoystickCollection[2].SetJoyInput(j, currState);
				}
			}

			// Tape control
			if (Spectrum._controller.IsPressed(Play))
			{
				if (!pressed_Play)
				{
					Spectrum.OSD_FireInputMessage(Play);
					TapeDevice.Play();
					pressed_Play = true;
				}
			}
			else
				pressed_Play = false;

			if (Spectrum._controller.IsPressed(Stop))
			{
				if (!pressed_Stop)
				{
					Spectrum.OSD_FireInputMessage(Stop);
					TapeDevice.Stop();
					pressed_Stop = true;
				}
			}
			else
				pressed_Stop = false;

			if (Spectrum._controller.IsPressed(RTZ))
			{
				if (!pressed_RTZ)
				{
					Spectrum.OSD_FireInputMessage(RTZ);
					TapeDevice.RTZ();
					pressed_RTZ = true;
				}
			}
			else
				pressed_RTZ = false;

			if (Spectrum._controller.IsPressed(Record))
			{
				//TODO
			}
			if (Spectrum._controller.IsPressed(NextTape))
			{
				if (!pressed_NextTape)
				{
					Spectrum.OSD_FireInputMessage(NextTape);
					TapeMediaIndex++;
					pressed_NextTape = true;
				}
			}
			else
				pressed_NextTape = false;

			if (Spectrum._controller.IsPressed(PrevTape))
			{
				if (!pressed_PrevTape)
				{
					Spectrum.OSD_FireInputMessage(PrevTape);
					TapeMediaIndex--;
					pressed_PrevTape = true;
				}
			}
			else
				pressed_PrevTape = false;

			if (Spectrum._controller.IsPressed(NextBlock))
			{
				if (!pressed_NextBlock)
				{
					Spectrum.OSD_FireInputMessage(NextBlock);
					TapeDevice.SkipBlock(true);
					pressed_NextBlock = true;
				}
			}
			else
				pressed_NextBlock = false;

			if (Spectrum._controller.IsPressed(PrevBlock))
			{
				if (!pressed_PrevBlock)
				{
					Spectrum.OSD_FireInputMessage(PrevBlock);
					TapeDevice.SkipBlock(false);
					pressed_PrevBlock = true;
				}
			}
			else
				pressed_PrevBlock = false;

			if (Spectrum._controller.IsPressed(TapeStatus))
			{
				if (!pressed_TapeStatus)
				{
					Spectrum.OSD_ShowTapeStatus();
					pressed_TapeStatus = true;
				}
			}
			else
				pressed_TapeStatus = false;

			if (Spectrum._controller.IsPressed(HardResetStr))
			{
				if (!pressed_HardReset)
				{
					HardReset();
					pressed_HardReset = true;
				}
			}
			else
				pressed_HardReset = false;

			if (Spectrum._controller.IsPressed(SoftResetStr))
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
			if (Spectrum._controller.IsPressed(NextDisk))
			{
				if (!pressed_NextDisk)
				{
					Spectrum.OSD_FireInputMessage(NextDisk);
					DiskMediaIndex++;
					pressed_NextDisk = true;
				}
			}
			else
				pressed_NextDisk = false;

			if (Spectrum._controller.IsPressed(PrevDisk))
			{
				if (!pressed_PrevDisk)
				{
					Spectrum.OSD_FireInputMessage(PrevDisk);
					DiskMediaIndex--;
					pressed_PrevDisk = true;
				}
			}
			else
				pressed_PrevDisk = false;

			if (Spectrum._controller.IsPressed(EjectDisk))
			{
				if (!pressed_EjectDisk)
				{
					Spectrum.OSD_FireInputMessage(EjectDisk);
					UPDDiskDevice?.FDD_EjectDisk();
				}
			}
			else
				pressed_EjectDisk = false;

			if (Spectrum._controller.IsPressed(DiskStatus))
			{
				if (!pressed_DiskStatus)
				{
					Spectrum.OSD_ShowDiskStatus();
					pressed_DiskStatus = true;
				}
			}
			else
				pressed_DiskStatus = false;
		}

		/// <summary>
		/// Instantiates the joysticks array
		/// </summary>
		protected void InitJoysticks(List<JoystickType> joys)
		{
			var jCollection = new List<IJoystick>();

			for (int i = 0; i < joys.Count; i++)
			{
				jCollection.Add(InstantiateJoystick(joys[i], i + 1));
			}

			JoystickCollection = jCollection.ToArray();

			for (int i = 0; i < JoystickCollection.Length; i++)
			{
				Spectrum.OSD_FireInputMessage("Joystick " + (i + 1) + ": " + JoystickCollection[i].JoyType);
			}
		}

		/// <summary>
		/// Instantiates a new IJoystick object
		/// </summary>
		public IJoystick InstantiateJoystick(JoystickType type, int playerNumber)
		{
			switch (type)
			{
				case JoystickType.Kempston:
					return new KempstonJoystick(this, playerNumber);
				case JoystickType.Cursor:
					return new CursorJoystick(this, playerNumber);
				case JoystickType.SinclairLEFT:
					return new SinclairJoystick1(this, playerNumber);
				case JoystickType.SinclairRIGHT:
					return new SinclairJoystick2(this, playerNumber);
				case JoystickType.NULL:
					return new NullJoystick(this, playerNumber);
			}

			return null;
		}

		/// <summary>
		/// Returns a IJoystick object depending on the type (or null if not found)
		/// </summary>
		protected IJoystick LocateUniqueJoystick(JoystickType type)
		{
			return JoystickCollection.FirstOrDefault(a => a.JoyType == type);
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
