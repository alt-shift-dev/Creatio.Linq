using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Creatio.Linq
{
	public class DebugWriter
	{
		private static Action<string> _writer = null;

		public static void SetWriter(Action<string> writer)
		{
			_writer = writer;
		}

		public static void ClearWriter()
		{
			SetWriter(null);
		}

		public static void WriteLine(string message)
		{
			if(null == _writer)
			{
				Trace.WriteLine(message);
			}
			else
			{
				_writer(message);
			}
		}
	}
}
