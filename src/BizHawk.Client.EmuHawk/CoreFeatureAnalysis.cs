using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public partial class CoreFeatureAnalysis : ToolFormBase, IToolFormAutoConfig
	{
		private class CoreInfo
		{
			public string CoreName { get; set; }
			public bool Released { get; set; }
			public Dictionary<string, ServiceInfo> Services { get; set; }
			public List<string> NotApplicableTypes { get; set; }

			public CoreInfo() { }
			public CoreInfo(IEmulator emu)
			{
				CoreName = emu.Attributes().CoreName;
				Released = emu.Attributes().Released;
				Services = new Dictionary<string, ServiceInfo>();
				var ser = emu.ServiceProvider;
				foreach (Type t in ser.AvailableServices.Where(type => type != emu.GetType()))
				{
					var si = new ServiceInfo(t, ser.GetService(t));
					Services.Add(si.TypeName, si);
				}

				var notApplicableAttribute = ((ServiceNotApplicableAttribute)Attribute
					.GetCustomAttribute(emu.GetType(), typeof(ServiceNotApplicableAttribute)));

				if (notApplicableAttribute != null)
				{
					NotApplicableTypes = notApplicableAttribute.NotApplicableTypes
					.Select(x => x.ToString())
					.ToList();
				}
				else
				{
					NotApplicableTypes = new List<string>();
				}
			}
		}

		private class ServiceInfo
		{
			public string TypeName { get; set; }
			public bool Complete { get; set; }
			public List<FunctionInfo> Functions { get; set; }

			public ServiceInfo() { }
			public ServiceInfo(Type serviceType, object service)
			{
				TypeName = serviceType.IsGenericType
					? serviceType.GetGenericTypeDefinition().ToString()
					: serviceType.ToString();

				Functions = new List<FunctionInfo>();

				IEnumerable<MethodInfo> methods = serviceType.GetMethods();

				if (serviceType.IsInterface)
				{
					var map = service.GetType().GetInterfaceMap(serviceType);
					// project interface methods to actual implementations
					methods = methods.Select(
						m => map.TargetMethods[Array.IndexOf(map.InterfaceMethods, m)]);
				}

				foreach (var method in methods)
				{
					Functions.Add(new FunctionInfo(method, service));
				}
				Complete = Functions.TrueForAll(static f => f.Complete);
			}
		}

		private class FunctionInfo
		{
			public string TypeName { get; set; }
			public bool Complete { get; set; }

			public FunctionInfo() { }
			public FunctionInfo(MethodInfo m, object service)
			{
				TypeName = m.ToString();
				try
				{
					Complete = m.IsImplemented();
				}
				catch
				{
					Complete = false; // TODO: fixme
				}
			}
		}

		public static Icon ToolIcon
			=> Properties.Resources.Logo;

		[ConfigPersist]
		private Dictionary<string, CoreInfo> KnownCores { get; set; }

		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		[RequiredService]
		private IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => "Core Features";

		public CoreFeatureAnalysis()
		{
			InitializeComponent();
			Icon = ToolIcon;
			KnownCores = new Dictionary<string, CoreInfo>();
		}

		private TreeNode CreateCoreTree(CoreInfo ci)
		{
			var ret = new TreeNode
			{
				Text = ci.CoreName + (ci.Released ? "" : " (UNRELEASED)"),
				ForeColor = ci.Released ? Color.Black : Color.DarkGray
			};

			foreach (var service in ci.Services.Values)
			{
				string img = service.Complete ? "Good" : "Bad";
				var serviceNode = new TreeNode
				{
					Text = service.TypeName,
					ForeColor = service.Complete ? Color.Black : Color.Red,
					ImageKey = img,
					SelectedImageKey = img,
					StateImageKey = img
				};

				foreach (var function in service.Functions)
				{
					img = function.Complete ? "Good" : "Bad";
					serviceNode.Nodes.Add(new TreeNode
					{
						Text = function.TypeName,
						ForeColor = function.Complete ? Color.Black : Color.Red,
						ImageKey = img,
						SelectedImageKey = img,
						StateImageKey = img
					});
				}
				ret.Nodes.Add(serviceNode);
			}

			foreach (var service in Emulation.Common.ReflectionCache.Types.Where(t => t.IsInterface
				&& typeof(IEmulatorService).IsAssignableFrom(t) && !typeof(ISpecializedEmulatorService).IsAssignableFrom(t) // don't show ISpecializedEmulatorService subinterfaces as "missing" as there's no expectation that they'll be implemented eventually
				&& t != typeof(IEmulatorService) && t != typeof(ITextStatable) // denylisting ITextStatable is a hack for now, eventually we can get merge it into IStatable w/ default interface methods
				&& !ci.Services.ContainsKey(t.ToString()) && !ci.NotApplicableTypes.Contains(t.ToString())))
			{
				string img = "Bad";
				var serviceNode = new TreeNode
				{
					Text = service.ToString(),
					ForeColor = Color.Red,
					ImageKey = img,
					SelectedImageKey = img,
					StateImageKey = img
				};
				ret.Nodes.Add(serviceNode);
			}
			return ret;
		}

		private void DoCurrentCoreTree(CoreInfo ci)
		{
			CurrentCoreTree.ImageList = new ImageList();
			CurrentCoreTree.ImageList.Images.Add("Good", Properties.Resources.GreenCheck);
			CurrentCoreTree.ImageList.Images.Add("Bad", Properties.Resources.ExclamationRed);

			CurrentCoreTree.Nodes.Clear();
			CurrentCoreTree.BeginUpdate();
			var coreNode = CreateCoreTree(ci);
			coreNode.Expand();
			CurrentCoreTree.Nodes.Add(coreNode);
			CurrentCoreTree.EndUpdate();
		}

		private void DoAllCoresTree(CoreInfo current_ci)
		{
			CoreTree.ImageList = new ImageList();
			CoreTree.ImageList.Images.Add("Good", Properties.Resources.GreenCheck);
			CoreTree.ImageList.Images.Add("Bad", Properties.Resources.ExclamationRed);
			CoreTree.ImageList.Images.Add("Unknown", Properties.Resources.RetroQuestion);

			var possibleCoreTypes = CoreInventory.Instance.SystemsFlat
				.OrderByDescending(core => core.CoreAttr.Released)
				.ThenBy(core => core.Name)
				.ToList();

			toolStripStatusLabel1.Text = $"Total: {possibleCoreTypes.Count} Released: {KnownCores.Values.Count(c => c.Released)} Profiled: {KnownCores.Count}";

			CoreTree.Nodes.Clear();
			CoreTree.BeginUpdate();

			foreach (var ci in KnownCores.Values)
			{
				var coreNode = CreateCoreTree(ci);

				if (ci.CoreName == current_ci.CoreName)
				{
					coreNode.Expand();
				}
				CoreTree.Nodes.Add(coreNode);
			}

			foreach (var core in possibleCoreTypes)
			{
				if (!KnownCores.ContainsKey(core.Name))
				{
					string img = "Unknown";
					var coreNode = new TreeNode
					{
						Text = core.Name + (core.CoreAttr.Released ? "" : " (UNRELEASED)"),
						ForeColor = core.CoreAttr.Released ? Color.Black : Color.DarkGray,
						ImageKey = img,
						SelectedImageKey = img,
						StateImageKey = img
					};
					CoreTree.Nodes.Add(coreNode);
				}
			}

			CoreTree.EndUpdate();
		}

		public override void Restart()
		{
			var ci = new CoreInfo(Emulator);
			KnownCores[ci.CoreName] = ci;

			DoCurrentCoreTree(ci);
			DoAllCoresTree(ci);
		}
	}
}
