using System;

namespace Creatio.Linq.QueryGeneration.Util
{
	/// <summary>
	/// Restores query mode by invoking configured dispose action.
	/// </summary>
	public class DisposeAction: IDisposable
	{
		private Action _disposeAction;

		public DisposeAction(Action disposeAction)
		{
			_disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
		}

		public void Dispose()
		{
			_disposeAction.Invoke();
		}
	}
}