using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class AndAdapter : IController
	{
		public bool IsPressed(string button)
		{
			return this[button];
		}

		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name) { return Source.GetFloat(name); }

		public IController Source { get; set; }
		public IController SourceAnd { get; set; }
		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }

		public bool this[string button]
		{
			get
			{
				if (Source != null && SourceAnd != null)
				{
					return Source[button] & SourceAnd[button];
				}

				return false;
			}

			set
			{
				throw new InvalidOperationException();
			}
		}
	}

	public class ORAdapter : IController
	{
		public bool IsPressed(string button)
		{
			return this[button];
		}

		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name) { return Source.GetFloat(name); }

		public IController Source { get; set; }
		public IController SourceOr { get; set; }
		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }

		public bool this[string button]
		{
			get
			{
				return (Source != null ? Source[button] : false) |
					(SourceOr != null ? SourceOr[button] : false);
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

	}
}
