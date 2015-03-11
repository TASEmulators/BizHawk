using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common.InputAdapterExtensions;

namespace BizHawk.Client.EmuHawk
{
	public class MovieZone
	{
		public string Name;

		public int Start;
		public int Length;
		private string _inputKey;
		public string InputKey
		{
			get { return _inputKey; }
			set { _inputKey = value; ReSetLog(); }
		}
		private string[] _log;

		public bool Replace = true;
		public bool Overlay = false;

		private Bk2ControllerAdapter controller;
		private Bk2ControllerAdapter targetController;

		public MovieZone()
		{
		}
		public MovieZone(TasMovie movie, int start, int length, string key = "")
		{
			Bk2LogEntryGenerator lg = Global.MovieSession.LogGeneratorInstance() as Bk2LogEntryGenerator;
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			targetController = new Bk2ControllerAdapter();
			targetController.Type = Global.Emulator.ControllerDefinition;
			targetController.LatchFromSource(targetController); // Reference and create all buttons

			if (key == "")
				key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");
			key = key.Substring(0, key.Length - 1);

			_inputKey = key;
			Length = length;
			_log = new string[length];

			// Get a IController that only contains buttons in key.
			string[] keys = key.Split('|');
			ControllerDefinition d = new ControllerDefinition();
			for (int i = 0; i < keys.Length; i++)
			{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(keys[i]))
					d.BoolButtons.Add(keys[i]);
				else
					d.FloatControls.Add(keys[i]);
			}

			controller = new Bk2ControllerAdapter() { Type = d };
			Bk2LogEntryGenerator logGenerator = new Bk2LogEntryGenerator("");
			logGenerator.SetSource(controller);
			logGenerator.GenerateLogEntry(); // Reference and create all buttons.
			for (int i = 0; i < length; i++)
			{
				controller.LatchFromSource(movie.GetInputState(i + start));
				_log[i] = logGenerator.GenerateLogEntry();
			}
		}
		private void ReSetLog()
		{
			// Get a IController that only contains buttons in key.
			string[] keys = _inputKey.Split('|');
			ControllerDefinition d = new ControllerDefinition();
			for (int i = 0; i < keys.Length; i++)
			{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(keys[i]))
					d.BoolButtons.Add(keys[i]);
				else
					d.FloatControls.Add(keys[i]);
			}
			Bk2ControllerAdapter newController = new Bk2ControllerAdapter() { Type = d };

			Bk2LogEntryGenerator logGenerator = new Bk2LogEntryGenerator("");
			logGenerator.SetSource(newController);
			logGenerator.GenerateLogEntry(); // Reference and create all buttons.
			// Reset all buttons in targetController (it may still have buttons that aren't being set here set true)
			Bk2LogEntryGenerator tC = new Bk2LogEntryGenerator("");
			tC.SetSource(targetController);
			targetController.SetControllersAsMnemonic(tC.EmptyEntry);
			for (int i = 0; i < Length; i++)
			{
				controller.SetControllersAsMnemonic(_log[i]);
				LatchFromSourceButtons(targetController, controller);
				newController.LatchFromSource(targetController);
				_log[i] = logGenerator.GenerateLogEntry();
			}

			controller = newController;
		}

		public void PlaceZone(TasMovie movie)
		{
			if (Start > movie.InputLogLength)
			{ // Cannot place a frame here. Find a nice way around this.
				return;
			}

			// TODO: This should probably do something with undo history batches/naming.
			if (!Replace)
				movie.InsertEmptyFrame(Start, Length);

			Bk2LogEntryGenerator logGenerator = new Bk2LogEntryGenerator("");
			logGenerator.SetSource(targetController);
			if (Overlay)
			{
				for (int i = 0; i < Length; i++)
				{ // Overlay the frames.
					controller.SetControllersAsMnemonic(_log[i]);
					LatchFromSourceButtons(targetController, controller);
					ORLatchFromSource(targetController, movie.GetInputState(i + Start));
					movie.SetFrame(i + Start, logGenerator.GenerateLogEntry());
				}
			}
			else
			{
				for (int i = 0; i < Length; i++)
				{ // Copy over the frame.
					controller.SetControllersAsMnemonic(_log[i]);
					LatchFromSourceButtons(targetController, controller);
					movie.SetFrame(i + Start, logGenerator.GenerateLogEntry());
				}
			}

			if (movie is TasMovie) // Assume TAStudio is open?
			{
				if (Global.Emulator.Frame > Start)
				{
					// TODO: Go to start of macro? Ask TAStudio to do that?

					// TasMovie.InvalidateAfter(Start) [this is private]
					// Load last state, Emulate to Start

					// Or do this, if TAStudio has to be open.
					if (GlobalWin.Tools.IsLoaded<TAStudio>())
						(GlobalWin.Tools.Get<TAStudio>() as TAStudio).GoToFrame(Start);

					GlobalWin.Tools.UpdateBefore();
					GlobalWin.Tools.UpdateAfter();
				}
				else if (GlobalWin.Tools.IsLoaded<TAStudio>())
				{
					GlobalWin.Tools.Get<TAStudio>().UpdateValues();
				}
			}
		}

		public void Save(string fileName)
		{
			// Save the controller definition/LogKey
			// Save the controller name and player count. (Only for the user.)
			// Save whether or not the macro should use overlay input, and/or replace
			string[] header = new string[4];
			header[0] = InputKey;
			header[1] = Global.Emulator.ControllerDefinition.Name;
			header[2] = Global.Emulator.ControllerDefinition.PlayerCount.ToString();
			header[3] = Overlay.ToString() + "," + Replace.ToString();

			File.WriteAllLines(fileName, header);
			File.AppendAllLines(fileName, _log);
		}
		public MovieZone(string fileName)
		{
			if (!File.Exists(fileName))
				return;

			string[] readText = File.ReadAllLines(fileName);
			// If the LogKey contains buttons/controls not accepted by the emulator,
			//	tell the user and display the macro's controller name and player count
			_inputKey = readText[0];
			Bk2LogEntryGenerator lg = Global.MovieSession.LogGeneratorInstance() as Bk2LogEntryGenerator;
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			string key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");
			key = key.Substring(0, key.Length - 1);
			string[] emuKeys = key.Split('|');
			string[] macroKeys = _inputKey.Split('|');
			for (int i = 0; i < macroKeys.Length; i++)
			{
				if (!emuKeys.Contains(macroKeys[i]))
				{
					System.Windows.Forms.MessageBox.Show("The selected macro is not compatible with the current emulator core." +
						"\nMacro controller: " + readText[1] + "\nMacro player count: " + readText[2], "Error");
					return;
				}
			}

			// Settings
			string[] settings = readText[3].Split(',');
			Overlay = Convert.ToBoolean(settings[0]);
			Replace = Convert.ToBoolean(settings[1]);

			_log = new string[readText.Length - 4];
			readText.ToList().CopyTo(4, _log, 0, _log.Length);
			Length = _log.Length;
			Start = 0;

			Name = Path.GetFileNameWithoutExtension(fileName);

			// Adapters
			targetController = new Bk2ControllerAdapter();
			targetController.Type = Global.Emulator.ControllerDefinition;
			targetController.LatchFromSource(targetController); // Reference and create all buttons
			string[] keys = _inputKey.Split('|');
			ControllerDefinition d = new ControllerDefinition();
			for (int i = 0; i < keys.Length; i++)
			{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(keys[i]))
					d.BoolButtons.Add(keys[i]);
				else
					d.FloatControls.Add(keys[i]);
			}
			controller = new Bk2ControllerAdapter() { Type = d };
		}

		#region "Custom Latch"

		public void LatchFromSourceButtons(Bk2ControllerAdapter latching, IController source)
		{
			foreach (string button in source.Type.BoolButtons)
			{
				latching[button] = source.IsPressed(button);
			}

			foreach (string name in source.Type.FloatControls)
			{
				latching.SetFloat(name, source.GetFloat(name));
			}
		}

		private void ORLatchFromSource(Bk2ControllerAdapter latching, IController source)
		{
			foreach (string button in latching.Type.BoolButtons)
			{
				latching[button] |= source.IsPressed(button);
			}

			foreach (string name in latching.Type.FloatControls)
			{
				float sFloat = source.GetFloat(name);
				int indexRange = source.Type.FloatControls.IndexOf(name);
				if (sFloat == source.Type.FloatRanges[indexRange].Mid)
					latching.SetFloat(name, sFloat);
			}
		}

		#endregion
	}
}
