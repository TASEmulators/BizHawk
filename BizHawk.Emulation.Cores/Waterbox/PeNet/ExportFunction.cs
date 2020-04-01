using System.Text;
using ExtensionMethods = PeNet.Utilities.ExtensionMethods;

namespace PeNet
{
    /// <summary>
    ///     Represents an exported function.
    /// </summary>
    public class ExportFunction
    {
        /// <summary>
        ///     Create a new ExportFunction object.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="address">Address of function.</param>
        /// <param name="ordinal">Ordinal of the function.</param>
        public ExportFunction(string name, uint address, ushort ordinal)
        {
            Name = name;
            Address = address;
            Ordinal = ordinal;
        }

        /// <summary>
        ///     Function name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Function RVA.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        ///     Function Ordinal.
        /// </summary>
        public ushort Ordinal { get; }

        /// <summary>
        ///     Creates a string representation of all
        ///     properties of the object.
        /// </summary>
        /// <returns>The exported function as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("ExportFunction\n");
            sb.Append(ExtensionMethods.PropertiesToString(this, "{0,-20}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}