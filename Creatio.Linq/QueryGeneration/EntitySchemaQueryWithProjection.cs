using System;
using System.Threading;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.QueryGeneration
{
	/// <summary>
	/// Extends <see cref="EntitySchemaQuery"/> to hold method which projects query result to
	/// type required in LINQ Select() method.
	/// </summary>
	public class EntitySchemaQueryWithProjection: EntitySchemaQuery
	{
		private Func<Entity, object> _resultProjector;

		/// <inheritdoc />
		public EntitySchemaQueryWithProjection()
		{
		}

		/// <inheritdoc />
		public EntitySchemaQueryWithProjection(EntitySchema rootSchema) : base(rootSchema)
		{
		}

		/// <inheritdoc />
		public EntitySchemaQueryWithProjection(EntitySchema rootSchema, CancellationToken cancellationToken) : base(rootSchema, cancellationToken)
		{
		}

		/// <inheritdoc />
		public EntitySchemaQueryWithProjection(EntitySchemaManager entitySchemaManager, string sourceSchemaName) : base(entitySchemaManager, sourceSchemaName)
		{
		}

		/// <inheritdoc />
		public EntitySchemaQueryWithProjection(EntitySchemaQuery source) : base(source)
		{
		}

		/// <summary>
		/// Sets func which converts <see cref="Entity"/> to result object requested in LINQ Select() method.
		/// </summary>
		public void SetResultProjector(Func<Entity, object> resultProjector)
		{
			_resultProjector = resultProjector ?? throw new ArgumentNullException(nameof(resultProjector));
		}

		/// <summary>
		/// Convert <see cref="Entity"/> to result object requested in LINQ Select() method.
		/// Use <see cref="SetResultProjector"/> prior to using projections.
		/// </summary>
		/// <typeparam name="TResult">Type of result.</typeparam>
		/// <param name="entity">Source entity.</param>
		/// <returns>Filled result entity.</returns>
		public TResult Project<TResult>(Entity entity)
		{
			_ = entity ?? throw new ArgumentNullException(nameof(entity));
			_ = _resultProjector ?? throw new InvalidOperationException("Call SetResultProjector() prior to using projections.");

			if (typeof(TResult) == typeof(DynamicEntity))
			{
				return (TResult)(object)new DynamicEntity(entity);
			}

			var result = _resultProjector(entity);
			try
			{
				return typeof(TResult) == result.GetType()
					? (TResult) result
					: (TResult) Convert.ChangeType(result, typeof(TResult));
			}
			catch
			{
				throw new InvalidOperationException($"Unable to convert type {result.GetType()} to target projection type {typeof(TResult)}.");
			}

		}
	}
}