using Qt.Quick;

namespace BizHawk.Client.QtTest;

public static class Program
{
	public static void Main(string[] args)
	{
		Qml.LoadFromRootModule("Main");
		Qml.WaitForExit();
	}
}
