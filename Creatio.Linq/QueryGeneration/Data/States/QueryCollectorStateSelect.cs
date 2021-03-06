﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Creatio.Linq.QueryGeneration.Util;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting Select clause.
	/// </summary>
	internal class QueryCollectorStateSelect: QueryCollectorStateBase
	{
		private List<string> _fragments = new List<string>();
		private AggregationTypeStrict? _aggregationType = null;

		public QueryCollectorStateSelect(QueryPartCollector aggregator) : base(aggregator)
		{
			LogWriter.WriteLine("Entering Select state.");
		}

		public override void SetColumn(string columnPath)
		{
			_fragments.Add(columnPath);
		}

		public override void PushColumn()
		{
			if (IsColumnDataDefined())
			{
				throw new InvalidOperationException("Nested result elements not supported.");
			}
		}

		public override void PopColumn()
		{
			AppendColumn();
		}

		public override void SetFunction(string methodName, object value)
		{
			_aggregationType = AggregationTypeConverter.FromString(methodName);
		}

		public override void SetComparison(FilterComparisonType comparison, object value)
		{
			throw new NotSupportedException($"For filtering use .Where() clause.");
		}

		protected bool IsColumnDataDefined()
		{
			return _fragments.Any() || _aggregationType.HasValue;
		}

		protected void AppendColumn()
		{
			if (!IsColumnDataDefined())
			{
				return;
			}

			var columnPath = _fragments.Any()
				? string.Join("->", _fragments)
				: "";

			StoreColumn(new QuerySelectColumnData
			{
				ColumnPath = columnPath,
				AggregationType = _aggregationType,
			});

			_fragments.Clear();
			_aggregationType = null;
		}

		protected virtual void StoreColumn(QuerySelectColumnData column)
		{
			Aggregator.AddSelect(column);
		}

		public override void Dispose()
		{
			AppendColumn();

			LogWriter.WriteLine("Disposing Select state.");
		}
	}
}