using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

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
			CoreTree.ImageList = new ImageList();
			CoreTree.ImageList.Images.Add("Good", Properties.Resources.GreenCheck);
			CoreTree.ImageList.Images.Add("Bad", Properties.Resources.ExclamationRed);


			var services = Assembly
				.GetAssembly(typeof(IEmulator))
				.GetTypes()
				.Where(t => t.IsInterface)
				.Where(t => typeof(ICoreService).IsAssignableFrom(t))
				.Where(t => t != typeof(ICoreService))
				.ToList();

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

				bool missingImplementation = false;

				foreach (var service in services.Where(s => !core.ServicesNotApplicable.NotApplicableTypes.Contains(s)))
				{
					bool isImplemented = false;
					if (service.IsAssignableFrom(core.CoreType))
					{
						isImplemented = true;
					}
					else if (service.IsAssignableFrom(typeof(ISettable<,>)))
					{
						isImplemented = core.CoreType
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
							catch (Exception)
							{
								// TODO: SavestateBinary() and SaveStateBinary(BinaryWriter bw) cause an exception, need to look at signature too
							}
						}

						// Properties are redundant the getter and setters show up when iterating methods
						/*
						foreach (var field in service.GetProperties().OrderBy(f => f.Name))
						{
							var i = field.IsImplemented();
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
						*/
					}
					else
					{
						missingImplementation = true;
					}

					serviceNode.StateImageKey = serviceNode.SelectedImageKey = serviceNode.ImageKey = fullyImplementedInterface ? "Good" : "Bad";

					coreNode.Nodes.Add(serviceNode);
				}

				coreNode.StateImageKey = coreNode.SelectedImageKey = coreNode.ImageKey = missingImplementation ? "Bad" : "Good";

				CoreTree.Nodes.Add(coreNode);
			}

			CoreTree.EndUpdate();
		}
	}
}
