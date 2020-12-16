using System;
using System.Collections;
using System.Reflection;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.Tests
{
	/// <summary>
	/// Contains extensions for <see cref="Entity"/> class.
	/// </summary>
	public static class EntityExtensions
	{
		/// <summary>
		/// Generates new <see cref="Entity"/> instance and populates it's columns from anonymous class.
		/// </summary>
		public static Entity CreateEntity<T>(this UserConnection userConnection, string schemaName, T entityMap)
			where T: class
		{
			_ = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
			_ = entityMap ?? throw new ArgumentNullException(nameof(entityMap));
			_ = schemaName ?? throw new ArgumentNullException(nameof(schemaName));

			var entity = userConnection.EntitySchemaManager
				.GetInstanceByName(schemaName)
				.CreateEntity(userConnection);

			entity.PrimaryColumnValue = Guid.NewGuid();
			entity.SetDefColumnValues();

			var entityColumns = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var entityColumn in entityColumns)
			{
				entity.SetColumnValue(entityColumn.Name, entityColumn.GetValue(entityMap));
			}

			entity.Save();

			return entity;
		}

	}
}