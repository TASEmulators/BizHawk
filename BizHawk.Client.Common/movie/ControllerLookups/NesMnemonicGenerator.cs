using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class NesMnemonicGenerator : IMnemonicGeneratorCollection
	{
		public NesMnemonicGenerator()
		{

		}

		public IEnumerable<IMnemonicGenerator> Generators
		{
			get
			{
				return Enumerable.Empty<IMnemonicGenerator>();
			}
		}
	}
}
