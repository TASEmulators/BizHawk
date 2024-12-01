using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	public interface IPipeline : IDisposable
	{
		/// <summary>
		/// Sets vertex data for this pipeline
		/// </summary>
		void SetVertexData(IntPtr data, int count);

		/// <summary>
		/// Sets index data for this pipeline
		/// </summary>
		void SetIndexData(IntPtr data, int count);

		bool HasUniformSampler(string name);

		string GetUniformSamplerName(int index);

		/// <summary>
		/// Sets a uniform sampler to use use the provided texture
		/// </summary>
		void SetUniformSampler(string name, ITexture2D tex);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniformMatrix(string name, Matrix4x4 mat, bool transpose = false);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniformMatrix(string name, ref Matrix4x4 mat, bool transpose = false);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniform(string name, Vector4 value);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniform(string name, Vector2 value);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniform(string name, float value);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetUniform(string name, bool value);
	}
}
