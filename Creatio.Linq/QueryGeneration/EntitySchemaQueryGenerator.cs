using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Creatio.Linq.QueryGeneration.Data;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration
{
	/// <summary>
	/// Generates <see cref="EntitySchemaQuery"/> based on query parts collected by
	/// expression visitors.
	/// </summary>
	internal class EntitySchemaQueryGenerator
	{
		private string _schemaName;
		private QueryPartsAggregator _queryParts;

		/// <summary>
		/// Initializes new instance of <see cref="EntitySchemaQueryGenerator"/> class.
		/// </summary>
		/// <param name="schemaName">Schema name.</param>
		/// <param name="queryParts">Query parts.</param>
		public EntitySchemaQueryGenerator(string schemaName, QueryPartsAggregator queryParts)
		{
			_schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
			_queryParts = queryParts ?? throw new ArgumentNullException(nameof(queryParts));
		}

		/// <summary>
		/// Generate ESQ based on collected query data.
		/// </summary>
		/// <param name="userConnection"></param>
		/// <returns>ESQ with projection converter.</returns>
		public EntitySchemaQueryWithProjection GenerateQuery(UserConnection userConnection)
		{
			if(null == userConnection) throw new ArgumentNullException(nameof(userConnection));

			var columns = new Dictionary<string, EntitySchemaQueryColumn>();
			var esq = new EntitySchemaQueryWithProjection(userConnection.EntitySchemaManager, _schemaName);

			if (_queryParts.UseResultProjection)
			{
				foreach (var queryColumnData in _queryParts.Select)
				{
					if (!columns.ContainsKey(queryColumnData.ColumnPath))
					{
						var column = esq.AddColumn(queryColumnData.ColumnPath);
						columns.Add(queryColumnData.ColumnPath, column);
						queryColumnData.Name = column.Name;
					}
				}
			}
			else
			{
				esq.AddAllSchemaColumns();
			}

			int orderPosition = 1;
			foreach (var queryOrder in _queryParts.Orders)
			{
				var column = columns.ContainsKey(queryOrder.ColumnPath)
					? columns[queryOrder.ColumnPath]
					: esq.AddColumn(queryOrder.ColumnPath);

				column.OrderDirection = queryOrder.Descending
					? OrderDirection.Descending
					: OrderDirection.Ascending;

				column.OrderPosition = orderPosition++;
			}
			
			ApplyFilters(esq, esq.Filters, _queryParts.Filters);

			var resultProjector = CreateResultProjector(
				_queryParts.ResultProjectionCtor,
				_queryParts.Select.ToArray()
			);

			esq.SetResultProjector(resultProjector);

			return esq;
		}

		/// <summary>
		/// Recursively applies filters to ESQ.
		/// </summary>
		private void ApplyFilters(EntitySchemaQuery esq, EntitySchemaQueryFilterCollection rootFilter, QueryFilterCollection filters)
		{
			rootFilter.IsNot = filters.Negative;

			// apply filters
			foreach (var filter in filters.Filters)
			{
				rootFilter.Add(ConvertFilter(esq, filter));
			}

			// recursively apply nested filters
			foreach (var filterCollection in filters.ChildFilters)
			{
				var group = new EntitySchemaQueryFilterCollection(esq, filterCollection.LogicalOperation);

				ApplyFilters(esq, group, filterCollection);

				rootFilter.Add(group);
			}
		}

		/// <summary>
		/// Converts QueryFilterData to ESQ filter.
		/// </summary>
		private IEntitySchemaQueryFilterItem ConvertFilter(EntitySchemaQuery esq, QueryFilterData filter)
		{
			if(null == esq) throw new ArgumentNullException(nameof(esq));
			if(null == filter) throw new ArgumentNullException(nameof(filter));

			if (filter.Value.GetType().IsArray)
			{
				var enumerable = (IEnumerable)filter.Value;
				return esq.CreateFilterWithParameters(filter.ComparisonType, filter.ColumnPath,
					enumerable.Cast<object>());
			}

			return esq.CreateFilterWithParameters(filter.ComparisonType, filter.ColumnPath, filter.Value);
		}

		/// <summary>
		/// Creates result projector for previously generated ESQ.
		/// </summary>
		private Func<Entity, object> CreateResultProjector(ConstructorInfo projectionConstructor, QuerySelectColumnData[] selectColumns)
		{
			// no projection means we should return same entity
			if (null == projectionConstructor) return entity => entity;
			// but if projection was defined select columns are mandatory
			if (null == selectColumns) throw new ArgumentNullException(nameof(selectColumns));

			return entity =>
			{
				var ctorParams = selectColumns
					.Select(column => entity.GetColumnValue(column.Name))
					.ToArray();

				return projectionConstructor.Invoke(ctorParams);
			};
		}
	}
}