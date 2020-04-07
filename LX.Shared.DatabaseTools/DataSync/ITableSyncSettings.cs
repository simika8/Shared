namespace LX.Common.Database.DataSync
{
#if EXTERN
	public
#else
	internal
#endif
	interface ITableSyncSettings
	{
		bool Enabled { get; }
		string TableName { get; }
		int TableId { get; }
		void RunCallback();
	}
}
