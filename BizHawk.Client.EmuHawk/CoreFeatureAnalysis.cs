using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class CoreFeatureAnalysis : Form, IToolFormAutoConfig
	{
		#region ConfigPersist

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
				Complete = Functions.All(f => f.Complete);
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

		[ConfigPersist]
		private Dictionary<string, CoreInfo> KnownCores { get; set; }

		#endregion

		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		[RequiredService]
		IEmulator Emulator { get; set; }

		public CoreFeatureAnalysis()
		{
			InitializeComponent();
			KnownCores = new Dictionary<string, CoreInfo>();
		}

		public void NewUpdate(ToolFormUpdateType type) { }

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


			var knownServices = Assembly.GetAssembly(typeof(IEmulator))
				.GetTypes()
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => t != typeof(IEmulatorService))
				.Where(t => t.IsInterface);

			var additionalServices = knownServices
				.Where(t => !ci.Services.ContainsKey(t.ToString()))
				.Where(t => !ci.NotApplicableTypes.Contains(t.ToString()))
				.Where(t => !typeof(ISpecializedEmulatorService).IsAssignableFrom(t)); // We don't want to show these as unimplemented, they aren't expected services

			foreach (Type service in additionalServices)
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

			var possibleCoreTypes =
				Assembly
				.Load("BizHawk.Emulation.Cores")
				.GetTypes()
				.Where(t => typeof(IEmulator).IsAssignableFrom(t) && !t.IsAbstract)
				.Select(t => new
				{
					Type = t,
					CoreAttributes = (CoreAttribute)t.GetCustomAttributes(typeof(CoreAttribute), false).First()
				})
				.OrderByDescending(t => t.CoreAttributes.Released)
				.ThenBy(t => t.CoreAttributes.CoreName)
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

			foreach (var t in possibleCoreTypes)
			{
				if (!KnownCores.ContainsKey(t.CoreAttributes.CoreName))
				{
					string img = "Unknown";
					var coreNode = new TreeNode
					{
						Text = t.CoreAttributes.CoreName + (t.CoreAttributes.Released ? "" : " (UNRELEASED)"),
						ForeColor = t.CoreAttributes.Released ? Color.Black : Color.DarkGray,
						ImageKey = img,
						SelectedImageKey = img,
						StateImageKey = img
					};
					CoreTree.Nodes.Add(coreNode);
				}
			}

			CoreTree.EndUpdate();
		}

		#region IToolForm

		public void UpdateValues()
		{
		}

		public void FastUpdate()
		{
		}

		public void Restart()
		{
			var ci = new CoreInfo(Emulator);
			KnownCores[ci.CoreName] = ci;

			DoCurrentCoreTree(ci);
			DoAllCoresTree(ci);
		}

		public bool AskSaveChanges() => true;

		public bool UpdateBefore => false;

		#endregion
	}
}
