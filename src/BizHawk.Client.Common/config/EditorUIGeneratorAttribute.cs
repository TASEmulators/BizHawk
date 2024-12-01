namespace BizHawk.Client.Common
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EditorUIGeneratorAttribute : Attribute
	{
		public readonly Type GeneratorType;

		public EditorUIGeneratorAttribute(Type generatorType)
		{
			GeneratorType = generatorType;
		}
	}
}
