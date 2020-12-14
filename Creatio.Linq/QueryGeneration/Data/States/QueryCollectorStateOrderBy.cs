using System.Diagnostics;
using Creatio.Linq.QueryGeneration.Data.Fragments;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartCollector"/> when re-linq is visiting OrderBy clause.
	/// </summary>
	internal class QueryCollectorStateOrderBy: QueryCollectorStateBase
	{
		private bool _descending = false;

		public QueryCollectorStateOrderBy(QueryPartCollector aggregator) : base(aggregator)
		{
			Trace.WriteLine("Entering OrderBy state.");
		}

		public override void SetColumn(string columnPath)
		{
			Aggregator.AddOrder(new QueryOrderData(columnPath, _descending));
			_descending = false;
		}

		public override void SetSortOrder(bool descending)
		{
			_descending = descending;
		}

		public override void Dispose()
		{
			Trace.WriteLine("Disposing OrderBy state.");
		}
	}
}