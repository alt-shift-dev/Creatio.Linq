using System;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace Creatio.Linq
{
	/// <summary>
	/// Extends Terrasoft.Core.Entities.Entity to allow defining
	/// ESQ column expressions in LINQ queries.
	/// </summary>
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
			throw new InvalidOperationException("Do not use this method except for defining LINQ queries.");
		}
		
		#endregion
	}
}