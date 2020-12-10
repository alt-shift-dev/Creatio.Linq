using System;

namespace Creatio.Linq.QueryGeneration.Data
{
	/// <summary>
	/// Restores query mode by invoking configured dispose action.
	/// </summary>
	public class QueryModeRestorer: IDisposable
	{
		private Action _disposeAction;

		public QueryModeRestorer(Action disposeAction)
		{
			_disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
		}

		public void Dispose()
		{
			_disposeAction.Invoke();
		}
	}
}