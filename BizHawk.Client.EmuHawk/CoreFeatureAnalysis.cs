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
										.Single()
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
					ForeColor = core.CoreAttributes.Released ? Color.Black : Color.DarkGray
				};

				foreach (var service in services)
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

					if (isImplemented)
					{
						foreach (var field in service.GetProperties())
						{
							serviceNode.Nodes.Add(new TreeNode
							{
								Text = field.Name
							});
						}

						foreach (var field in service.GetMethods())
						{
							serviceNode.Nodes.Add(new TreeNode
							{
								Text = field.Name
							});
						}
					}

					coreNode.Nodes.Add(serviceNode);
				}


				CoreTree.Nodes.Add(coreNode);
			}

			CoreTree.EndUpdate();
		}
	}
}
