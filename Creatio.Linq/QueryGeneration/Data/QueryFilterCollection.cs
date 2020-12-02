using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Terrasoft.Common;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Defines collection of query filters.
	/// </summary>
	internal class QueryFilterCollection
	{
		private List<QueryFilterData> _filters = new List<QueryFilterData>();
		private List<QueryFilterCollection> _childFilters = new List<QueryFilterCollection>();

		/// <summary>
		/// Parent filter collection.
		/// </summary>
		[JsonIgnore]
		public QueryFilterCollection Parent { get; private set; }

		/// <summary>
		/// Whether whole filter collection should be reversed.
		/// </summary>
		public bool Negative { get; set; } = false;

		/// <summary>
		/// Logical operation applied to filters.
		/// </summary>
		public LogicalOperationStrict LogicalOperation { get; set; }

		/// <summary>
		/// Filters in current collection.
		/// </summary>
		public IReadOnlyList<QueryFilterData> Filters => _filters.AsReadOnly();

		/// <summary>
		/// Child filter collections.
		/// </summary>
		public IReadOnlyList<QueryFilterCollection> ChildFilters => _childFilters.AsReadOnly();

		/// <summary>
		/// Filter item which was added last.
		/// </summary>
		[JsonIgnore]
		public QueryFilterData Current { get; private set; }

		/// <summary>
		/// Adds new filter item, if null passed created shiny new empty filter.
		/// </summary>
		public void AddFilter(QueryFilterData filter = null)
		{
			Current = filter ?? new QueryFilterData();
			_filters.Add(Current);
		}

		/// <summary>
		/// Initializes new instance of <see cref="QueryFilterCollection"/> class.
		/// </summary>
		/// <param name="parent">Link to parent filter collection, null if it's top level.</param>
		public QueryFilterCollection(QueryFilterCollection parent = null)
		{
			Parent = parent;
		}

		/// <summary>
		/// Pushes new item to child filter collection and returns it.
		/// </summary>
		/// <returns></returns>
		public QueryFilterCollection PushCollection()
		{
			var childCollection = new QueryFilterCollection(this);
			_childFilters.Add(childCollection);

			return childCollection;
		}

		/// <summary>
		/// Returns true if current filter collection can be united with parent one.
		/// </summary>
		public bool CanUniteWithParent()
		{
			var sorryWeCantBeTogether = Negative 
			    || ChildFilters.Any() 
				|| LogicalOperation != Parent.LogicalOperation;

			return !sorryWeCantBeTogether;
		}

		/// <summary>
		/// Unites current filter collection with parent and return parent.
		/// </summary>
		public QueryFilterCollection TryUniteWithParent()
		{
			if (!CanUniteWithParent())
				return Parent;

			var parent = Parent;
			Parent = null;

			_filters.ForEach(parent.AddFilter);
			_filters.Clear();

			parent.RemoveChild(this);
			return parent;
		}


		private void RemoveChild(QueryFilterCollection child)
		{
			if (_childFilters.Contains(child))
			{
				_childFilters.Remove(child);
			}
		}
	}
}