using System;
using System.Diagnostics;
using Terrasoft.Common;

namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Represents query result column.
	/// </summary>
	[DebuggerDisplay("{ColumnPath} ({AggregationType})")]
	internal class QuerySelectColumnData
	{
		/// <summary>
		/// Column path.
		/// </summary>
		public string ColumnPath { get; set; }

		/// <summary>
		/// Column alias in ESQ.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Aggregation func
		/// </summary>
		public AggregationTypeStrict? AggregationType { get; set; }

		/// <summary>
		/// Get internal column identifier - with column path and aggregation type.
		/// </summary>
		/// <returns></returns>
		public string GetColumnId()
		{
			return AggregationType.HasValue
				? $"{ColumnPath}|{AggregationType}"
				: ColumnPath;
		}
	}
}