using System;
using System.Dynamic;
using System.Linq;

namespace Creatio.Linq
{
	public class DynamicColumn
	{
		private static readonly Type[] AllowedTypes = new[]
		{
			typeof(Guid),
			typeof(Guid?),
			typeof(int),
			typeof(int?),
			typeof(long),
			typeof(long?),
			typeof(string),
			typeof(bool),
			typeof(bool?),
			typeof(byte[]),
			typeof(decimal),
			typeof(decimal?),
			typeof(DateTime),
			typeof(DateTime?),
		};

		public string ColumnName { get; }
		public Type DefinedType { get; } = typeof(object);

		public DynamicColumn(string columnName)
		{
			if (string.IsNullOrEmpty(columnName))
			{
				throw new ArgumentNullException(nameof(columnName));
			}

			ColumnName = columnName;
		}

		public DynamicColumn(string columnName, Type definedType)
			: this(columnName)
		{
			if (!AllowedTypes.Contains(definedType))
			{
				throw new InvalidOperationException($"Column type {definedType} not supported.");
			}

			DefinedType = definedType;
		}

		/// <summary>
		/// Evaluates to "LIKE '%pattern%'"
		/// </summary>
		public bool Contains(string pattern)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Evaluates to "LIKE 'pattern%'"
		/// </summary>
		public bool StartsWith(string pattern)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Evaluates to "LIKE '%pattern'"
		/// </summary>
		public bool EndsWith(string pattern)
		{
			throw new NotSupportedException();
		}

		#region Implicit convertations

		public static implicit operator string(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator long(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator long?(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator int(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator int?(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator bool(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator bool?(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator decimal(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator decimal?(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator DateTime(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator DateTime?(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator Guid(DynamicColumn value)
		{
			return default;
		}

		public static implicit operator Guid?(DynamicColumn value)
		{
			return default;
		}

		#endregion
	}
}