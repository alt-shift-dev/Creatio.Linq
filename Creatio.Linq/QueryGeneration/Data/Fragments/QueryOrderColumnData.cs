using System;
using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Query order column and direction.
	/// </summary>
	[DebuggerDisplay("{ColumnPath}, desc: {Descending}")]
	internal class QueryOrderColumnData: QuerySelectColumnData
	{
		/// <summary>
		/// true if sort in descending order.
		/// </summary>
		public bool Descending { get; set; }
	}
}