namespace NLua.Method
{
	public class LuaEventHandler
	{
		public LuaFunction Handler = null;

		public void HandleEvent(object[] args)
		{
			Handler.Call(args);
		}
	}
}