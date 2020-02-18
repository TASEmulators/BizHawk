using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	/// <summary>Indicates that a method (or property getter/setter) inherited from a <see cref="IEmulatorService"/> has yet to be implemented.</summary>
	/// <remarks>
	/// By convention, calling a method with this attribute should throw a <see cref="NotImplementedException"/>.
	/// If this attribute is not present on an implementation, it is assumed that the method is implemented and working.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class FeatureNotImplementedAttribute : Attribute
	{
	}

	/// <summary>Indicates that a class intentionally does not inherit from the specified <see cref="IEmulatorService">IEmulatorServices</see>, and will never do so.</summary>
	/// <remarks>
	/// <see cref="ISpecializedEmulatorService">ISpecializedEmulatorServices</see> that a core doesn't implement should not be listed, as the semantic of only being applicable to some cores is already clear.<br/>
	/// Any <see cref="IEmulatorService"/> which isn't specified and is also not implemented is assumed to be a work-in-progress.
	/// These should be implemented as soon as possible, simply throwing a <see cref="NotImplementedException"/> on call, and should be annotated with <see cref="FeatureNotImplementedAttribute"/>.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ServiceNotApplicableAttribute : Attribute
	{
		/// <remarks>TODO neither array nor <see cref="IEnumerable{T}"/> is the correct collection to be using here, try <see cref="IReadOnlyList{T}"/>/<see cref="IReadOnlyCollection{T}"/> instead</remarks>
		public ServiceNotApplicableAttribute(Type[] types)
		{
			NotApplicableTypes = types?.AsEnumerable() ?? Enumerable.Empty<Type>();
		}

		public IEnumerable<Type> NotApplicableTypes { get; }
	}
}
