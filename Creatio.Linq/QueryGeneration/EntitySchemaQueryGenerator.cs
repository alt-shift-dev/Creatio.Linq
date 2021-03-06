﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Creatio.Linq.QueryGeneration.Data;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Creatio.Linq.QueryGeneration.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
		private QueryPartCollector _queryParts;

		/// <summary>
		/// Initializes new instance of <see cref="EntitySchemaQueryGenerator"/> class.
		/// </summary>
		/// <param name="schemaName">Schema name.</param>
		/// <param name="queryParts">Query parts.</param>
		public EntitySchemaQueryGenerator(string schemaName, QueryPartCollector queryParts)
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

			var columnMap = new Dictionary<string, EntitySchemaQueryColumn>();
			var esq = new EntitySchemaQueryWithProjection(userConnection.EntitySchemaManager, _schemaName);

			LogAggregatedQueryParts(_queryParts);

			ApplyResultAggregation(userConnection);
			
			var orphanGroupColumns = MergeAndGetOrphanGroupColumns().ToArray();

			if (_queryParts.UseResultProjection || _queryParts.ResultAggregationType.HasValue)
			{
				ApplySelectColumns(esq, columnMap, _queryParts.Select);
			}
			else if (!_queryParts.GroupingColumnsDefined)
			{
				esq.AddAllSchemaColumns();
			}

			ApplyOrderColumns(esq, columnMap, _queryParts.Orders);
			ApplyGroupColumns(esq, columnMap, orphanGroupColumns);
			ApplyFilters(esq, esq.Filters, _queryParts.Filters);

			var resultProjector = CreateResultProjector(
				_queryParts.ResultProjectionCtor,
				_queryParts.ResultAggregationType,
				_queryParts.Select.ToArray()
			);

			esq.SetResultProjector(resultProjector);

			return esq;
		}

		/// <summary>
		/// Recursively applies filters to ESQ.
		/// </summary>
		private static void ApplyFilters(
			EntitySchemaQuery esq, 
			EntitySchemaQueryFilterCollection rootFilter, 
			QueryFilterCollection filters)
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
		/// Adds select columns to ESQ.
		/// </summary>
		private static void ApplySelectColumns(
			EntitySchemaQuery esq, 
			Dictionary<string, EntitySchemaQueryColumn> columnMap, 
			IEnumerable<QuerySelectColumnData> selectColumns)
		{
			foreach (var queryColumnData in selectColumns)
			{
				if (!columnMap.ContainsKey(queryColumnData.GetColumnId()))
				{
					var column = queryColumnData.AggregationType.HasValue
						? esq.AddColumn(esq.CreateAggregationFunction(queryColumnData.AggregationType.Value, queryColumnData.ColumnPath))
						: esq.AddColumn(queryColumnData.ColumnPath);

					var columnName = column.Name;

					// check if lookup field was requested
					if (!string.IsNullOrEmpty(column.ValueExpression.Path) &&
					    column.ValueExpression.Path != queryColumnData.ColumnPath)
					{
						// by default ESQ loads two columns for lookup fields: Id and Name
						// (more common: primary key column and primary display column).
						// this is super-complicated logic to determine primary key column name
						columnName += "Id";
					}
					
					columnMap.Add(queryColumnData.GetColumnId(), column);
					queryColumnData.Name = columnName;
				}
			}
		}

		/// <summary>
		/// Add order columns to ESQ.
		/// </summary>
		private static void ApplyOrderColumns(
			EntitySchemaQuery esq, 
			Dictionary<string, EntitySchemaQueryColumn> columnMap,
			IEnumerable<QueryOrderColumnData> orderColumns)
		{
			int orderPosition = 1;
			foreach (var queryOrder in orderColumns)
			{
				EntitySchemaQueryColumn column;

				if (!columnMap.TryGetValue(queryOrder.GetColumnId(), out column))
				{
					column = queryOrder.AggregationType.HasValue
						? esq.AddColumn(esq.CreateAggregationFunction(queryOrder.AggregationType.Value, queryOrder.ColumnPath))
						: esq.AddColumn(queryOrder.ColumnPath);
				}

				column.OrderDirection = queryOrder.Descending
					? OrderDirection.Descending
					: OrderDirection.Ascending;

				column.OrderPosition = orderPosition++;
			}
		}

		/// <summary>
		/// Add group columns if they were not added while in select mode.
		/// </summary>
		private static void ApplyGroupColumns(
			EntitySchemaQuery esq,
			Dictionary<string, EntitySchemaQueryColumn> columnMap,
			IEnumerable<QueryGroupColumnData> groupColumns)
		{
			foreach (var groupColumn in groupColumns)
			{
				if (!columnMap.ContainsKey(groupColumn.GetColumnId()))
				{
					var column = esq.AddColumn(groupColumn.ColumnPath);
					columnMap.Add(groupColumn.GetColumnId(), column);
					groupColumn.Name = column.Name;
				}
			}
		}

		/// <summary>
		/// Attempts to merge group columns with select/order columns, returns group columns which were not used in select.
		/// </summary>
		private IEnumerable<QueryGroupColumnData> MergeAndGetOrphanGroupColumns()
		{
			if (_queryParts.Groups.Count == 1)
			{
				foreach (var selectColumn in _queryParts.Select)
				{
					var shouldReplaceColumnPath =
						(selectColumn.AggregationType.HasValue && string.IsNullOrEmpty(selectColumn.ColumnPath))
						|| (!selectColumn.AggregationType.HasValue && selectColumn.ColumnPath == "Key");

					if (shouldReplaceColumnPath)
					{
						selectColumn.ColumnPath = _queryParts.Groups.First().ColumnPath;
					}
				}
			}

			foreach (var groupColumn in _queryParts.Groups)
			{
				// in some cases ColumnPath contains wrong ESQ column path
				// because of re-linq tree visit rules. All cases are described:
				Func<QuerySelectColumnData, bool> selector = (column) =>
					// case:
					// .GroupBy(item => new object[] { item.Column<int>("Column1"), item.Column<string>("Column2") })
					// .Select(group => new { Column1 = group.Key[0], Column2 = group.Key[1] })
					column.ColumnPath == QueryUtils.GetIndexMemberName(groupColumn.Position)
					// case:
					// .GroupBy(item => new { Column1 = item.Column<int>("Column1"), Column2 = item.Column<string>("Column2") })
					// .Select(group => new { Alias1 = group.Key.Column1, Alias2 = group.Key.Column2 })
					|| column.ColumnPath == QueryUtils.GetAliasMemberName(groupColumn.Alias)
					// case:
					// .GroupBy(item => item.Column<int>("Column1"))
					// .Select(group => new { Alias1 = group.Key })
					|| !column.AggregationType.HasValue && column.ColumnPath == QueryUtils.SingleGroupColumnAlias
					// case (aggregate columns have no column path):
					// .GroupBy(item => item.Column<int>("Column1"))
					// .Select(group => new { Alias1 = group.Key, Count = group.Count() })
					|| column.AggregationType.HasValue && string.IsNullOrEmpty(column.ColumnPath);
					
				// get select columns with incorrect column path
				var selectKeyColumns = _queryParts.Select
					.Where(selector)
					.ToArray();

				// same for order columns
				var orderKeyColumns = _queryParts.Orders
					.Where(selector)
					.ToArray();

				// 1. if column is contained in both Select/OrderBy AND GroupBy clause, then it will be added to ESQ
				//    in Select/OrderBy block and should not be added again in GroupBy block.
				// 2. if column is only contained in GroupBy clause, it will not be added in Select/OrderBy block
				//    and query will be broken - so we need to add it in GroupBy block, hence yield return.
				if (selectKeyColumns.Any() || orderKeyColumns.Any())
				{
					selectKeyColumns.ForEach(column => column.ColumnPath = groupColumn.ColumnPath);
					orderKeyColumns.ForEach(column => column.ColumnPath = groupColumn.ColumnPath);
				}
				else
				{
					yield return groupColumn;
				}
			}
		}
		
		/// <summary>
		/// Applies result aggregation.
		/// </summary>
		private void ApplyResultAggregation(UserConnection userConnection)
		{
			if (_queryParts.ResultAggregationType.HasValue)
			{
				if (_queryParts.Select.Count > 1)
				{
					throw new InvalidOperationException(
						$"'Count' aggregation can only be applied to 1 column, but you specified {_queryParts.Select.Count}");
				}

				switch (_queryParts.ResultAggregationType)
				{
					case AggregationTypeStrict.Avg:
					case AggregationTypeStrict.Sum:
					case AggregationTypeStrict.Min:
					case AggregationTypeStrict.Max:
						if (!_queryParts.SelectColumnsDefined)
						{
							throw new InvalidOperationException(
								$"{_queryParts.ResultAggregationType} cannot be applied when no result column defined.");
						}
						
						break;
				}

				if (!_queryParts.SelectColumnsDefined)
				{
					var columnPath = userConnection.EntitySchemaManager
						.GetInstanceByName(_schemaName)
						.PrimaryColumn.Name;
					
					_queryParts.AddSelect(new QuerySelectColumnData
					{
						AggregationType = _queryParts.ResultAggregationType,
						ColumnPath = columnPath
					});
				}
				else
				{
					_queryParts.Select.First().AggregationType = _queryParts.ResultAggregationType;
				}
			}
		}

		/// <summary>
		/// Converts QueryFilterData to ESQ filter.
		/// </summary>
		private static IEntitySchemaQueryFilterItem ConvertFilter(EntitySchemaQuery esq, QueryFilterData filter)
		{
			if(null == esq) throw new ArgumentNullException(nameof(esq));
			if(null == filter) throw new ArgumentNullException(nameof(filter));

			if (null != filter.Value && filter.Value.GetType().IsArray)
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
		private Func<Entity, object> CreateResultProjector(ConstructorInfo projectionConstructor, AggregationTypeStrict? resultAggregation, QuerySelectColumnData[] selectColumns)
		{
			// aggregation was defined
			if (resultAggregation.HasValue)
			{
				return entity => entity.GetColumnValue(selectColumns.First().Name);
			}
			
			// no aggregation & no projection 
			if (null == projectionConstructor)
			{
				// no columns - return same entity
				if (selectColumns.Length == 0)
				{
					return entity => entity;
				}

				if (selectColumns.Length == 1)
				{
					return entity => entity.GetColumnValue(selectColumns.First().Name);
				}

				throw new InvalidOperationException($"If you want to select more than 1 column you should use projections.");
			}
			
			// but if projection was defined select columns are mandatory
			if (null == selectColumns) throw new ArgumentNullException(nameof(selectColumns));

			// all other cases
			return entity =>
			{
				var ctorParams = selectColumns
					.Select(column => entity.GetColumnValue(column.Name))
					.ToArray();

				return projectionConstructor.Invoke(ctorParams);
			};
		}

		private void LogAggregatedQueryParts(QueryPartCollector aggregator)
		{
			if (!LogState.Options.LogAggregatedParts)
			{
				return;
			}
			
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

			LogWriter.WriteLine($"Aggregated query:\r\n{serialized}\r\n");
		}
	}
}