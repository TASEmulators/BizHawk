using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
		public static IMovie Load(string path)
		{
			// TODO: open the file and determine the format, and instantiate the appropriate implementation
			// Currently we just assume it is a bkm implementation
			return new Movie(path);
		}
	}
}
