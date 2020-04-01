using System.Text;
using ExtensionMethods = PeNet.Utilities.ExtensionMethods;

namespace PeNet
{
    /// <summary>
    ///     Represents an imported function.
    /// </summary>
    public class ImportFunction
    {
        /// <summary>
        ///     Create a new ImportFunction object.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="dll">DLL where the function comes from.</param>
        /// <param name="hint">Function hint.</param>
		/// <param name="thunk">Thunk RVA</param>
        public ImportFunction(string name, string dll, ushort hint, uint thunk)
        {
            Name = name;
            DLL = dll;
            Hint = hint;
			Thunk = thunk;
        }

        /// <summary>
        ///     Function name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     DLL where the function comes from.
        /// </summary>
        public string DLL { get; }

        /// <summary>
        ///     Function hint.
        /// </summary>
        public ushort Hint { get; }

		/// <summary>
		/// Thunk RVA
		/// </summary>
		public uint Thunk { get; }

        /// <summary>
        ///     Creates a string representation of all
        ///     properties of the object.
        /// </summary>
        /// <returns>The imported function as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("ImportFunction\n");
            sb.Append(ExtensionMethods.PropertiesToString(this, "{0,-20}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}