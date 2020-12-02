using System;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Represents query result column.
	/// </summary>
	public class QuerySelectColumnData
	{
		/// <summary>
		/// Column path.
		/// </summary>
		public string ColumnPath { get; set; }

		/// <summary>
		/// Column alias in ESQ.
		/// </summary>
		public string ColumnAlias { get; set; }

		/// <summary>
		/// Column type.
		/// </summary>
		public Type ColumnType { get; set; }


		public QuerySelectColumnData(string columnPath, Type columnType)
		{
			if (string.IsNullOrEmpty(columnPath)) throw new ArgumentNullException(nameof(columnPath));
			if (null == columnType) throw new ArgumentNullException(nameof(columnType));

			ColumnPath = columnPath;
			ColumnType = columnType;
		}
	}
}