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

		/// <summary>
		/// Initializes new instance of <see cref="QueryData"/> class.
		/// </summary>
		/// <param name="queryParts">Query parts collected when LINQ query was parsed.</param>
		public QueryData(QueryPartCollector queryParts)
		{
			QueryParts = queryParts ?? throw new ArgumentNullException(nameof(queryParts));
		}

		/// <summary>
		/// Generates <see cref="EntitySchemaQueryWithProjection"/> for previously collected query parts.
		/// </summary>
		/// <param name="userConnection">UserConnection instance.</param>
		/// <param name="schemaName">Root schema name.</param>
		public EntitySchemaQueryWithProjection CreateQuery(UserConnection userConnection, string schemaName)
		{
			return new EntitySchemaQueryGenerator(schemaName, QueryParts)
				.GenerateQuery(userConnection);
		}
	}
}