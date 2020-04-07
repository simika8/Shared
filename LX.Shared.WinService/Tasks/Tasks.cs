using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LX.Common;
using LX.Common.Database;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Database.DataSync;
using LX.Common.Database.Extensions;
using LX.Common.Database.Settings;
using LX.Common.EventHandlers;
using LX.Common.Extensions;
using LX.Shared.WinService.Tasks;

namespace LX.Shared.WinService
{

	#region Task azonosítók

	/*
		10001 .. 10303 comservből átírt funkciók
			10001 - rendelések kezelése

		11001 .. 11114 WinKarbból átírt funkciók
		12001 .. 12999 új lx_comm funkciók.
		13001 .. 13999 új lx_oep funkciók.
	*/

	#endregion Task azonosítók

	/// <summary>
	/// Task típusok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	enum TaskType
	{
		#region LX_Comm taskok

		CommRPC = 12001, // folyamatos
		Medigen = 12002, // hagyományos task
		Okospolc = 12003, // folyamatos
		UKF_Export = 12004, // hagyományos task
		Eeszt = 12005, // hagyományos task (, de krv@ gyors)
		BenuSAPv2_Export = 12006, // hagyományos task
		DispenserWS = 12007, // folyamatos
		QueueingSystemService = 12008, //folyamatos
		NOSZ = 12009, // hagyományos task (, de krv@ gyors)
		VCount = 12010,
		CentralWarehouseOrderHandler = 12011, // hagyományos task
		WPMDownloader = 12012, // hagyományos task
		DobozAzonositas = 12013, // folyamatos
		Palota = 12014, // hagyományos task

		#endregion LX_Comm taskok

		#region LX_OEP taskok

		OVF = 13001, // folyamatos
		PUPHAX = 13002,
		PUPHAXTTTLETOLT = 13003,
		EjelentesRPC = 13004,

		#endregion LX_OEP taskok

		#region LX_KarbantartoSvc taskok

		EPSync = 14001,
		UniversalExport = 14002,

		#endregion LX_KarbantartoSvc taskok

		#region Statements taskok

		StatementsExport = 15001,

		#endregion
	}

#if EXTERN
	public
#else
	internal
#endif
	class TaskActionParams
	{
		public bool Finished { get; set; } = true;
		public IEnumerable<T_TASKS_DETAILS> Details;
	}

#if EXTERN
	public
#else
	internal
#endif
	static class TaskManager
	{
		private static List<T_TASKS> s_tasksTable;
		private static List<T_TASKS_DETAILS> s_tasksDetails;

		private static readonly object s_lockTasks = new object();
		private static readonly object s_lockTasksDetails = new object();

		private static LXTimer s_taskTimer;
		private static IConnectionProvider s_connectionProvider;

		private static readonly Dictionary<TaskType, Type> s_tasks = new Dictionary<TaskType, Type>();
		private static Dictionary<TaskType, ITask> s_activeTasks = new Dictionary<TaskType, ITask>();

		public static bool IsActive { get; private set; } = true;
		public static bool IsActiveFunc() => IsActive;

		public static CancellationTokenSource Cancel { get; } = new CancellationTokenSource();

		public static void Start()
		{
			MessageHandler.SendMessage(nameof(TaskManager), "Task vezérlés kezdése", MessageHandler.MessageType.Info);
			IsActive = true;

			s_connectionProvider = new LXConnectionProvider();

			// task tábla szinkronizáció engedélyezése
			TableSync.Enable<T_TASKS>();

			// task details tábla szinkronizáció engedélyezése
			TableSync.Enable<T_TASKS_DETAILS>();

			// task timer indítása a ciklikus taskokhoz
			s_taskTimer = new LXTimer(TimerCallBack, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
		}

		public static void InitStop()
		{
			IsActive = false;

			Cancel.Cancel(false);
		}

		public static void Stop()
		{
			// task details tábla szinkronizáció leállítása
			TableSync.Disable<T_TASKS_DETAILS>();

			// task tábla szinkronizáció leállítása
			TableSync.Disable<T_TASKS>();

			// task timer megállítása
			s_taskTimer?.Change(Timeout.Infinite, Timeout.Infinite);

			// megvárjuk míg végigfut a ciklikus task feladata
			s_taskTimer?.WaitToFinish();
			s_taskTimer?.Dispose();
			s_taskTimer = null;

			lock (s_activeTasks)
			{
				// egyéb taskok leállítása
				Parallel.ForEach(s_activeTasks, current => StopTask(current.Value));

				// taskok törlése
				s_activeTasks.Clear();
			}

			s_connectionProvider = null;

			MessageHandler.SendMessage(nameof(TaskManager), "Task vezérlés befejeződött", MessageHandler.MessageType.Info);
		}

		/// <summary>
		/// Task kezelő osztály hozzáadása az adott task típushoz
		/// Minden tasktípushoz max egy taskkezelő osztály adható
		/// </summary>
		/// <param name="taskType">Task típusa</param>
		public static void AddTask<T>(TaskType taskType) where T : ITask
		{
			if (!s_tasks.ContainsKey(taskType))
			{
				s_tasks[taskType] = typeof(T);
			}
		}

		/// <summary>
		/// Task kezelő osztály eltávolítása az adott tasktípusról
		/// </summary>
		/// <param name="taskType">Task típusa</param>
		public static void RemoveTask(TaskType taskType)
		{
			s_tasks.Remove(taskType);
		}

		public static T GetTask<T>(TaskType taskType) where T : ITask
			=> s_activeTasks.TryGetValue(taskType, out var task) ? (T)task : throw new Exception($"Nem fut a(z) {Ext.GetDescription<T>()} háttérfolyamat.");

		/// <summary>
		///
		/// </summary>
		/// <exception cref="OperationCanceledException"></exception>
		public static void CheckCancelOperation()
		{
			// ha megállították a szervizt,
			if (!IsActive)
			{
				// akkor dobunk egy cancel exceptiont
				throw new OperationCanceledException("Feldolgozás megszakítva.");
			}
		}

		private static void StopTask(ITask taskObject)
		{
			if (taskObject is null)
			{
				return;
			}

			taskObject.Stop();
			MessageHandler.SendMessage(nameof(TaskManager), $"{taskObject.GetDescription()} modul leállt", MessageHandler.MessageType.Info);
		}

		private static void StartTask(ITask taskObject)
		{
			if (taskObject is null)
			{
				return;
			}

			MessageHandler.SendMessage(nameof(TaskManager), $"{taskObject.GetDescription()} modul indul", MessageHandler.MessageType.Info);
			taskObject.Start();
		}

		private static void TaskAktivChanged(T_TASKS currentTask)
		{
			if ("T" == currentTask.TS_AKTIV)
			{
				// ha már fut a task, akkor nem kell még1x elindítani
				lock (s_activeTasks)
				{
					if (s_activeTasks.ContainsKey((TaskType)currentTask.TS_TASKTYPE))
					{
						return;
					}
				}

				// task osztály példányosítása
				if (!(s_tasks[(TaskType)currentTask.TS_TASKTYPE].GetNewObject() is ITask taskObject))
				{
					return;
				}

				lock (s_activeTasks)
				{
					s_activeTasks.Add((TaskType)currentTask.TS_TASKTYPE, taskObject);
				}

				StartTask(taskObject);
			}
			else
			{
				ITask taskObject;
				lock (s_activeTasks)
				{
					taskObject = s_activeTasks.FirstOrDefault(t => t.Key.Equals((TaskType)currentTask.TS_TASKTYPE)).Value;
				}

				if (taskObject is null)
				{
					return;
				}

				try
				{
					StopTask(taskObject);
				}
				catch (Exception ex)
				{
					MessageHandler.SendMessage(nameof(TaskManager), $"Hiba a(z) {taskObject.GetDescription()} modul leállítása közben:" + Environment.NewLine + ex,
						MessageHandler.MessageType.Error);
				}

				lock (s_activeTasks)
				{
					s_activeTasks.Remove((TaskType)currentTask.TS_TASKTYPE);
				}

				GC.Collect();
			}
		}

		internal static void SyncCallbackTasks(DateTime changeTime, int tableId)
		{
			lock (s_lockTasks)
			{
				try
				{
					using var conn = s_connectionProvider.GetOpenConnection();
					using var trans = conn.BeginTransaction();
					string sql;

					if (s_tasksTable is { })
					{
						sql = "SELECT CAST(recordid AS integer) ts_id FROM t_sync_deletedrows WHERE tableid = @tableid AND deltime >= @deltime";
						int[] deletedRows = s_connectionProvider.Sql(sql)
							.AddParam("tableid", tableId)
							.AddParam("deltime", changeTime)
							.GetValues<int>();

						if (deletedRows.Any())
						{
							RemoveDeletedTasks(deletedRows);
						}
					}

					sql = "SELECT * FROM t_tasks WHERE ts_changetime >= @changetime;";
					var changedRows = s_connectionProvider.Sql(sql)
						.AddParam("changetime", changeTime)
						.GetObjects<T_TASKS>();

					ProcessChangedTasks(changedRows);

					trans.Rollback();
					GC.Collect();
				}
				catch (Exception ex)
				{
					MessageHandler.SendMessage(nameof(TaskManager),
						"Hiba történt a taskok sorok lekérdezése közben:" + Environment.NewLine + ex,
						MessageHandler.MessageType.Error);
				}
			}
		}

		private static void ProcessChangedTasks(T_TASKS[] changedRows)
		{
			if (!changedRows.Any())
			{
				return;
			}

			Parallel.ForEach(changedRows.Where(c => s_tasks.ContainsKey((TaskType)c.TS_TASKTYPE)).ToArray(), TaskAktivChanged);

			s_tasksTable = changedRows
				.Union(s_tasksTable.Coalesce())
				.ToList();
		}

		private static void RemoveDeletedTasks(int[] deletedRows)
		{
			var deletedTasks = s_tasksTable.Where(t => deletedRows.Contains(t.TS_ID));
			var taskPairs = s_activeTasks
				.Where(ts => deletedTasks.Any(t => t.TS_TASKTYPE == (int)ts.Key))
				.ToArray();

			foreach (var current in taskPairs)
			{
				StopTask(current.Value);
			}

			s_activeTasks = s_activeTasks
				.AsEnumerable()
				.Except(taskPairs)
				.AsDictionary();

			s_tasksTable = s_tasksTable
				.Except(deletedTasks)
				.ToList();
		}

		internal static Func<T_TASKS_DETAILS, bool> TaskDetailsDeleteFilter(int[] deletedRows)
			=> oldRows => !deletedRows.Contains(oldRows.TD_ID);

		internal static void SyncCallbackTasksDetails(DateTime changeTime, int tableId)
		{
			lock (s_lockTasksDetails)
			{
				try
				{
					const string selectSql = "SELECT * FROM t_tasks_details WHERE td_changetime >= @td_changetime;";

					s_tasksDetails = TableSync.Get<T_TASKS_DETAILS>()?.CallbackCore(s_tasksDetails, ("td_changetime", changeTime), selectSql);
				}
				catch (Exception ex)
				{
					MessageHandler.SendMessage(
						nameof(TaskManager),
						$"Hiba történt a {nameof(T_TASKS_DETAILS)} sorok lekérdezése közben:" + Environment.NewLine + ex,
						MessageHandler.MessageType.Error);
				}
			}
		}

		private static bool ProcessTasks()
		{
			bool voltTask = false;

			try
			{
				Dictionary<TaskType, ITask> loopTaskObjs;
				lock (s_activeTasks)
				{
					loopTaskObjs = s_activeTasks
						.Where(t => t.Value is ILoopTask)
						.AsDictionary();
				}

				static bool Filter(T_TASKS t)
					=> t.TS_AKTIV.Equals("T")
					&& t.TS_LASTWORKTIME
						   .NAddSeconds(t.TS_ERRORWAITTIME ?? t.TS_FREQ)
						   .GetValueOrDefault() <= DateTime.Now;

				var aktTasks = s_tasksTable.Coalesce().Where(Filter);

				// végigmegy a beregisztált ciklikus taskokon
				foreach (var currentTask in aktTasks.Where(t => loopTaskObjs.ContainsKey((TaskType)t.TS_TASKTYPE)))
				{
					if (!voltTask)
					{
						voltTask = true;
					}

					if (!IsActive)
					{
						break; // ha megállították a szervizt
					}

					int? errorWaitTime = ProcessTask(currentTask, loopTaskObjs[(TaskType)currentTask.TS_TASKTYPE] as ILoopTask);

					const string updateSql = // SQL
					#region SQL
@"UPDATE t_tasks SET
	  ts_lastworktime = current_timestamp
	, ts_errorwaittime = @ts_errorwaittime
WHERE (ts_id = @ts_id);";
					#endregion SQL

					s_connectionProvider.Sql(updateSql)
						.AddParam("ts_id", currentTask.TS_ID)
						.AddParam("ts_errorwaittime", errorWaitTime)
						.ExecSql();
				}
			}
			catch (Exception ex)
			{
				MessageHandler.SendMessage(
					nameof(TaskManager),
					"Hiba történt a taskok feldolgozása közben:" + Environment.NewLine + ex,
					MessageHandler.MessageType.Error);
			}
			return voltTask;
		}

		private static int? ProcessTask(T_TASKS currentTask, ILoopTask taskObj)
		{
			int? errorWaitTime = null;
			bool forced = !currentTask.TS_LASTWORKTIME.HasValue; // volt-e kapcsolódikra kattintás, vagy első alkalommal fut a task

			var details = s_tasksDetails
				.Coalesce()
				.Where(d => 1 == d.TD_STATUS && d.TD_TASKTYPE == currentTask.TS_TASKTYPE);

			var taskActionParams = new TaskActionParams { Details = details };

			try
			{
				do
				{
					if (TimeIsAcceptable(currentTask.TS_PREFWORKTIMENAME, (taskObj as ILoopTaskDL)?.GetDeadlines(forced)))
					{
						taskObj.TaskAction(taskActionParams); // az adott task feladatát (TaskActionjét) futtatjuk
					}
					else
					{
						taskActionParams.Finished = true;
					}
				} while (!taskActionParams.Finished);
			}
			catch (OperationCanceledException ex) when (!IsActive)
			{
				// a folyamat meg lett szakítva, a szerviz újraindítása után folytatódik egyből
				errorWaitTime = 1;
				MessageHandler.SendMessage(nameof(TaskManager), ex.Message, MessageHandler.MessageType.Warning);
			}
			catch (Exception ex)
			{
				// hiba esetén 5 perc múlva újrapróbáljuk
				errorWaitTime = 5 * 60;
				MessageHandler.SendMessage(
					nameof(TaskManager),
					$"Hiba történt a(z) '{currentTask.TS_NAME}' task futása közben:" + Environment.NewLine + ex,
					MessageHandler.MessageType.Error);
			}

			return errorWaitTime;
		}

		private static void TimerCallBack(object state)
		{
			while (IsActive && ProcessTasks())
			{
				try
				{
					Task.Delay(1000, Cancel.Token).GetAwaiter().GetResult();
				}
				catch (Exception ex)
				{
					MessageHandler.SendMessage(nameof(Tasks), ex.ToString(), MessageHandler.MessageType.Error);
				}
			}
		}

		/// <summary>
		/// Eldönti, hogy a megadott időpont a megadott időintervallumba esik-e
		/// </summary>
		/// <param name="time">Vizsgálandó idő</param>
		/// <param name="interval">Időintervallum, ami között vizsgálunk</param>
		/// <returns>Benne van-e</returns>
		/// <exception cref="ArgumentException">Hibás időintervallum megadása esetén</exception>
		private static bool TimeIsBetween(TimeSpan time, string interval)
		{
			// interval pl: 8:00-11:00, vagy 20:00-05:00
			string[] timeParts = interval.Split('-');

			if (timeParts.Length != 2)
			{
				throw new ArgumentException($"Hibás időintervallum: {interval}");
			}

			var start = DateTime.ParseExact(timeParts[0], "H:mm", CultureInfo.InvariantCulture).TimeOfDay;
			var stop = DateTime.ParseExact(timeParts[1], "H:mm", CultureInfo.InvariantCulture).TimeOfDay;

			return start <= stop
				? time >= start && time < stop  // ha napon belüli időszak van megadva, mint pl: 8:00-11:00
				: time >= start || time < stop; // ha napon átlógó időszak van megadva, mint pl: 20:00-05:00
		}

		/// <summary>
		/// Beadott szabály és időpont alapján visszaadja, hogy az időpont preferált-e
		/// </summary>
		/// <param name="rule">Kategória szabály</param>
		/// <param name="time">Időpont</param>
		/// <exception cref="ArgumentException">Hibás szabály esetén</exception>
		/// <returns>Preferált-e a <paramref name="time"/> időpont</returns>
		private static string GetTimeCategory(string rule, TimeSpan time)
		{
			// pl: 2*8:00-11:00(-);11:00-18:00(+);20:00-05:00(+)
			// 2: időmegadás típusa. 8-11, és 15-18 között tiltott időszak, 20-05 között preferált időszak, többi időszak normál
			// ha üres stringet adunk be, akkor "+"-t

			// regkif: (?<type>\d+)\*(?<intervals>(?:\d{1,2}\:\d{2}\-\d{1,2}\:\d{2}\(.\))(?:\;(?:\d{1,2}\:\d{2}\-\d{1,2}\:\d{2}\(.\)))*)

			string result = string.Empty;

			if (rule.IsNullOrWhitespace())
			{
				result = "+";
			}

			if (rule.Equals("2*"))
			{
				return result;
			}

			var parts = Regex.Match(rule, @"(?<type>\d+)\*(?<intervals>(?:\d{1,2}\:\d{2}\-\d{1,2}\:\d{2}\(.\))(?:\;(?:\d{1,2}\:\d{2}\-\d{1,2}\:\d{2}\(.\)))*)");

			// if (!parts.Groups["type"].Value.IsNullOrWhitespace() || !parts.Groups["intervals"].Value.IsNullOrWhitespace())
			if (!rule.TrimEnd(';').Equals(parts.Groups[0].Value))
			{
				throw new ArgumentException($@"Hibás kategória: {rule}", nameof(rule));
			}

			if (!parts.Groups["type"].Value.Equals("2"))
			{
				return result;
			}

			string[] intervals = parts.Groups["intervals"].Value.Split(';');

			foreach (string current in intervals)
			{
				if (!TimeIsBetween(time, Regex.Match(current, @"(.*)\(").Groups[1].Value))
				{
					continue;
				}

				if (!result.IsNullOrWhitespace())
				{
					throw new Exception($"Ütköző időszakok: \"{result}\", \"{current}\"");
				}

				result = current;
			}

			result = Regex.Match(result, @".*\((.)").Groups[1].Value;

			return result;
		}

		/// <summary>
		/// Megadott szabály alapján eldönti, hogy alkalmas-e a megadott időpont,
		/// határidőket is támogat.
		/// </summary>
		/// <param name="rule">Szabály (időinvervallumok)</param>
		/// <param name="time">Vizsgálandó időpont</param>
		/// <param name="deadline1">Első határidő</param>
		/// <param name="deadline2">Második határidő</param>
		/// <returns></returns>
		private static bool TimeIsAcceptable(string rule, DateTime time, DateTime deadline1, DateTime deadline2)
		{
			string tc = GetTimeCategory(rule, time.TimeOfDay);

			//ha az időszak preferált
			switch (tc)
			{
				case "+":
					return true;
				case "-":
					return time > deadline1 && time > deadline2; // mindkét határidőt túlléptük-e?
				default:
					return time > deadline1 || time > deadline2; // az első határidőt túlléptük-e?
			}
		}

		/// <summary>
		/// Visszaadja, hogy egy adott task dogozhat-e az aktuális időpillanatban.
		/// </summary>
		/// <param name="prefWorktimeName">Taskhoz tartozó azonosító</param>
		/// <param name="deadlines">Határidők</param>
		/// <exception cref="ArgumentException">Hibásan megadott határidők esetén</exception>
		/// <returns></returns>
		private static bool TimeIsAcceptable(string prefWorktimeName, (DateTime firstDeadline, DateTime secondDeadline)? deadlines)
		{
			if (prefWorktimeName.IsNullOrWhitespace())
			{
				return true;
			}

			string rule = ConfigSync.Get(prefWorktimeName);

			if (rule.IsNullOrWhitespace())
			{
				rule = ConfigSync.Get("PREFEREDWORKTIME_DEFAULT");
			}

			if (rule.IsNullOrWhitespace())
			{
				return true;
			}

			var (firstDeadline, secondDeadline) = deadlines ?? default;

			return TimeIsAcceptable(rule, DateTime.Now, firstDeadline, secondDeadline);
		}

		public static void SetTaskDetailStatus(T_TASKS_DETAILS taskDetails, int? newStatus)
		{
			if (taskDetails is null)
			{
				return;
			}

			LXDb.New("UPDATE t_tasks_details SET td_status = @td_status WHERE td_id = @td_id;")
				.AddParam("td_status", newStatus)
				.AddParam("td_id", taskDetails.TD_ID)
				.ExecSql();
		}
	}
}
