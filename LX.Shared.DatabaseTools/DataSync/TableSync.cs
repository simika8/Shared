using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using LX.Common.Database.ConnectionProvider;
using LX.Common.EventHandlers;

namespace LX.Common.Database.DataSync
{
#if EXTERN
	public
#else
	internal
#endif
	static class TableSync
	{
		private static FbRemoteEvent s_remoteEvent;
		private static FbConnection s_eventConn;
		private static EventHandler<FbRemoteEventCountsEventArgs> s_remoteEventHandler;

		private static readonly CancellationTokenSource s_cts = new CancellationTokenSource();
		private const int ConnectionCheckInterval = 5;

		private static readonly List<string> s_tables = new List<string>();
		private static readonly ConcurrentDictionary<string, ITableSyncSettings> s_tableSettingsMap = new ConcurrentDictionary<string, ITableSyncSettings>();

		private static readonly ConcurrentDictionary<string, DateTime> s_eventsFired = new ConcurrentDictionary<string, DateTime>();

		public static void RegisterTables(params string[] tableNames)
			=> s_tables.AddRange(tableNames);

		public static TableSyncSettings<T> AddEventHandler<T>(CallbackDelegate syncCallback, FilterDelegate<T> deleteFilter) where T : class, new()
		{
			TableSyncSettings<T> d;

			string tableName = typeof(T).Name;
			string syncName = $"{tableName}_SYNC";

			if (s_tableSettingsMap.TryGetValue(syncName, out var settings))
			{
				d = settings as TableSyncSettings<T>;
			}
			else
			{
				d = new TableSyncSettings<T>(tableName, syncCallback, deleteFilter);
				s_tableSettingsMap[syncName] = d;
			}

			return d;
		}

		public static TableSyncSettings<T> Get<T>() where T : class, new()
		{
			return s_tableSettingsMap.TryGetValue($"{typeof(T).Name}_SYNC", out var settings)
				? settings as TableSyncSettings<T>
				: null;
		}

		public static void Enable<T>() where T : class, new()
			=> Get<T>()?.Enable();

		public static void Disable<T>() where T : class, new()
			=> Get<T>()?.Disable();

		public static void Start()
		{
			static void RemoteEventHandler(object sender, FbRemoteEventCountsEventArgs args)
			{
				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} Esemény észlelve: {args.Name} ");

				// megjegyzem, hogy az adott típusú eventet fel kell dolgozni
				s_eventsFired[args.Name] = DateTime.Now;
			}


			static void ProcessEvents()
			{
				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} Esemény feldolgozás indul");

				while (!s_cts.IsCancellationRequested)
				{
					string eventName = s_eventsFired.OrderBy(x => x.Value).FirstOrDefault().Key;

					if (eventName is { } && s_tableSettingsMap.TryGetValue(eventName, out var tableSyncSettings))
					{
#if DEBUG
						Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} Esemény feldolgozás kezd: {eventName}");
						var watch = Stopwatch.StartNew();
#endif
						s_eventsFired.TryRemove(eventName, out _);

						if (tableSyncSettings.Enabled)
						{
							tableSyncSettings.RunCallback();
						}

#if DEBUG
						watch.Stop();
						Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} Esemény feldolgozás vége: {eventName}. {watch.ElapsedMilliseconds} ms");
#endif
					}
					else
					{
						Thread.Sleep(10);
					}
				}

				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} Esemény feldolgozás leáll");
			}

			Task.Run(() => ProcessEvents(), s_cts.Token);

			s_remoteEventHandler = RemoteEventHandler;

			ConnectionGuardLoopAsync();
		}

		public static void Stop()
		{
			s_cts.Cancel();

			// lekapcsoljuk az eseménykezelőt és a db kapcsolatot
			if (s_remoteEvent != null)
			{
				s_remoteEvent.CancelEvents();
				s_remoteEvent.RemoteEventCounts -= s_remoteEventHandler;
				s_remoteEvent.Dispose();
			}

			s_remoteEvent = null;
			s_remoteEventHandler = null;
			s_eventConn?.Dispose();
		}

		private static async void ConnectionGuardLoopAsync()
		{
			try
			{
				while (!s_cts.IsCancellationRequested)
				{
					int? error = CheckConnection();
					await Task.Delay((error ?? ConnectionCheckInterval) * 1000, s_cts.Token).ConfigureAwait(false);
				}
			}
			catch { /* ignored */ }
		}

		private static int? CheckConnection()
		{
			if (s_eventConn != null)
			{
				try
				{
					using var trans = s_eventConn.BeginTransaction();
					trans.Rollback();
					return null;
				}
				catch (Exception ex)
				{
					MessageHandler.SendMessage(
						nameof(TableSync),
						"Hiba történt a szinkronizáló adatbázis kapcsolat ellenőrzése közben:" + Environment.NewLine + ex,
						MessageHandler.MessageType.Warning);

					CloseEvents(ref s_remoteEvent);

					s_eventConn.Dispose();
					s_eventConn = null;
					GC.Collect();
				}
			}

			try
			{
				var connectionProvider = new LXConnectionProvider();
				s_eventConn = connectionProvider.GetOpenConnection();
				s_remoteEvent = CreateNewEvent(connectionProvider.ConnectionString, s_remoteEventHandler, s_tables);
			}
			catch (Exception ex)
			{
				MessageHandler.SendMessage(
					nameof(TableSync),
					$"Hiba történt a tábla szinkronizálás közben:" + Environment.NewLine + ex,
					MessageHandler.MessageType.Error);
				return 1;
			}

			return null;
		}

		private static void CloseEvents(ref FbRemoteEvent fbRemoteEvent)
		{
			fbRemoteEvent?.Dispose();
			fbRemoteEvent = null;
		}

		private static FbRemoteEvent CreateNewEvent(string connectionString, EventHandler<FbRemoteEventCountsEventArgs> eventHandler, List<string> tableList)
		{
			var remoteEvent = new FbRemoteEvent(connectionString);
			remoteEvent.RemoteEventCounts += eventHandler;
			remoteEvent.QueueEvents(tableList.Select(t => $"{t}_SYNC").ToArray());
			return remoteEvent;
		}
	}
}
