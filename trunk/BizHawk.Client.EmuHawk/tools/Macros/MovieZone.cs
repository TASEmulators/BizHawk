using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	class MovieZone
	{
		public string Name;

		public int Start;
		public int Length;
		public string InputKey;
		private string[] _log;

		public bool Replace = true;
		public bool Overlay = false;
		public float defaultFloat = 0f;

		private Bk2ControllerAdapter controller;
		private Bk2ControllerAdapter targetController;

		public MovieZone()
		{
		}
		public MovieZone(TasMovie movie, int start, int length, string key = "")
		{
			var lg = Global.MovieSession.LogGeneratorInstance() as Bk2LogEntryGenerator;
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			targetController = new Bk2ControllerAdapter();
			targetController.Type = Global.Emulator.ControllerDefinition;

			if (key == "")
				key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");
			key = key.Substring(0, key.Length - 1);

			InputKey = key;
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
			var logGenerator = new Bk2LogEntryGenerator("");
			logGenerator.SetSource(controller);
			for (int i = 0; i < length; i++)
			{
				// Set controller with only the buttons in it's Type.
				_log[i] = logGenerator.GenerateLogEntry();
			}
		}

		public void PlaceZone(TasMovie movie)
		{
			// TODO: This should probably do something with undo history batches/naming.
			if (!Replace)
				movie.InsertEmptyFrame(Start, Length);

			if (Overlay)
			{
				for (int i = 0; i < Length; i++)
				{
					// Overlay the frame. Float controls should use the defaultFloat value.
				}
			}
			else
			{
				for (int i = 0; i < Length; i++)
				{ 
					// Copy over the frame. (Only using the buttons in controller.Type)
				}
			}

			if (movie is TasMovie) // Assume TAStudio is open?
			{
				if (Global.Emulator.Frame < Start)
				{
					// TODO: Go to start of macro? Ask TAStudio to do that?

					// TasMovie.InvalidateAfter(Start) [this is private]
					// Load last state, Emulate to Start

					// Or do this, if we can accept that TAStudio has to be open.
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

			File.WriteAllText(fileName, Global.Emulator.ControllerDefinition.Name);
			File.AppendAllText(fileName, "\n" + Global.Emulator.ControllerDefinition.PlayerCount);
			File.AppendAllLines(fileName, _log);
		}
		public MovieZone(string fileName)
		{
			// If the LogKey contains buttons/controls not accepted by the emulator, tell the user and display the macro's controller name and player count.

			if (!File.Exists(fileName))
				return;

			_log = File.ReadAllLines(fileName);
			Length = _log.Length;
			Start = 0;

			Name = Path.GetFileNameWithoutExtension(fileName);
		}
	}
}
