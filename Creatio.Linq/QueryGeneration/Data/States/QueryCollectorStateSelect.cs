using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Creatio.Linq.QueryGeneration.Data.Fragments;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data.States
{
	/// <summary>
	/// Defines behavior of <see cref="QueryPartsAggregator"/> when re-linq is visiting Select clause.
	/// </summary>
	internal class QueryCollectorStateSelect: QueryCollectorStateBase
	{
		private List<string> _fragments = new List<string>();
		private Type _type;

		public QueryCollectorStateSelect(QueryPartsAggregator aggregator) : base(aggregator)
		{
			Trace.WriteLine("Entering Select state.");
		}

		public override void SetColumn(string columnPath, Type columnType)
		{
			_fragments.Add(columnPath);

			if (null != columnType)
			{
				_type = columnType;
			}
		}

		public override void PushColumn()
		{
			if (_fragments.Any())
			{
				throw new InvalidOperationException("Nested result elements not allowed.");
			}
		}

		public override void PopColumn()
		{
			var columnPath = string.Join(".", _fragments);
			Aggregator.AddSelect(new QuerySelectColumnData(columnPath, _type));

			_fragments.Clear();
			_type = null;
		}

		public override void Dispose()
		{
			if (_fragments.Any())
			{
				throw new InvalidOperationException("Column fragments not empty.");
			}

			Trace.WriteLine("Disposing Select state.");
		}
	}
}