using System;
using System.Threading;

namespace LX.Common
{
	/// <summary>
	/// Megadott számú szálra tud várakozni,
	/// addig vár, míg a megadott szám alá csökken az aktuálisan futó szálak száma
	/// </summary>
	public class CountdownEvent : IDisposable
	{
		private readonly ManualResetEvent _doneAll;
		private readonly ManualResetEvent _doneOne;
		private readonly int _total;
		private int _current;
		private bool _disposedValue;// = false;
		private readonly object _lockObject = new object();

		/// <summary>
		/// Létrehozza és beállítja
		/// </summary>
		/// <param name="total">Maximálisan futó szálak száma</param>
		public CountdownEvent(int total)
		{
			_total = total;
			_current = 0;
			_doneAll = new ManualResetEvent(true);
			_doneOne = new ManualResetEvent(true);
		}

		/// <summary>
		/// Szál indítása előtt kell meghívni
		/// </summary>
		public void AddCount()
		{
			lock (_lockObject)
			{
				if (++_current >= _total)
				{
					_doneOne.Reset();
				}

				_doneAll.Reset();
			}
		}

		/// <summary>
		/// Szál befejezésekor kell meghívni
		/// </summary>
		public void Signal()
		{
			lock (_lockObject)
			{
				if (--_current <= _total)
				{
					_doneOne.Set();
				}

				if (_current == 0)
				{
					_doneAll.Set();
				}
			}
		}

		/// <summary>
		/// Megvárja, míg az összes szál befejeződött
		/// </summary>
		public void Wait() => _doneAll.WaitOne();

		/// <summary>
		/// Megvárja, míg a futó szálak száma a megadott limit alá csökken,
		/// hogy újat indíthassunk
		/// </summary>
		public void WaitOne() => _doneOne.WaitOne();

		/// <summary>
		/// Objektum elbontása, nem kötelező meghívni
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (_disposedValue)
			{
				return;
			}

			if (disposing)
			{
				((IDisposable)_doneAll).Dispose();
				((IDisposable)_doneOne).Dispose();
			}
			_disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Szálakkal kapcsolatos funkciók megvalósítása
	/// </summary>
	public class LXThreadTools : IDisposable
	{
		private bool _disposedValue;

		public ManualResetEvent ThreadsFinishedEvent { get; } = new ManualResetEvent(true);

		protected virtual void Dispose(bool disposing)
		{
			if (_disposedValue)
			{
				return;
			}

			if (disposing)
			{
				ThreadsFinishedEvent.Close();
			}

			_disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/*

		/// <summary>
		/// Indít egy új szálat, amit meg lehet várni
		/// </summary>
		/// <param name="start">Thread feladata</param>
		/// <returns>Elindított thread</returns>
		public Thread StartNewThread(ThreadStart start)
		{
			ThreadsFinishedEvent.Reset();
			var th = new Thread(() =>
			{
				try
				{
					start();
				}
				finally
				{
					if (Interlocked.Decrement(ref _threadCounter) == 0)
					{
						ThreadsFinishedEvent.Set();
					}
				}
			});

			Interlocked.Increment(ref _threadCounter);
			th.Start();
			return th;
		}

		*/

		#region Teszt

		/*

		public static void TimeoutRunner(Action work, int timeout)
		{
			TimeoutRunner(work, timeout, () => true);
		}

		public static void TimeoutRunner(Action work, int timeout, Func<bool> canProcess)
		{
			Thread thread;
			Stopwatch sw = new Stopwatch();
			Exception ex = null;

			thread = new Thread(() => {
				try
				{
					work();
				}
				catch (Exception e)
				{
					ex = e;
				}
			});

			sw.Start();
			thread.Start();

			while (thread.IsAlive)
			{
				if (sw.ElapsedMilliseconds > timeout)
				{
					thread.Abort();
					throw new TimeoutException($"A művelet nem futott le a rendelkezésre álló idő alatt ({timeout} ms).");
				}
				if (!canProcess())
				{
					thread.Abort();
					throw new OperationCanceledException("A művelet végrehajtása le lett állítva.");
				}
				Thread.Sleep(10);
			}

			if (ex != null)
			{
				throw ex;
			}
		}

		*/

		#endregion Teszt

	}

	/// <summary>
	/// Feljavított Timer
	/// </summary>
	public class LXTimer : IDisposable
	{
		private int _inProgress; // false
		private Timer _timer;
		private readonly TimerCallback _callback;
		private readonly ManualResetEvent _waitForMe = new ManualResetEvent(true);

		public LXTimer(TimerCallback callback)
		{
			_callback = callback;
			if (_timer is null)
			{
				_timer = new Timer(InternalCallback);
			}
		}

		public LXTimer(TimerCallback callback, object state, int dueTime, int period) : this(callback)
		{
			_timer = new Timer(InternalCallback, state, dueTime, period);
		}

		public LXTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period) : this(callback)
		{
			_timer = new Timer(InternalCallback, state, dueTime, period);
		}

		public LXTimer(TimerCallback callback, object state, uint dueTime, uint period) : this(callback)
		{
			_timer = new Timer(InternalCallback, state, dueTime, period);
		}

		public LXTimer(TimerCallback callback, object state, long dueTime, long period) : this(callback)
		{
			_timer = new Timer(InternalCallback, state, dueTime, period);
		}

		public bool Change(int dueTime, int period) => (_timer ?? (_timer = new Timer(InternalCallback))).Change(dueTime, period);

		public bool Change(TimeSpan dueTime, TimeSpan period) => (_timer ?? (_timer = new Timer(InternalCallback))).Change(dueTime, period);

		public bool Change(uint dueTime, uint period) => (_timer ?? (_timer = new Timer(InternalCallback))).Change(dueTime, period);

		public bool Change(long dueTime, long period) => (_timer ?? (_timer = new Timer(InternalCallback))).Change(dueTime, period);

		public bool WaitToFinish() => _waitForMe.WaitOne();

		public bool WaitToFinish(int timeout) => _waitForMe.WaitOne(timeout);

		public bool WaitToFinish(TimeSpan timeout) => _waitForMe.WaitOne(timeout);

		private void InternalCallback(object state)
		{
			_waitForMe.Reset();

			if (Interlocked.CompareExchange(ref _inProgress, 1, 0) != 0)
			{
				return;
			}

			try
			{
				_callback(state); // eredeti callback hívása
			}
			finally
			{
				--_inProgress;
				_waitForMe.Set();
			}
		}

		#region IDisposable Support

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (_disposedValue)
			{
				return;
			}

			if (disposing)
			{
				_waitForMe.Close();
				_timer.Dispose();
			}

			_disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
