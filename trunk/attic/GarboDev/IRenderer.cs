namespace GarboDev
{
    using System;

    public interface IRenderer
    {
        Memory Memory { set; }
        void Initialize(object data);
        void Reset();
        void RenderLine(int line);
        uint[] ShowFrame();
    }
}
