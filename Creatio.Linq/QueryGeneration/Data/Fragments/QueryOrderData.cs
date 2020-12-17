using System;
using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Query order column and direction.
	/// </summary>
	[DebuggerDisplay("{ColumnPath}, desc: {Descending}")]
	internal class QueryOrderData: QueryColumnDataBase
	{
		/// <summary>
		/// true if sort in descending order.
		/// </summary>
		public bool Descending { get; set; }

		/// <summary>
		/// Initialized new instance of <see cref="QueryOrderData"/> class.
		/// </summary>
		public QueryOrderData(string columnPath, bool descending)
		{
			if(string.IsNullOrEmpty(columnPath)) throw new ArgumentNullException(nameof(columnPath));

			ColumnPath = columnPath;
			Descending = descending;
		}
	}
}