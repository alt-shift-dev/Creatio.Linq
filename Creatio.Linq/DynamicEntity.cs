using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace Creatio.Linq
{
	public class DynamicEntity: Entity
	{
		public DynamicEntity(UserConnection userConnection) : base(userConnection)
		{
		}

		public DynamicEntity(UserConnection userConnection, Guid schemaUId) : base(userConnection, schemaUId)
		{
		}

		public DynamicEntity(Entity source) : base(source)
		{
		}

		#region Public methods & properties

		/// <summary>
		/// Defines path to ESQ column.
		/// Only use in LINQ expressions, returns empty value.
		/// </summary>
		/// <typeparam name="T">Type of column value.</typeparam>
		/// <param name="columnPath">ESQ expression for column path.</param>
		/// <returns>default(T)</returns>
		public T Column<T>(string columnPath)
		{
			return default;
		}

		/// <summary>
		/// Same as <see cref="Column{T}"/> but used to define aggregated columns.
		/// </summary>
		/// <typeparam name="T">Type of column value.</typeparam>
		/// <param name="columnPath">ESQ expression for column path.</param>
		/// <returns>default(T[])</returns>
		public T[] AggColumn<T>(string columnPath)
		{
			return default;
		}

		#endregion
	}
}