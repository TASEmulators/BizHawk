//https://github.com/Themaister/RetroArch/wiki/GLSL-shaders
//https://github.com/Themaister/Emulator-Shader-Pack/blob/master/Cg/README

using System;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;


//Here, I started making code to support GUI editing of filter chains.
//I decided to go for broke and implement retroarch's system first, and then the GUI editing should be able to internally produce a metashader

//namespace BizHawk.Client.EmuHawk
//{
//  class FilterManager
//  {
//    class PipelineState
//    {
//      public PipelineState(PipelineState other)
//      {
//        Size = other.Size;
//        Format = other.Format;
//      }
//      public Size Size;
//      public string Format;
//    }

//    abstract class BaseFilter
//    {
//      bool Connect(FilterChain chain, BaseFilter parent)
//      {
//        Chain = chain;
//        Parent = parent;
//        return OnConnect();
//      }

//      public PipelineState OutputState;
//      public FilterChain Chain;
//      public BaseFilter Parent;

//      public abstract bool OnConnect();
//    }

//    class FilterChain
//    {
//      public void AddFilter(BaseFilter filter)
//      {
//      }
//    }

//    class Filter_Grayscale : BaseFilter
//    {
//      public override bool OnConnect()
//      {
//        if(Parent.OutputState.Format != "rgb") return false;
//        OutputState = new PipelineState { Parent.OutputState; }
//      }
//    }

//    class Filter_EmuOutput_RGBA : BaseFilter
//    {
//      public Filter_EmuOutput_RGBA(int width, int height)
//      {
//        OutputState = new PipelineState() { Size = new Size(width, height), Format = "rgb" };
//      }

//      public override bool OnConnect()
//      {
//        return true;
//      }
//    }

//  }
//}