using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Represents a TasStudio Marker
	/// A marker is a tagged frame with a message
	/// </summary>
	public class Marker
	{
		private int _frame;

		public Marker(int frame, string message = "")
		{
			_frame = frame;
			Message = message;
		}

		public virtual int Frame 
		{
			get { return _frame; }
		}

		public virtual string Message { get; set; }

		public override string ToString()
		{
			return Frame.ToString() + '\t' + Message;
		}

		public override int GetHashCode()
		{
			return this.Frame.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			else if (obj is Marker)
			{
				return this.Frame == (obj as Marker).Frame;
			}
			else
			{
				return false;
			}
		}

		public static bool operator ==(Marker a, Marker b)
		{
			return a.Frame == b.Frame;
		}

		public static bool operator !=(Marker a, Marker b)
		{
			return a.Frame != b.Frame;
		}

		public static bool operator ==(Marker marker, int frame)
		{
			return marker.Frame == frame;
		}

		public static bool operator !=(Marker marker, int frame)
		{
			return marker.Frame != frame;
		}
	}

	/// <summary>
	/// Specialized Marker that represents the currently emulated frame
	/// Frame always points to Global.Emulator.Frame, and settings it isn't possible
	/// </summary>
	public class CurrentFrameMarker : Marker
	{
		public CurrentFrameMarker()
			: base(0)
		{

		}

		public override int Frame
		{
			get { return Global.Emulator.Frame; }
		}

		public virtual string Message
		{
			get { return String.Empty; }
			set { return; }
		}
	}

	public class MarkerList : List<Marker>
	{
		private readonly CurrentFrameMarker _current;
		public MarkerList()
			: base()
		{
			_current = new CurrentFrameMarker();
		}

		public CurrentFrameMarker CurrentFrame
		{
			get
			{
				return _current;
			}
		}
	}
}
