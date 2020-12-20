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
		/// <inheritdoc />
		public DynamicEntity(UserConnection userConnection) : base(userConnection)
		{
		}

		/// <inheritdoc />
		public DynamicEntity(UserConnection userConnection, Guid schemaUId) : base(userConnection, schemaUId)
		{
		}

		/// <inheritdoc />
		public DynamicEntity(Entity source) : base(source)
		{
		}

		#region Public methods

		/// <summary>
		/// Defines path to ESQ column.
		/// Only use in LINQ expressions, throws exception if executed.
		/// </summary>
		/// <typeparam name="T">Type of column value.</typeparam>
		/// <param name="columnPath">ESQ expression for column path.</param>
		public T Column<T>(string columnPath)
		{
			throw new InvalidOperationException("Do not use this method except for defining LINQ queries.");
		}
		
		#endregion
	}
}