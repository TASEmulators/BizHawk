using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class TasMovie : Bk2Movie
	{
		public TasMovie(string path)
			: base(path)
		{

		}

		public TasMovie()
			: base()
		{
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0"; 
		}

		public override string PreferredExtension
		{
			get
			{
				return Extension;
			}
		}

		public new const string Extension = "tasproj";

		public MovieRecord this[int index]
		{
			get
			{
				// TODO
				return new MovieRecord("", false);
			}
		}

		public void SetBoolButton(int index, string button, bool value)
		{
			// TODO
		}

		public Dictionary<string, string> ColumnNames
		{
			get
			{
				// TODO
				return new Dictionary<string, string>
				{
					{ "A", "A" },
					{ "B", "B" }
				};
			}
		}
	}
}
