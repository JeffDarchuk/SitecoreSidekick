using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sitecore.Diagnostics;

namespace ScsContentMigrator
{
	public class CMThreadPool
	{
		private OperationStatus _status;
		private List<IAsyncResult> _runningThreads = new List<IAsyncResult>();
		private ConcurrentQueue<Tuple<WaitCallback, object>> _state = new ConcurrentQueue<Tuple<WaitCallback, object>>();
		private bool starting = true;

		public CMThreadPool(OperationStatus status)
		{
			_status = status;
			Task.Run(() =>
			{
				while (_runningThreads.Count > 0 || _state.Count > 0 || starting)
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
						if (_runningThreads.Count < ContentMigrationHandler.remoteThreads)
						{
							Tuple<WaitCallback, object> stateTuple = null;
							if (_state.TryDequeue(out stateTuple))
							{
								_runningThreads.Add(stateTuple.Item1.BeginInvoke(stateTuple.Item2, null, null));
							}
							else
							{
								Thread.Sleep(10);
							}
						}
					}
					catch (Exception e)
					{
						Log.Error("problem initializing the content migration thread", e, this);
					}
					if (_runningThreads.Count == ContentMigrationHandler.writerThreads)
						Thread.Sleep(10);
				}
				_status.EndOperation();
			});
		}
		public void Queue(WaitCallback f, object state = null)
		{
			_state.Enqueue(new Tuple<WaitCallback, object>(f, state));
			starting = false;
		}
	}
}
