﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	internal class QueryPartsAggregator
	{
		private List<QueryOrderData> _orders = new List<QueryOrderData>();
		private List<QuerySelectColumnData> _select = new List<QuerySelectColumnData>();
		private List<QueryGroupColumnData> _groups = new List<QueryGroupColumnData>();
		private QueryFilterCollection _filters = new QueryFilterCollection();
		private ConstructorInfo _resultTypeConstructor;
		
		/// <summary>
		/// Collection (hierarchical) of query filters.
		/// </summary>
		public QueryFilterCollection Filters => _filters;

		/// <summary>
		/// Collection of sort orders.
		/// </summary>
		public IReadOnlyList<QueryOrderData> Orders => _orders.AsReadOnly();

		/// <summary>
		/// Collection of result columns.
		/// </summary>
		public IReadOnlyList<QuerySelectColumnData> Select => _select.AsReadOnly();

		/// <summary>
		/// Collection of group columns.
		/// </summary>
		public IReadOnlyList<QueryGroupColumnData> Groups => _groups.AsReadOnly();

		/// <summary>
		/// Page size.
		/// </summary>
		public int? Take { get; set; }

		/// <summary>
		/// Page start.
		/// </summary>
		public int? Skip { get; set; }

		/// <summary>
		/// Should return number of rows selected instead of projection or not.
		/// </summary>
		public bool ReturnCount = false;

		/// <summary>
		/// Whether query returns anonymous class with projection
		/// </summary>
		public bool UseResultProjection => _resultTypeConstructor != null;

		/// <summary>
		/// Gets ConstructorInfo provided by re-linq for generating query result items.
		/// </summary>
		public ConstructorInfo ResultProjectionCtor => _resultTypeConstructor;

		/// <summary>
		/// Add query sort.
		/// </summary>
		/// <param name="order"></param>
		public void AddOrder(QueryOrderData order)
		{
			_orders.Add(order ?? throw new ArgumentNullException(nameof(order)));
		}

		/// <summary>
		/// Add query grouping.
		/// </summary>
		/// <param name="group"></param>
		public void AddGroup(QueryGroupColumnData group)
		{
			_groups.Add(group ?? throw new ArgumentNullException(nameof(group)));
		}

		/// <summary>
		/// Add column to query results.
		/// </summary>
		/// <param name="select"></param>
		public void AddSelect(QuerySelectColumnData select)
		{
			_select.Add(select ?? throw new ArgumentNullException(nameof(select)));
		}

		/// <summary>
		/// Sets <see cref="ConstructorInfo"/> used to create result projections.
		/// </summary>
		/// <param name="constructorInfo"></param>
		public void SetResultConstructor(ConstructorInfo constructorInfo)
		{
			_resultTypeConstructor = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
		}
	}
}