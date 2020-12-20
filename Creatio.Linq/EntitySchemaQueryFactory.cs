using System;
using Terrasoft.Core;

namespace Creatio.Linq
{
	public static class EntitySchemaQueryFactory
	{
		/// <summary>
		/// Allows to use LINQ queries with current user connection.
		/// </summary>
		/// <param name="userConnection">UserConnection instance.</param>
		/// <param name="schemaName">Root schema name (i.e. "Contact").</param>
		public static EntitySchemaQueryable<DynamicEntity> QuerySchema(this UserConnection userConnection, string schemaName)
		{
			if (null == userConnection) throw new ArgumentNullException(nameof(userConnection));
			if (string.IsNullOrEmpty(schemaName)) throw new ArgumentNullException(nameof(schemaName));

			return new EntitySchemaQueryable<DynamicEntity>(userConnection, schemaName);
		}

		/// <summary>
		/// Allows to use LINQ queries with current user connection.
		/// </summary>
		/// <param name="userConnection">UserConnection instance.</param>
		/// <param name="schemaName">Root schema name (i.e. "Contact").</param>
		/// <param name="logOptions">Allows to log query generation process.</param>
		public static EntitySchemaQueryable<DynamicEntity> QuerySchema(this UserConnection userConnection, string schemaName, LogOptions logOptions)
		{
			if (null == userConnection) throw new ArgumentNullException(nameof(userConnection));
			if (string.IsNullOrEmpty(schemaName)) throw new ArgumentNullException(nameof(schemaName));
			if (null == logOptions) throw new ArgumentNullException(nameof(logOptions));
			if (null == logOptions.LogAction) throw new InvalidOperationException($"LogOptions.LogAction cannot be null.");

			return new EntitySchemaQueryable<DynamicEntity>(userConnection, schemaName, logOptions);
		}
	}
}
