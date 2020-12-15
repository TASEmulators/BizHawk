namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		private bool Handle_SIG(eMessage msg)
		{
			//I know, ive done this two completely different ways
			//both ways are sloppy glue, anyway
			//I havent decided on the final architecture yet

			switch (msg)
			{
				case eMessage.SIG_InputState:
					comm->value = (uint)core.CB_InputState(comm->port, comm->device, comm->index, comm->id);
					break;

				case eMessage.SIG_VideoUpdate:
					core.SIG_VideoUpdate();
					break;

				case eMessage.SIG_Sample:
					{
						short* samples = (short*)comm->buf[(int)BufId.Param0];
						core.retro_audio_sample(samples[0], samples[1]);
					}
					break;

				case eMessage.SIG_SampleBatch:
					{
						void* samples = (void*)comm->buf[(int)BufId.Param0];
						core.retro_audio_sample_batch(samples, (int)comm->buf_size[(int)BufId.Param0]/4);
					}
					break;

				default:
					return false;
			
			} //switch(msg)

			Message(eMessage.Resume);
			return true;
		}
	}
}