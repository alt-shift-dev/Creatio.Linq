using System;
using Terrasoft.Core;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Holds aggregated query elements.
	/// </summary>
	internal class QueryData
	{
		/// <summary>
		/// Aggregated query elements.
		/// </summary>
		public QueryPartCollector QueryParts { get; }

		public QueryData(QueryPartCollector queryParts)
		{
			QueryParts = queryParts ?? throw new ArgumentNullException(nameof(queryParts));
		}

		public EntitySchemaQueryWithProjection CreateQuery(UserConnection userConnection, string schemaName)
		{
			return new EntitySchemaQueryGenerator(schemaName, QueryParts)
				.GenerateQuery(userConnection);
		}
	}
}