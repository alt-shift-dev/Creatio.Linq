using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Creatio.Linq.QueryGeneration.Data;
using Creatio.Linq.QueryGeneration.Data.States;
using Creatio.Linq.QueryGeneration.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Terrasoft.Common;

namespace Creatio.Linq.QueryGeneration
{
	/// <summary>
	/// ESQ expression model visitor.
	/// Called when re-linq enters From(), Select(), OrderBy(), etc clauses.
	/// </summary>
	internal class EntitySchemaQueryExpressionModelVisitor: QueryModelVisitorBase
	{
		// hello, you owe me columns
		private readonly QueryPartCollector _collector;
		private readonly QueryCollectorState _state;

		/// <summary>
		/// Parses LINQ query and collects query parts to <see cref="QueryModel"/>.
		/// </summary>
		public static QueryData GenerateEntitySchemaQueryData(QueryModel queryModel)
		{
			_ = queryModel ?? throw new ArgumentNullException(nameof(queryModel));
			
			var visitor = new EntitySchemaQueryExpressionModelVisitor();
			visitor.VisitQueryModel(queryModel);
			return visitor.GetEntitySchemaQueryData();
		}

		/// <summary>
		/// Initializes new instance of <see cref="EntitySchemaQueryExpressionModelVisitor"/> class.
		/// </summary>
		private EntitySchemaQueryExpressionModelVisitor()
		{
			_collector = new QueryPartCollector();
			_state = new QueryCollectorState(_collector);
		}

		/// <summary>
		/// Get collected query parts.
		/// </summary>
		public QueryData GetEntitySchemaQueryData()
		{
			_state.Dispose();
			return new QueryData(_collector);
		}

		/// <inheritdoc />
		protected override void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel)
		{
			LogWriter.WriteLine($"VisitBodyClauses");
			base.VisitBodyClauses(bodyClauses, queryModel);
		}

		/// <inheritdoc />
		public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
		{
			LogWriter.WriteLine($"VisitResultOperator: {resultOperator}, type {resultOperator.GetType().Name}");

			switch (resultOperator)
			{
				// First()
				case FirstResultOperator _:
					_collector.Take = 1;
					break;

				// Count()
				case CountResultOperator _:
				case LongCountResultOperator _:
					_collector.SetResultAggregationType(AggregationTypeStrict.Count);
					break;

				// Take()
				case TakeResultOperator takeOperator:
					var takeExpr = takeOperator.Count;

					if (takeExpr.NodeType == ExpressionType.Constant)
					{
						_collector.Take = (int)((ConstantExpression)takeExpr).Value;
					}
					else
					{
						throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
					}

					break;

				// Skip()
				case SkipResultOperator skipOperator:
					var skipExpr = skipOperator.Count;

					if (skipExpr.NodeType == ExpressionType.Constant)
					{
						_collector.Skip = (int)((ConstantExpression)skipExpr).Value;
					}
					else
					{
						throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
					}

					break;

				// Min()
				case MinResultOperator _:
					_state.SetFunction("Min", null);
					break;

				// Max()
				case MaxResultOperator _:
					_state.SetFunction("Max", null);
					break;

				// Average()
				case AverageResultOperator _:
					_state.SetFunction("Average", null);
					break;

				// GroupBy()
				case GroupResultOperator groupOperator:
					using (_state.PushCollectorMode(QueryCollectionState.GroupBy))
					{
						var key = groupOperator.KeySelector;
						var elementSelector = groupOperator.ElementSelector;

						UpdateEntitySchemaQueryExpression(key);
						UpdateEntitySchemaQueryExpression(elementSelector);

						if (key is NewExpression newKeySelector)
						{
							int position = 0;
							foreach (var memberInfo in newKeySelector.Members)
							{
								_state.SetColumnAlias(position++, memberInfo.Name);
							}
						}
					}

					break;
			}
		}

		/// <inheritdoc />
		public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
		{
			LogWriter.WriteLine($"VisitMainFromClause: {fromClause}");

			base.VisitMainFromClause(fromClause, queryModel);

			var subQueryExpression = fromClause.FromExpression as SubQueryExpression;
			if (subQueryExpression == null)
			{
				return;
			}

			LogWriter.WriteLine($"Visiting SubQueryExpression: {subQueryExpression}");
			VisitQueryModel(subQueryExpression.QueryModel);
		}

		/// <inheritdoc />
		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			LogWriter.WriteLine($"VisitSelectClause: {selectClause}");

			using (_state.PushCollectorMode(QueryCollectionState.Select))
			{

				UpdateEntitySchemaQueryExpression(selectClause.Selector);

				base.VisitSelectClause(selectClause, queryModel);
			}
		}

		/// <inheritdoc />
		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			LogWriter.WriteLine($"VisitWhereClause: {whereClause}");

			using (_state.PushCollectorMode(QueryCollectionState.Where))
			{
				using (_state.PushFilter(LogicalOperationStrict.And))
				{
					UpdateEntitySchemaQueryExpression(whereClause.Predicate);
				}

				base.VisitWhereClause(whereClause, queryModel, index);
			}
		}

		/// <inheritdoc />
		public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
		{
			LogWriter.WriteLine($"VisitOrderByClause: {orderByClause}");

			using (_state.PushCollectorMode(QueryCollectionState.OrderBy))
			{
				foreach (var ordering in orderByClause.Orderings)
				{
					using (_state.PushColumn())
					{
						_state.SetSortOrder(ordering.OrderingDirection == OrderingDirection.Desc);
						UpdateEntitySchemaQueryExpression(ordering.Expression);
					}
				}

				base.VisitOrderByClause(orderByClause, queryModel, index);
			}
			
		}

		/// <inheritdoc />
		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			throw new InvalidOperationException($"EntitySchemaQuery LINQ provider does not support join operator. Use ESQ column path expressions instead.");
		}

		/// <inheritdoc />
		public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
		{
			LogWriter.WriteLine($"VisitAdditionalFromClause {fromClause}");
			
			base.VisitAdditionalFromClause(fromClause, queryModel, index);
		}

		/// <inheritdoc />
		public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
		{
			LogWriter.WriteLine($"VisitGroupJoinClause: {groupJoinClause}");
			throw new NotSupportedException("VisitGroupJoinClause");
		}

		/// <summary>
		/// Drills into query expression tree for further details, see <see cref="EntitySchemaQueryExpressionTreeVisitor"/>.
		/// </summary>
		private void UpdateEntitySchemaQueryExpression(Expression expression)
		{
			EntitySchemaQueryExpressionTreeVisitor.SetupQueryParts(expression, _state);
		}
	}
}