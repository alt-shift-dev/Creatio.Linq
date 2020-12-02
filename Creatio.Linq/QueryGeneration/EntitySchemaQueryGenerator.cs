using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Creatio.Linq.QueryGeneration.Data;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration
{
	internal class EntitySchemaQueryGenerator
	{
		private string _schemaName;
		private QueryPartsAggregator _queryParts;

		public EntitySchemaQueryGenerator(string schemaName, QueryPartsAggregator queryParts)
		{
			_schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
			_queryParts = queryParts ?? throw new ArgumentNullException(nameof(queryParts));
		}

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
						queryColumnData.ColumnAlias = column.Name;
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
			if (filter.Value.GetType().IsArray)
			{
				var enumerable = filter.Value as IEnumerable;
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
					.Select(column => entity.GetColumnValue(column.ColumnAlias))
					.ToArray();

				return projectionConstructor.Invoke(ctorParams);
			};
		}
	}
}