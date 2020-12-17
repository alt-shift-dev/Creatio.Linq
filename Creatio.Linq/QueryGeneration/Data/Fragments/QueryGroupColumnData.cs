using System;
using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Represents group column.
	/// </summary>
	[DebuggerDisplay("{ColumnPath} ({Position}) - {Alias}")]
	internal class QueryGroupColumnData: QueryColumnDataBase
	{
		/// <summary>
		/// ESQ column name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// LINQ column alias.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Group parameter position in original LINQ-query.
		/// </summary>
		public int Position { get; set; }

		public QueryGroupColumnData(int position, string columnPath)
		{
			if(string.IsNullOrEmpty(columnPath)) throw new ArgumentNullException(nameof(columnPath));

			ColumnPath = columnPath;
			Position = position;
		}

		/// <summary>
		/// Gets column identifier (like for select columns).
		/// </summary>
		/// <returns></returns>
		public string GetColumnId() => ColumnPath;
	}
}