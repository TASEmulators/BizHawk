using System.Linq;
using System.Reflection;

// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	public class BinaryStateLump
	{
		[Name("BizState 1", "0")]
		public static BinaryStateLump ZipVersion { get; private set; }
		[Name("BizVersion", "txt")]
		public static BinaryStateLump BizVersion { get; private set; }
		[Name("Core", "bin")]
		public static BinaryStateLump Corestate { get; private set; }
		[Name("Framebuffer", "bmp")]
		public static BinaryStateLump Framebuffer { get; private set; }
		[Name("Input Log", "txt")]
		public static BinaryStateLump Input { get; private set; }
		[Name("CoreText", "txt")]
		public static BinaryStateLump CorestateText { get; private set; }
		[Name("MovieSaveRam", "bin")]
		public static BinaryStateLump MovieSaveRam { get; private set; }

		// Only for movies they probably shouldn't be leaching this stuff
		[Name("Header", "txt")]
		public static BinaryStateLump Movieheader { get; private set; }
		[Name("Comments", "txt")]
		public static BinaryStateLump Comments { get; private set; }
		[Name("Subtitles", "txt")]
		public static BinaryStateLump Subtitles { get; private set; }
		[Name("SyncSettings", "json")]
		public static BinaryStateLump SyncSettings { get; private set; }

		// TasMovie
		[Name("LagLog")]
		public static BinaryStateLump LagLog { get; private set; }
		[Name("GreenZone")]
		public static BinaryStateLump StateHistory { get; private set; }
		[Name("GreenZoneSettings", "txt")]
		public static BinaryStateLump StateHistorySettings { get; private set; }
		[Name("Markers", "txt")]
		public static BinaryStateLump Markers { get; private set; }
		[Name("ClientSettings", "json")]
		public static BinaryStateLump ClientSettings { get; private set; }
		[Name("VerificationLog", "txt")]
		public static BinaryStateLump VerificationLog { get; private set; }
		[Name("UserData", "txt")]
		public static BinaryStateLump UserData { get; private set; }
		[Name("Session", "txt")]
		public static BinaryStateLump Session { get; private set; }

		// branch stuff
		[Name("Branches/CoreData", "bin")]
		public static BinaryStateLump BranchCoreData { get; private set; }
		[Name("Branches/InputLog", "txt")]
		public static BinaryStateLump BranchInputLog { get; private set; }
		[Name("Branches/FrameBuffer", "bmp")]
		public static BinaryStateLump BranchFrameBuffer { get; private set; }
		[Name("Branches/CoreFrameBuffer", "bmp")]
		public static BinaryStateLump BranchCoreFrameBuffer { get; private set; }
		[Name("Branches/Header", "json")]
		public static BinaryStateLump BranchHeader { get; private set; }
		[Name("Branches/Markers", "txt")]
		public static BinaryStateLump BranchMarkers { get; private set; }
		[Name("Branches/UserText", "txt")]
		public static BinaryStateLump BranchUserText { get; private set; }

		[AttributeUsage(AttributeTargets.Property)]
		private sealed class NameAttribute : Attribute
		{
			public string Name { get; }
			public string Ext { get; }
			public NameAttribute(string name)
			{
				Name = name;
			}

			public NameAttribute(string name, string ext)
			{
				Name = name;
				Ext = ext;
			}
		}

		public string ReadName => Name;
		public string WriteName => Ext != null ? Name + '.' + Ext : Name;

		public string Name { get; protected set; }
		public string Ext { get; protected set; }

		private BinaryStateLump(string name, string ext)
		{
			Name = name;
			Ext = ext;
		}

		protected BinaryStateLump()
		{
		}

		static BinaryStateLump()
		{
			foreach (var prop in typeof(BinaryStateLump).GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				var attr = prop.GetCustomAttributes(false).OfType<NameAttribute>().Single();
				object value = new BinaryStateLump(attr.Name, attr.Ext);
				prop.SetValue(null, value, null);
			}
		}
	}
}
