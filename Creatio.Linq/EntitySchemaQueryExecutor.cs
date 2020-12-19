using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Creatio.Linq.QueryGeneration;
using Creatio.Linq.QueryGeneration.Data;
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

		/// <summary>
		/// Initializes new instance of <see cref="EntitySchemaQueryExecutor"/> class.
		/// </summary>
		/// <param name="userConnection">User connection to execute query.</param>
		/// <param name="schemaName">Root entity schema.</param>
		public EntitySchemaQueryExecutor(UserConnection userConnection, string schemaName)
		{
			_userConnection = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
			_schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
		}

		/// <inheritdoc />
		public TResult ExecuteScalar<TResult>(QueryModel queryModel)
		{
			return ExecuteCollection<TResult>(queryModel).Single();
		}

		/// <inheritdoc />
		public TResult ExecuteSingle<TResult>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			return returnDefaultWhenEmpty
				? ExecuteCollection<TResult>(queryModel).SingleOrDefault()
				: ExecuteCollection<TResult>(queryModel).Single();
		}

		/// <inheritdoc />
		public IEnumerable<TResult> ExecuteCollection<TResult>(QueryModel queryModel)
		{
			var queryData = EntitySchemaQueryExpressionModelVisitor.GenerateEntitySchemaQueryData(queryModel);
			var esq = queryData.CreateQuery(_userConnection, _schemaName);
			var esqOptions = GetQueryOptions(esq, queryData);

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
	}
}