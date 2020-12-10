using System;
using System.Diagnostics;

namespace Creatio.Linq.QueryGeneration.Data.Fragments
{
	/// <summary>
	/// Represents query result column.
	/// </summary>
	[DebuggerDisplay("{ColumnPath} ({ColumnType})")]
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
		/// Column type.
		/// </summary>
		public Type ColumnType { get; set; }


		public QuerySelectColumnData(string columnPath, Type columnType)
		{
			if (string.IsNullOrEmpty(columnPath)) throw new ArgumentNullException(nameof(columnPath));
			//if (null == columnType) throw new ArgumentNullException(nameof(columnType));

			ColumnPath = columnPath;
			ColumnType = columnType;
		}
	}
}