using System;
using Terrasoft.Core;

namespace Creatio.Linq
{
	public static class EntitySchemaQueryFactory
	{
		public static EntitySchemaQueryable<DynamicEntity> QuerySchema(this UserConnection userConnection, string schemaName)
		{
			return new EntitySchemaQueryable<DynamicEntity>(userConnection, schemaName);
		}
	}
}
