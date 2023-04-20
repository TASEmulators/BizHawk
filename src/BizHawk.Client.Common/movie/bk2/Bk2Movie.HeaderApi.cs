using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		private string _syncSettingsJson = "";

		public string SyncSettingsJson
		{
			get => _syncSettingsJson;
			set
			{
				if (_syncSettingsJson != value)
				{
					Changes = true;
					_syncSettingsJson = value;
				}
			}
		}

		public override ulong Rerecords
		{
			set
			{
				if (Header[HeaderKeys.Rerecords] != value.ToString())
				{
					Changes = true;
					Header[HeaderKeys.Rerecords] = value.ToString();
				}
			}
		}

		public virtual bool StartsFromSavestate
		{
			// ReSharper disable SimplifyConditionalTernaryExpression
			get => Header.TryGetValue(HeaderKeys.StartsFromSavestate, out var s) ? bool.Parse(s) : false;
			// ReSharper restore SimplifyConditionalTernaryExpression
			set
			{
				if (value)
				{
					Header[HeaderKeys.StartsFromSavestate] = "True";
				}
				else
				{
					Header.Remove(HeaderKeys.StartsFromSavestate);
				}
			}
		}

		public bool StartsFromSaveRam
		{
			// ReSharper disable SimplifyConditionalTernaryExpression
			get => Header.TryGetValue(HeaderKeys.StartsFromSaveram, out var s) ? bool.Parse(s) : false;
			// ReSharper restore SimplifyConditionalTernaryExpression
			set
			{
				if (value)
				{
					if (!Header.ContainsKey(HeaderKeys.StartsFromSaveram))
					{
						Header.Add(HeaderKeys.StartsFromSaveram, "True");
					}
				}
				else
				{
					Header.Remove(HeaderKeys.StartsFromSaveram);
				}
			}
		}

		public override string GameName
		{
			set
			{
				if (Header[HeaderKeys.GameName] != value)
				{
					Changes = true;
					Header[HeaderKeys.GameName] = value;
				}
			}
		}

		public override string SystemID
		{
			set
			{
				if (Header[HeaderKeys.Platform] != value)
				{
					Changes = true;
					Header[HeaderKeys.Platform] = value;
				}
			}
		}

		public override string Hash
		{
			set
			{
				if (Header[HeaderKeys.Sha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.Sha1] = value;
				}
			}
		}

		public override string Author
		{
			set
			{
				if (Header[HeaderKeys.Author] != value)
				{
					Changes = true;
					Header[HeaderKeys.Author] = value;
				}
			}
		}

		public override string Core
		{
			set
			{
				if (Header[HeaderKeys.Core] != value)
				{
					Changes = true;
					Header[HeaderKeys.Core] = value;
				}
			}
		}

		public override string BoardName
		{
			set
			{
				if (Header[HeaderKeys.BoardName] != value)
				{
					Changes = true;
					Header[HeaderKeys.BoardName] = value;
				}
			}
		}

		public override string EmulatorVersion
		{
			set
			{
				if (Header[HeaderKeys.EmulatorVersion] != value)
				{
					Changes = true;
					Header[HeaderKeys.EmulatorVersion] = value;
				}
			}
		}

		public override string OriginalEmulatorVersion
		{
			set
			{
				if (Header[HeaderKeys.OriginalEmulatorVersion] != value)
				{
					Changes = true;
					Header[HeaderKeys.OriginalEmulatorVersion] = value;
				}
			}
		}

		public override string FirmwareHash
		{
			set
			{
				if (Header[HeaderKeys.FirmwareSha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.FirmwareSha1] = value;
				}
			}
		}

		protected string CommentsString()
		{
			var sb = new StringBuilder();

			foreach (var comment in Comments)
			{
				sb.AppendLine(comment);
			}

			return sb.ToString();
		}

		public string TextSavestate { get; set; }
		public byte[] BinarySavestate { get; set; }
		public int[] SavestateFramebuffer { get; set; }
		public byte[] SaveRam { get; set; }
	}
}
