using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terrasoft.Core.Process;

namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Contains extension methods for <see cref="Type"/>.
	/// </summary>
	internal static class TypeExtensions
	{
		/// <summary>
		/// Checks if given type is anonymous (compiler-generated).
		/// </summary>
		public static bool IsAnonymousType(this Type type)
		{
			_ = type ?? throw new ArgumentNullException(nameof(type));

			// https://stackoverflow.com/questions/2483023/how-to-test-if-a-type-is-anonymous
			return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
			       && type.IsGenericType && type.Name.Contains("AnonymousType")
			       && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
			       && type.Attributes.HasFlag(TypeAttributes.NotPublic);
		}

		/// <summary>
		/// Checks if given type is a LINQ grouping.
		/// </summary>
		public static bool IsLinqGrouping(this Type type)
		{
			_ = type ?? throw new ArgumentNullException(nameof(type));

			return type.FullName.StartsWith("System.Linq.IGrouping");

		}
    }
}