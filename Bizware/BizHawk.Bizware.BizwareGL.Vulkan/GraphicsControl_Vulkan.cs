using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	class GLControlWrapper_Vulkan : GLControl, IGraphicsControl
	{
		//Note: In order to work around bugs in OpenTK which sometimes do things to a context without making that context active first...
		//we are going to push and pop the context before doing stuff

		public GLControlWrapper_Vulkan(IGL_Vulkan owner)
			: base(GraphicsMode.Default, 2, 0, GraphicsContextFlags.Default)
		{
			Owner = owner;
			GLControl = this;
		}

		global::OpenTK.GLControl GLControl;
		IGL_Vulkan Owner;

		public Control Control { get { return this; } }


		public void SetVsync(bool state)
		{
			//IGraphicsContext curr = global::OpenTK.Graphics.GraphicsContext.CurrentContext;
			GLControl.MakeCurrent();
			GLControl.VSync = state;
			//Owner.MakeContextCurrent(curr, Owner.NativeWindowsForContexts[curr]);
		}

		public void Begin()
		{
			if (!GLControl.Context.IsCurrent)
				Owner.MakeContextCurrent(GLControl.Context, GLControl.WindowInfo);
		}

		public void End()
		{
			Owner.MakeDefaultCurrent();
		}

		public new void SwapBuffers()
		{
			if (!GLControl.Context.IsCurrent)
				MakeCurrent();
			base.SwapBuffers();
		}
	}
}
