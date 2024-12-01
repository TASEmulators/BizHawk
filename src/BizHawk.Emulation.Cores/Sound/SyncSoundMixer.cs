using BizHawk.Emulation.Common;

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Components
{
	/// <summary>
	/// ISoundProvider mixer that generates a single ISoundProvider output from multiple ISoundProvider sources
	/// Currently only supports sync (not async)
	///
	/// Bizhawk expects ISoundProviders to output at 44100KHz, so this is what SyncSoundMixer does. Therefore, try to make
	/// sure that your child ISoundProviders also do this I guess.
	/// 
	/// This is currently used in the ZX Spectrum and CPC cores but others may find it useful in future
	/// </summary>
	public sealed class SyncSoundMixer : ISoundProvider
	{
		/// <summary>
		/// Currently attached ChildProviders
		/// </summary>
		private readonly List<ChildProvider> _soundProviders = new List<ChildProvider>();

		/// <summary>
		/// The final output max volume
		/// </summary>
		public short FinalMaxVolume
		{
			get => _finalMaxVolume;
			set
			{
				_finalMaxVolume = value;
				EqualizeVolumes();
			}
		}
		private short _finalMaxVolume;

		/// <summary>
		/// How the sound sources are balanced against each other
		/// </summary>
		public SoundMixBalance MixBalanceMethod
		{
			get => _mixBalanceMethod;
			set
			{
				_mixBalanceMethod = value;
				EqualizeVolumes();
			}
		}
		private SoundMixBalance _mixBalanceMethod;

		/// <summary>
		/// If specified the output buffer of the SyncSoundMixer will always contain this many samples
		/// You should probably nearly always specify a value for this and get your ISoundProvider sources
		/// to get as close to this nsamp value as possible. Otherwise the number of samples will
		/// be based on the highest nsamp out of all the child providers for that specific frame
		/// Useful examples:
		///		882 - 44100KHz - 50Hz
		///		735 - 44100Khz - 60Hz
		/// </summary>
		private readonly int? _targetSampleCount;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mixBalanceMethod">Whether each providers MaxVolume is reduced to an equal share of the final max volume value</param>
		/// <param name="maxVolume">The final 'master' max volume</param>
		/// <param name="targetSampleCount">
		/// If specified the output buffer of the SyncSoundMixer will always contain this many samples
		/// If left null the output buffer will contain the highest number of samples out of each of the providers every frame
		/// </param>
		public SyncSoundMixer(SoundMixBalance mixBalanceMethod = SoundMixBalance.Equalize, short maxVolume = short.MaxValue, int? targetSampleCount = null)
		{
			_mixBalanceMethod = mixBalanceMethod;
			_finalMaxVolume = maxVolume;
			_targetSampleCount = targetSampleCount;
		}

		/// <summary>
		/// Adds an ISoundProvider to the SyncSoundMixer
		/// </summary>
		/// <param name="source">The source ISoundProvider</param>
		/// <param name="sourceDescription">An ident string for the ISoundProvider (useful when debugging)</param>
		public void PinSource(ISoundProvider source, string sourceDescription)
		{
			PinSource(source, sourceDescription, FinalMaxVolume);
		}

		/// <summary>
		/// Adds an ISoundProvider to the SyncSoundMixer
		/// </summary>
		/// <param name="source">The source ISoundProvider</param>
		/// <param name="sourceDescription">An ident string for the ISoundProvider (useful when debugging)</param>
		/// <param name="sourceMaxVolume">The MaxVolume level for this particular ISoundProvider</param>
		public void PinSource(ISoundProvider source, string sourceDescription, short sourceMaxVolume)
		{
			_soundProviders.Add(new ChildProvider
			{
				SoundProvider = source,
				ProviderDescription = sourceDescription,
				MaxVolume = sourceMaxVolume
			});

			EqualizeVolumes();
		}

		/// <summary>
		/// Removes an existing ISoundProvider from the SyncSoundMixer
		/// </summary>
		public void UnPinSource(ISoundProvider source)
		{
			_soundProviders.RemoveAll(a => a.SoundProvider == source);
			EqualizeVolumes();
		}

		/// <summary>
		/// Sets each pinned sound provider's MaxVolume based on the MixBalanceMethod
		/// </summary>
		public void EqualizeVolumes()
		{
			if (_soundProviders.Count < 1)
				return;

			switch (MixBalanceMethod)
			{
				case SoundMixBalance.Equalize:
					var eachVolume = FinalMaxVolume / _soundProviders.Count;
					foreach (var source in _soundProviders)
					{
						source.MaxVolume = eachVolume;
					}
					break;
				case SoundMixBalance.MasterHardLimit:
					foreach (var source in _soundProviders)
					{
						if (source.MaxVolume > FinalMaxVolume)
						{
							source.MaxVolume = FinalMaxVolume;
						}
					}
					break;
			}
		}

		/// <summary>
		/// Returns the value of the highest nsamp in the SoundProviders collection
		/// </summary>
		private int GetHigestSampleCount()
		{
			var lookup = _soundProviders.OrderByDescending(x => x.InputNSamp)
				.FirstOrDefault();

			if (lookup == null)
			{
				return 0;
			}

			return lookup.InputNSamp;
		}

		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new InvalidOperationException("Only Sync mode is supported.");
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			foreach (var soundSource in _soundProviders)
			{
				soundSource.SoundProvider.DiscardSamples();
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			// fetch samples from all the providers
			foreach (var sp in _soundProviders)
			{
				sp.GetSamples();
			}

			nsamp = _targetSampleCount ?? GetHigestSampleCount();
			samples = new short[nsamp * 2];

			// process the output buffers
			foreach (var sp in _soundProviders)
			{
				sp.PrepareOutput(nsamp);
			}

			// mix the child providers together
			for (int i = 0; i < samples.Length; i++)
			{
				int sampleVal = 0;
				foreach (var sp in _soundProviders)
				{
					if (sp.OutputBuffer[i] > sp.MaxVolume)
					{
						sampleVal += (short)sp.MaxVolume;
					}
					else
					{
						sampleVal += sp.OutputBuffer[i];
					}
				}

				// final hard limit
				if (sampleVal > FinalMaxVolume)
				{
					sampleVal = FinalMaxVolume;
				}

				samples[i] = (short)sampleVal;
			}
		}

		/// <summary>
		/// Instantiated for every ISoundProvider source that is added to the mixer
		/// </summary>
		private class ChildProvider
		{
			/// <summary>
			/// The Child ISoundProvider
			/// </summary>
			public ISoundProvider SoundProvider;

			/// <summary>
			/// Identification string
			/// </summary>
			public string ProviderDescription;

			/// <summary>
			/// The max volume for this provider
			/// </summary>
			public int MaxVolume;

			/// <summary>
			/// Stores the incoming samples
			/// </summary>
			public short[] InputBuffer;

			/// <summary>
			/// The incoming number of samples
			/// </summary>
			public int InputNSamp;

			/// <summary>
			/// Stores the processed samples ready for mixing
			/// </summary>
			public short[] OutputBuffer;

			/// <summary>
			/// The output number of samples
			/// </summary>
			public int OutputNSamp;

			/// <summary>
			/// Fetches sample data from the child ISoundProvider
			/// </summary>
			public void GetSamples()
			{
				SoundProvider.GetSamplesSync(out InputBuffer, out InputNSamp);
			}

			/// <summary>
			/// Ensures the output buffer is ready for mixing based on the supplied nsamp value
			/// Overflow samples will be omitted and underflow samples will be empty air
			/// </summary>
			public void PrepareOutput(int nsamp)
			{
				OutputNSamp = nsamp;
				var outputBuffSize = OutputNSamp * 2;

				if (OutputNSamp != InputNSamp || InputBuffer.Length != outputBuffSize)
				{
					OutputBuffer = new short[outputBuffSize];

					var i = 0;
					while (i < InputBuffer.Length && i < outputBuffSize)
					{
						OutputBuffer[i] = InputBuffer[i];
						i++;
					}
				}
				else
				{
					// buffer needs no modification
					OutputBuffer = InputBuffer;
				}
			}
		}
	}

	/// <summary>
	/// Defines how mixed sound sources should be balanced
	/// </summary>
	public enum SoundMixBalance
	{
		/// <summary>
		/// Each sound source's max volume will be set to MaxVolume / nSources
		/// </summary>
		Equalize,
		/// <summary>
		/// Each sound source's individual max volume will be respected but the final MaxVolume will be limited to MaxVolume
		/// </summary>
		MasterHardLimit
	}
}
