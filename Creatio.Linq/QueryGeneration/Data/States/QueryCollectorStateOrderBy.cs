using System;
using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Creatio.Linq.QueryGeneration.Util;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting OrderBy clause.
	/// </summary>
	internal class QueryCollectorStateOrderBy: QueryCollectorStateSelect
	{
		private bool _descending = false;

		public QueryCollectorStateOrderBy(QueryPartCollector aggregator) : base(aggregator)
		{
			LogWriter.WriteLine("Entering OrderBy state.");
		}

		public override void SetSortOrder(bool descending)
		{
			_descending = descending;
		}

		protected override void StoreColumn(QuerySelectColumnData column)
		{
			var orderColumn = new QueryOrderColumnData
			{
				ColumnPath = column.ColumnPath,
				AggregationType = column.AggregationType,
				Descending = _descending,
				Name = column.Name
			};
			
			Aggregator.AddOrder(orderColumn);

			_descending = false;
		}

		public override void Dispose()
		{
			LogWriter.WriteLine("Disposing OrderBy state.");
			base.Dispose();
		}
	}
}