using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	// adapter for converting CIA output to VIA
	// inherits DataPortConnector so the conversion is invisible
	// to both devices

	struct SerialCableData
	{
		public bool ATNIN;
		public bool ATNOUT;
		public bool CLOCKIN;
		public bool CLOCKOUT;
		public bool DATAIN;
		public bool DATAOUT;
	}

	class SerialCable : DataPortConnector
	{
		private DataPortConnector connector;

		public SerialCable(DataPortConnector baseConnector) : base(baseConnector)
		{
			connector = baseConnector;
		}

		new public byte Data
		{
			get
			{
				return base.Data;
			}
			set
			{
				base.Data = value;
			}
		}

		new public byte Direction
		{
			get
			{
				return base.Direction;
			}
			set
			{
				base.Direction = value;
			}
		}

		new public byte RemoteData
		{
			get
			{
				return base.RemoteData;
			}
		}
	}

}
