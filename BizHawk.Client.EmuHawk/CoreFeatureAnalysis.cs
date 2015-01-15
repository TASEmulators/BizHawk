using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class CoreFeatureAnalysis : Form
	{
		public CoreFeatureAnalysis()
		{
			InitializeComponent();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void CoreFeatureAnalysis_Load(object sender, EventArgs e)
		{
			DoCurrentCoreTree();
			DoAllCoresTree();
		}

		private void DoCurrentCoreTree()
		{
			CurrentCoreTree.ImageList = new ImageList();
			CurrentCoreTree.ImageList.Images.Add("Good", Properties.Resources.GreenCheck);
			CurrentCoreTree.ImageList.Images.Add("Bad", Properties.Resources.ExclamationRed);

			var core = Global.Emulator;
			var services = Assembly
				.GetAssembly(typeof(IEmulator))
				.GetTypes()
				.Where(t => t.IsInterface)
				.Where(t => typeof(IEmulatorService).IsAssignableFrom(t))
				.Where(t => t != typeof(IEmulatorService))
				.ToList();

			var additionalRegisteredServices = core.ServiceProvider.AvailableServices
				.Where(s => !services.Contains(s)) 
				.Where(s => s != core.GetType()); // We don't care about the core itself

			services.AddRange(additionalRegisteredServices);

			CurrentCoreTree.Nodes.Clear();
			CurrentCoreTree.BeginUpdate();

			var coreNode = new TreeNode
			{
				Text = core.Attributes().CoreName + (core.Attributes().Released ? string.Empty : " (UNRELEASED)"),
				ForeColor = core.Attributes().Released ? Color.Black : Color.DarkGray,
			};


			coreNode.Expand();

			bool missingImplementation = false;

			foreach (var service in services)
			{
				bool isImplemented = false;
				if (core.ServiceProvider.HasService(service))
				{
					isImplemented = true;
				}
				else if (service.IsAssignableFrom(typeof(ISettable<,>))) // TODO
				{
					isImplemented = core.GetType()
						.GetInterfaces()
						.Where(t => t.IsGenericType &&
									t.GetGenericTypeDefinition() == typeof(ISettable<,>))
						.FirstOrDefault() != null;
				}

				var serviceNode = new TreeNode
				{
					Text = service.Name,
					ForeColor = isImplemented ? Color.Black : Color.Red
				};

				bool fullyImplementedInterface = isImplemented;

				if (isImplemented)
				{
					foreach (var field in service.GetMethods().OrderBy(f => f.Name))
					{
						try
						{
							var coreImplementation = core.ServiceProvider.GetService(service).GetType().GetMethod(field.Name);

							if (coreImplementation != null)
							{
								var i = coreImplementation.IsImplemented();
								serviceNode.Nodes.Add(new TreeNode
								{
									Text = field.Name,
									ImageKey = i ? "Good" : "Bad",
									SelectedImageKey = i ? "Good" : "Bad",
									StateImageKey = i ? "Good" : "Bad"
								});

								if (!i)
								{
									fullyImplementedInterface = false;
								}
							}
						}
						catch (Exception)
						{
							// TODO: SavestateBinary() and SaveStateBinary(BinaryWriter bw) cause an exception, need to look at signature too
						}
					}
				}
				else
				{
					missingImplementation = true;
				}

				serviceNode.StateImageKey = serviceNode.SelectedImageKey = serviceNode.ImageKey = fullyImplementedInterface ? "Good" : "Bad";

				coreNode.Nodes.Add(serviceNode);
			}

			coreNode.StateImageKey = coreNode.SelectedImageKey = coreNode.ImageKey = missingImplementation ? "Bad" : "Good";
			CurrentCoreTree.Nodes.Add(coreNode);

			CurrentCoreTree.EndUpdate();
		}

		private void DoAllCoresTree()
		{
			CoreTree.ImageList = new ImageList();
			CoreTree.ImageList.Images.Add("Good", Properties.Resources.GreenCheck);
			CoreTree.ImageList.Images.Add("Bad", Properties.Resources.ExclamationRed);

			var cores = Assembly
				.Load("BizHawk.Emulation.Cores")
				.GetTypes()
				.Where(t => typeof(IEmulator).IsAssignableFrom(t))
				.Select(core => new
				{
					CoreType = core,
					CoreAttributes = core.GetCustomAttributes(false)
										.OfType<CoreAttributes>()
										.Single(),
					ServicesNotApplicable = core.GetCustomAttributes(false)
										.OfType<ServiceNotApplicable>()
										.SingleOrDefault() ?? new ServiceNotApplicable()
				})
				.OrderBy(c => !c.CoreAttributes.Released)
				.ThenBy(c => c.CoreAttributes.CoreName)
				.ToList();

			TotalCoresLabel.Text = cores.Count.ToString();
			ReleasedCoresLabel.Text = cores.Count(c => c.CoreAttributes.Released).ToString();

			CoreTree.Nodes.Clear();
			CoreTree.BeginUpdate();

			foreach (var core in cores)
			{
				var coreNode = new TreeNode
				{
					Text = core.CoreAttributes.CoreName + (core.CoreAttributes.Released ? string.Empty : " (UNRELEASED)"),
					ForeColor = core.CoreAttributes.Released ? Color.Black : Color.DarkGray,
				};

				var service = typeof(IEmulator);

				bool isImplemented = false;
				if (service.IsAssignableFrom(core.CoreType))
				{
					isImplemented = true;
				}

				var serviceNode = new TreeNode
				{
					Text = service.Name,
					ForeColor = isImplemented ? Color.Black : Color.Red
				};

				serviceNode.Expand();

				bool fullyImplementedInterface = isImplemented;

				if (isImplemented)
				{
					foreach (var field in service.GetMethods().OrderBy(f => f.Name))
					{
						var coreImplementation = core.CoreType.GetMethod(field.Name);

						if (coreImplementation != null)
						{
							var i = coreImplementation.IsImplemented();
							serviceNode.Nodes.Add(new TreeNode
							{
								Text = field.Name,
								ImageKey = i ? "Good" : "Bad",
								SelectedImageKey = i ? "Good" : "Bad",
								StateImageKey = i ? "Good" : "Bad"
							});

							if (!i)
							{
								fullyImplementedInterface = false;
							}
						}
					}
				}

				serviceNode.StateImageKey = serviceNode.SelectedImageKey = serviceNode.ImageKey = fullyImplementedInterface ? "Good" : "Bad";

				coreNode.Nodes.Add(serviceNode);
				coreNode.StateImageKey = coreNode.SelectedImageKey = coreNode.ImageKey = fullyImplementedInterface ? "Good" : "Bad";

				CoreTree.Nodes.Add(coreNode);
			}

			CoreTree.EndUpdate();
		}
	}
}
