using System;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

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
		public QueryPartsAggregator QueryParts { get; }

		public QueryData(QueryPartsAggregator queryParts)
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