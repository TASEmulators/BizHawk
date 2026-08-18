namespace BizHawk.Emulation.Cores.Tapes
{
	/// <summary>
	/// Represents the possible commands that can be raised from each tape block
	/// </summary>
	public enum TapeCommand
	{
		NONE,
		STOP_THE_TAPE,
		STOP_THE_TAPE_48K,
		BEGIN_GROUP,
		END_GROUP,
		SHOW_MESSAGE,

		// control-flow markers, consumed while a converter expands the tape into its final linear block list
		// (they do not appear in the played list, so the tape player never has to interpret them)
		JUMP,
		LOOP_START,
		LOOP_END,
		CALL_SEQUENCE,
		RETURN_FROM_SEQUENCE,
		SELECT_BLOCK,
	}
}
