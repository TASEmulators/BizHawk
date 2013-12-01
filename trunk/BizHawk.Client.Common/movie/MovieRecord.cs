using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	class MovieRecord : IMovieRecord
	{
		private List<byte> _state = new List<byte>();

		public string Input { get; private set; }
		public bool Lagged { get; private set; }
		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public MovieRecord()
		{

		}
	}
}
