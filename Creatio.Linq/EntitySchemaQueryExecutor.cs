using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Creatio.Linq.QueryGeneration;
using Creatio.Linq.QueryGeneration.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Remotion.Linq;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;

namespace Creatio.Linq
{
	/// <summary>
	/// This executor is called by re-linq when query is to be executed.
	/// </summary>
	public class EntitySchemaQueryExecutor: IQueryExecutor
	{
		private UserConnection _userConnection;
		private readonly string _schemaName;

		public EntitySchemaQueryExecutor(UserConnection userConnection, string schemaName)
		{
			_userConnection = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
			_schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
		}

		public TResult ExecuteScalar<TResult>(QueryModel queryModel)
		{
			return ExecuteCollection<TResult>(queryModel).Single();
		}

		public TResult ExecuteSingle<TResult>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			return returnDefaultWhenEmpty
				? ExecuteCollection<TResult>(queryModel).SingleOrDefault()
				: ExecuteCollection<TResult>(queryModel).Single();
		}

		public IEnumerable<TResult> ExecuteCollection<TResult>(QueryModel queryModel)
		{
			var queryData = EntitySchemaQueryExpressionModelVisitor.GenerateEntitySchemaQueryData(queryModel);
			var esq = queryData.CreateQuery(_userConnection, _schemaName);
			var esqOptions = GetQueryOptions(esq, queryData);

			LogAggregatedQueryParts(queryData.QueryParts);

			//esq.PrimaryQueryColumn.IsAlwaysSelect = true;
			
			Trace.WriteLine(esq.GetSelectQuery(_userConnection).GetSqlText());

			var entityCollection = null == esqOptions
				? esq.GetEntityCollection(_userConnection)
				: esq.GetEntityCollection(_userConnection, esqOptions);

			_userConnection = null;

			return entityCollection.Select(item => esq.Project<TResult>(item));
		}

		private static EntitySchemaQueryOptions GetQueryOptions(EntitySchemaQuery esq, QueryData queryData)
		{
			if(null == esq) throw new ArgumentNullException(nameof(esq));
			if(null == queryData) throw new ArgumentNullException(nameof(queryData));

			if (!(queryData.QueryParts.Skip.HasValue || queryData.QueryParts.Take.HasValue))
			{
				return null;
			}

			if (queryData.QueryParts.Skip.HasValue)
			{
				esq.UseOffsetFetchPaging = true;
			}

			return new EntitySchemaQueryOptions
			{
				RowsOffset = queryData.QueryParts.Skip ?? 0,
				PageableRowCount = queryData.QueryParts.Take ?? int.MaxValue,
				PageableDirection = PageableSelectDirection.Next,
				PageableConditionValues = new Dictionary<string, object>(),
			};

		}

		private void LogAggregatedQueryParts(QueryPartsAggregator aggregator)
		{
			var serialized = JsonConvert.SerializeObject(
				aggregator,
				Formatting.Indented,
				new JsonSerializerSettings
				{
					Converters =
					{
						new StringEnumConverter()
					}
				}
			);
			Trace.WriteLine($"Aggregated query:\r\n{serialized}\r\n");
		}
	}
}