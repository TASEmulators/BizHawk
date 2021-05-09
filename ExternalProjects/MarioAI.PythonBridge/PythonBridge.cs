using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioAI
{
    public class PythonBridge
    {
		public int DoStuff()
		{
			using (Py.GIL())
			{
				using (PyScope scope = Py.CreateScope())
				{
					scope.Set("a", 3);
					scope.Set("b", 4);

					dynamic result = scope.Eval("a + b");

					return result;
				}
			}
		}
	}
}
