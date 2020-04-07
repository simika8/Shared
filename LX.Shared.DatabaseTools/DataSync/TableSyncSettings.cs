using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Database.Extensions;
using LX.Common.Extensions;

namespace LX.Common.Database.DataSync
{
#if EXTERN
	public
#else
	internal
#endif
	delegate void CallbackDelegate(DateTime changeTime, int tableId);

#if EXTERN
	public
#else
	internal
#endif
	delegate Func<T, bool> FilterDelegate<T>(int[] deletedRows) where T : class, new();

#if EXTERN
	public
#else
	internal
#endif
	class TableSyncSettings<T> : ITableSyncSettings where T : class, new()
	{
		private readonly CallbackDelegate _syncCallback;
		private readonly FilterDelegate<T> _deleteFilter;
		private bool _disabled;

		/// <summary>
		/// A propertyt használd helyette!
		/// </summary>
		private int? _tableId;
		private DateTime? _oldCommitTime;
		private DateTime? _maxCommitTime;

		public string TableName { get; }
		public int TableId => _tableId ?? GetTableIdFromDb();

		public bool Enabled => !_disabled;

		public TableSyncSettings(string tableName, CallbackDelegate syncCallback, FilterDelegate<T> deleteFilter)
		{
			TableName = tableName;
			_syncCallback = syncCallback;
			_deleteFilter = deleteFilter;
			_disabled = true;
		}

		private void Finished() => _oldCommitTime = _maxCommitTime;

		private int GetTableIdFromDb()
		{
			if (_tableId.HasValue)
			{
				return _tableId.Value;
			}

			_tableId = LXDb.New("SELECT tableid FROM p_sync_tableid(@tablename);")
				.AddParam("tablename", TableName)
				.GetValues<int?>().FirstOrDefault();

			Debug.Assert(_tableId != null, "_tableId != null");
			return _tableId.Value;
		}

		public void RunCallback()
		{
			if (_syncCallback is null || _disabled)
			{
				return;
			}

			try
			{
#if DEBUG
				var watch = Stopwatch.StartNew();
#endif
				var minwt = GetMinWriteTime();
#if DEBUG
				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} GetMinWriteTime {watch.ElapsedMilliseconds} ms ({minwt})");
				watch.Restart();
#endif
				_syncCallback(minwt, TableId);
#if DEBUG
				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} _syncCallback {watch.ElapsedMilliseconds} ms");
				watch.Stop();
#endif
				Finished();
			}
			catch // (Exception ex)
			{
				// hiba van
			}
		}

		private DateTime GetMinWriteTime()
		{
			var res = LXDb.New("SELECT minwritetime, maxcommittime FROM p_sync_time(@tableid, @oldcommittime);")
				.AddParam("tableid", TableId)
				.AddParam("oldcommittime", _oldCommitTime)
				.GetValues<(DateTime? MinWriteTime, DateTime? MaxCommitTime)?>()
				.FirstOrDefault();

			var minWriteTime = res?.MinWriteTime ?? default;
			_maxCommitTime = res?.MaxCommitTime ?? minWriteTime;

			return minWriteTime;
		}

		public List<T> CallbackCore(IEnumerable<T> rows, (string fieldName, DateTime timestamp) changeTime, string selectSql, params (string param, object value)[] selectParams)
		{
			// ha nem volt semmi változás (p_sync_time.minwritetime = null), akkor nem kell frissíteni
			if (changeTime.timestamp == default)
			{
				Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} előző lekérdezéssel sikerült leszedni az összes változást, ezért most pihenek");
				return rows?.ToList();
			}

			var connectionProvider = new LXConnectionProvider();
			using var conn = connectionProvider.GetOpenConnection();
			using var trans = conn.BeginTransaction();

#if DEBUG
			var watch = Stopwatch.StartNew();
#endif

			const string sql = "SELECT recordid FROM t_sync_deletedrows WHERE tableid = @tableid AND deltime >= @deltime";

			int[] deletedRows = connectionProvider.Sql(sql)
				.AddParam("tableid", TableId)
				.AddParam("deltime", changeTime.timestamp)
				.GetValues<int>(trans);


#if DEBUG
			Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} deletedRows {watch.ElapsedMilliseconds} ms");
			watch.Restart();
#endif

			var newRows = connectionProvider.Sql(selectSql)
				.AddParam(changeTime.fieldName, changeTime.timestamp)
				.AddParamRange(selectParams)
				.GetObjects<T>(trans);

#if DEBUG
			Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} newRows {watch.ElapsedMilliseconds} ms");
#endif

			var result = newRows
				.Union(rows.Coalesce())
				.Where(_deleteFilter?.Invoke(deletedRows) ?? (d => true))
				.ToList();

#if DEBUG
			watch.Stop();
#endif

			trans.Rollback();

			return result;
		}

		public void Enable()
		{
			_disabled = false;
			RunCallback();
		}

		public void Disable() => _disabled = true;
	}
}
