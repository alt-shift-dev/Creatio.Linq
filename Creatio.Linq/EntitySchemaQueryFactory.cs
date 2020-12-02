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
		/// <returns></returns>
		public static EntitySchemaQueryable<DynamicEntity> QuerySchema(this UserConnection userConnection, string schemaName)
		{
			if(null == userConnection) throw new ArgumentNullException(nameof(userConnection));
			if(string.IsNullOrEmpty(schemaName)) throw new ArgumentNullException(nameof(schemaName));

			return new EntitySchemaQueryable<DynamicEntity>(userConnection, schemaName);
		}
	}
}
