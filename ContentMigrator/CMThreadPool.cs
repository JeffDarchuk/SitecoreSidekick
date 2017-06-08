using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sitecore.Diagnostics;

namespace ScsContentMigrator
{
	public class CmThreadPool
	{
		private readonly List<IAsyncResult> _runningThreads = new List<IAsyncResult>();
		private readonly ConcurrentQueue<Tuple<WaitCallback, object>> _state = new ConcurrentQueue<Tuple<WaitCallback, object>>();
		private bool _starting = true;

		public CmThreadPool(OperationStatus status)
		{
			Task.Run(async () =>
			{
				while (_runningThreads.Count > 0 || _state.Count > 0 || _starting)
				{
					try
					{
						for (int i = 0; i < _runningThreads.Count; i++)
						{
							if (_runningThreads[i] == null || _runningThreads[i].IsCompleted)
							{
								_runningThreads.RemoveAt(i);
								i--;
							}
						}
						if (_runningThreads.Count < ContentMigrationRegistration.RemoteThreads)
						{
							Tuple<WaitCallback, object> stateTuple;
							if (_state.TryDequeue(out stateTuple))
							{
								_runningThreads.Add(stateTuple.Item1.BeginInvoke(stateTuple.Item2, null, null));
							}
							else
							{
								await Task.Delay(10);
							}
						}
					}
					catch (Exception e)
					{
						Log.Error("problem initializing the content migration thread", e, this);
					}
					if (_runningThreads.Count == ContentMigrationRegistration.WriterThreads)
						await Task.Delay(10);
				}
				status.EndOperation();
			});
		}
		public void Queue(WaitCallback f, object state = null)
		{
			_state.Enqueue(new Tuple<WaitCallback, object>(f, state));
			_starting = false;
		}
	}
}
