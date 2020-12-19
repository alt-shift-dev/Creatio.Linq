using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Creatio.Linq.QueryGeneration.Data.States;
using Remotion.Linq.Clauses;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Aggregates query info.
	/// </summary>
	internal class QueryPartCollector
	{
		private List<QueryOrderColumnData> _orders = new List<QueryOrderColumnData>();
		private List<QuerySelectColumnData> _select = new List<QuerySelectColumnData>();
		private List<QueryGroupColumnData> _groups = new List<QueryGroupColumnData>();
		private QueryFilterCollection _filters = new QueryFilterCollection();
		private ConstructorInfo _resultTypeConstructor;
		private AggregationTypeStrict? _resultAggregationType;

		/// <summary>
		/// Collection (hierarchical) of query filters.
		/// </summary>
		public QueryFilterCollection Filters => _filters;

		/// <summary>
		/// Collection of sort orders.
		/// </summary>
		public IReadOnlyList<QueryOrderColumnData> Orders => _orders.AsReadOnly();

		/// <summary>
		/// Collection of result columns.
		/// </summary>
		public IReadOnlyList<QuerySelectColumnData> Select => _select.AsReadOnly();

		/// <summary>
		/// Collection of group columns.
		/// </summary>
		public IReadOnlyList<QueryGroupColumnData> Groups => _groups.AsReadOnly();

		/// <summary>
		/// Gets aggregation type for the whole query (if defined).
		/// </summary>
		public AggregationTypeStrict? ResultAggregationType => _resultAggregationType;
		
		/// <summary>
		/// Page size.
		/// </summary>
		public int? Take { get; set; }

		/// <summary>
		/// Page start.
		/// </summary>
		public int? Skip { get; set; }

		/// <summary>
		/// Whether query returns anonymous class with projection
		/// </summary>
		public bool UseResultProjection => _resultTypeConstructor != null || SelectColumnsDefined;

		/// <summary>
		/// Whether any select columns were defined.
		/// </summary>
		public bool SelectColumnsDefined => _select.Any();

		/// <summary>
		/// Whether any grouping columns were defined.
		/// </summary>
		public bool GroupingColumnsDefined => _groups.Any();

		/// <summary>
		/// Whether any ordering columns were defined.
		/// </summary>
		public bool OrderingColumnsDefined => _orders.Any();

		/// <summary>
		/// Gets ConstructorInfo provided by re-linq for generating query result items.
		/// </summary>
		public ConstructorInfo ResultProjectionCtor => _resultTypeConstructor;

		/// <summary>
		/// Add query sort.
		/// </summary>
		public void AddOrder(QueryOrderColumnData orderColumn)
		{
			_orders.Add(orderColumn ?? throw new ArgumentNullException(nameof(orderColumn)));
		}

		/// <summary>
		/// Add query grouping.
		/// </summary>
		public void AddGroup(QueryGroupColumnData group)
		{
			_groups.Add(group ?? throw new ArgumentNullException(nameof(group)));
		}

		/// <summary>
		/// Add column to query results.
		/// </summary>
		public void AddSelect(QuerySelectColumnData select)
		{
			_select.Add(select ?? throw new ArgumentNullException(nameof(select)));
		}

		/// <summary>
		/// Sets <see cref="ConstructorInfo"/> used to create result projections.
		/// </summary>
		public void SetResultConstructor(ConstructorInfo constructorInfo)
		{
			_resultTypeConstructor = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
		}

		/// <summary>
		/// Sets aggregation type for the whole query.
		/// </summary>
		public void SetResultAggregationType(AggregationTypeStrict aggregationType)
		{
			_resultAggregationType = aggregationType;
		}
	}
}