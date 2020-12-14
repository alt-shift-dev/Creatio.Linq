﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting GroupBy clause.
	/// </summary>
	internal class QueryCollectorStateGroupBy: QueryCollectorStateBase
	{
		private List<QueryGroupColumnData> _columns = new List<QueryGroupColumnData>();

		public QueryCollectorStateGroupBy(QueryPartCollector aggregator) : base(aggregator)
		{
			Trace.WriteLine("Entering GroupBy state.");
		}

		public override void SetColumn(string columnPath)
		{
			_columns.Add(new QueryGroupColumnData(_columns.Count, columnPath));
		}

		public override void SetColumnAlias(int position, string alias)
		{
			if (_columns.Count <= position || position < 0) 
				throw new InvalidOperationException($"Unable to set alias for column at position {position} because it does not exist.");

			if(string.IsNullOrEmpty(alias))
				throw new ArgumentNullException(nameof(alias));

			_columns[position].Alias = alias;
		}

		public override void Dispose()
		{
			_columns.ForEach(Aggregator.AddGroup);

			Trace.WriteLine("Disposing GroupBy state.");
		}
	}
}