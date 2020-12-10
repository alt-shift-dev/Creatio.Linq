using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Creatio.Linq.QueryGeneration.Data;
using Creatio.Linq.QueryGeneration.Data.States;
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
		private readonly QueryPartsAggregator _aggregator;
		private readonly QueryCollectorState _state;

		public static QueryData GenerateEntitySchemaQueryData(QueryModel queryModel)
		{
			var visitor = new EntitySchemaQueryExpressionModelVisitor();
			visitor.VisitQueryModel(queryModel);
			return visitor.GetEntitySchemaQueryData();
		}

		public EntitySchemaQueryExpressionModelVisitor()
		{
			_aggregator = new QueryPartsAggregator();
			_state = new QueryCollectorState(_aggregator);
		}

		public QueryData GetEntitySchemaQueryData()
		{
			_state.Dispose();
			return new QueryData(_aggregator);
		}

		protected override void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel)
		{
			Trace.WriteLine($"VisitBodyClauses");
			base.VisitBodyClauses(bodyClauses, queryModel);
		}

		public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitResultOperator: {resultOperator}");

			// First()
			if (resultOperator is FirstResultOperator)
			{
				_aggregator.Take = 1;
				return;
			}

			// Count()
			if (resultOperator is CountResultOperator || resultOperator is LongCountResultOperator)
			{
				_aggregator.ReturnCount = true;
			}

			// Take()
			if (resultOperator is TakeResultOperator takeOperator)
			{
				var exp = takeOperator.Count;

				if (exp.NodeType == ExpressionType.Constant)
				{
					_aggregator.Take = (int) ((ConstantExpression) exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
			}

			// Skip()
			if (resultOperator is SkipResultOperator skipResult)
			{
				var exp = skipResult.Count;

				if (exp.NodeType == ExpressionType.Constant)
				{
					_aggregator.Skip = (int) ((ConstantExpression) exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
			}

			// GroupBy()
			if (resultOperator is GroupResultOperator groupResult)
			{
				using (_state.PushAggregationMode(QueryPartAggregationMode.GroupBy))
				{
					var key = groupResult.KeySelector;
					var elementSelector = groupResult.ElementSelector;

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
			}
		}

		public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
		{
			Trace.WriteLine($"VisitMainFromClause: {fromClause}");

			base.VisitMainFromClause(fromClause, queryModel);

			var subQueryExpression = fromClause.FromExpression as SubQueryExpression;
			if (subQueryExpression == null)
			{
				return;
			}

			Trace.WriteLine($"Visiting SubQueryExpression: {subQueryExpression}");
			VisitQueryModel(subQueryExpression.QueryModel);
		}

		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			Trace.WriteLine($"VisitSelectClause: {selectClause}");

			using (_state.PushAggregationMode(QueryPartAggregationMode.Select))
			{

				UpdateEntitySchemaQueryExpression(selectClause.Selector);

				base.VisitSelectClause(selectClause, queryModel);
			}
		}

		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitWhereClause: {whereClause}");

			using (_state.PushAggregationMode(QueryPartAggregationMode.Where))
			{
				using (_state.PushFilter(LogicalOperationStrict.And))
				{
					UpdateEntitySchemaQueryExpression(whereClause.Predicate);
				}

				base.VisitWhereClause(whereClause, queryModel, index);
			}
		}

		public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitOrderByClause: {orderByClause}");

			using (_state.PushAggregationMode(QueryPartAggregationMode.OrderBy))
			{
				foreach (var ordering in orderByClause.Orderings)
				{
					_state.SetSortOrder(ordering.OrderingDirection == OrderingDirection.Desc);
					UpdateEntitySchemaQueryExpression(ordering.Expression);
				}

				base.VisitOrderByClause(orderByClause, queryModel, index);
			}
			
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			throw new InvalidOperationException($"EntitySchemaQuery LINQ provider does not support join operator. Use ESQ column path expressions instead.");
		}

		public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitAdditionalFromClause {fromClause}");
			
			base.VisitAdditionalFromClause(fromClause, queryModel, index);
		}

		public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
		{
			Trace.WriteLine($"VisitGroupJoinClause: {groupJoinClause}");
			throw new NotSupportedException("VisitGroupJoinClause");
		}


		private void UpdateEntitySchemaQueryExpression(Expression expression)
		{
			EntitySchemaQueryExpressionTreeVisitor.SetupQueryParts(expression, _state);
		}
	}
}