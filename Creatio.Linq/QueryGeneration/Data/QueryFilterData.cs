using System;
using System.Diagnostics;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Parameters of single ESQ filter.
	/// </summary>
	[DebuggerDisplay("{ColumnPath} {ComparisonType} {Value}")]
	internal class QueryFilterData
	{
		/// <summary>
		/// Path to column.
		/// </summary>
		public string ColumnPath { get; set; }

		/// <summary>
		/// Value to compare with.
		/// </summary>
		public object Value { get; set; } = true;

		/// <summary>
		/// Comparison type.
		/// </summary>
		public FilterComparisonType ComparisonType { get; set; } = FilterComparisonType.Equal;

		public QueryFilterData()
		{
		}

		/// <summary>
		/// Initializes new instance of <see cref="QueryFilterData"/>.
		/// </summary>
		public QueryFilterData(string columnPath, object value, FilterComparisonType comparisonType)
		{
			ColumnPath = columnPath ?? throw new ArgumentNullException(nameof(columnPath));
			Value = value;
			ComparisonType = comparisonType;
		}
	}
}