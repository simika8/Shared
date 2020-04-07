using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using LX.Common;
using LX.Common.Database.DataSync;
using LX.Common.Database.Settings;
using LX.Common.EventHandlers;
using LX.Common.Extensions;
using LX.Shared.WinService.Tasks;

namespace LX.Shared.WinService
{
	[System.ComponentModel.DesignerCategory("")]
#if EXTERN
	public
#else
	internal
#endif
	class LXService : ServiceBase
	{
		protected bool ServiceMode { get; set; } = true;
		protected Func<int> LogLevel { get; set; }
		private static bool IsWin10 { get; } = Environment.OSVersion.Version.Major >= 10;

		protected void RegisterMainTables()
		{
			TableSync.RegisterTables(nameof(T_SETTINGS), nameof(T_TASKS), nameof(T_TASKS_DETAILS));

			// speciális eseménykezelők hozzáadása
			TableSync.AddEventHandler(ConfigSync.SyncCallback, ConfigSync.DeleteFilter);
			TableSync.AddEventHandler<T_TASKS>(TaskManager.SyncCallbackTasks, null);
			TableSync.AddEventHandler(TaskManager.SyncCallbackTasksDetails, TaskManager.TaskDetailsDeleteFilter);
		}

		public void RunInCmd(EventHandler<MessageHandler.MessageEventArgs> logger = null)
		{
			try
			{
				if (IsWin10)
				{
					ConsoleManager.EnableVtMode();
				}

				ServiceMode = false;

				MessageHandler.Message += logger ?? CommandlineLogger;

				OnStart(Environment.GetCommandLineArgs());

				Console.Title = $@"{ServiceName} - Press Escape to Exit";

				while (!(Console.KeyAvailable && Console.ReadKey(true).Key.Equals(ConsoleKey.Escape)))
				{
					Thread.Sleep(10);
				}

				OnStop();

				MessageHandler.Message -= logger ?? CommandlineLogger;

				if (IsWin10)
				{
					ConsoleManager.DisableVtMode();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Console.ReadKey();
			}
		}

		protected void CommandlineLogger(object sender, MessageHandler.MessageEventArgs e)
		{
			if (LogLevel is { } && e.Loglevel > LogLevel())
			{
				return;
			}

			string status;
			switch (e.MessageType)
			{
				case MessageHandler.MessageType.Error:
					status = IsWin10 ? "\x1B[1;31m[E" : "[E";
					break;
				case MessageHandler.MessageType.Warning:
					status = IsWin10 ? "\x1B[1;33m[W" : "[W";
					break;
				case MessageHandler.MessageType.Info:
				default:
					status = IsWin10 ? "\x1B[1;32m[I" : "[I";
					break;
			}

			string end = IsWin10 ? "\x1B[0m" : string.Empty;

			string senderName;

			switch (sender)
			{
				case string str:
					senderName = str;
					break;
				case TaskType tt:
					senderName = Enum.GetName(tt.GetType(), sender);
					break;
				default:
					senderName = sender?.GetDescription();
					break;
			}

			string message = (string.IsNullOrEmpty(senderName) ? string.Empty : $"{senderName}: ") + e.MessageText;

			Console.WriteLine($@"{status} {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ] {message}{end}");
		}

		protected void EventLogLogger(object sender, MessageHandler.MessageEventArgs e)
		{
			if (LogLevel is { } && e.Loglevel > LogLevel())
			{
				return;
			}

			EventLogEntryType status;
			switch (e.MessageType)
			{
				case MessageHandler.MessageType.Error:
					status = EventLogEntryType.Error;
					break;
				case MessageHandler.MessageType.Warning:
					status = EventLogEntryType.Warning;
					break;
				default:
				case MessageHandler.MessageType.Info:
					status = EventLogEntryType.Information;
					break;
			}

			string senderName;

			switch (sender)
			{
				case string str:
					senderName = str;
					break;
				case TaskType tt:
					senderName = Enum.GetName(tt.GetType(), sender);
					break;
				default:
					senderName = sender?.GetDescription();
					break;
			}

			string message = (string.IsNullOrEmpty(senderName) ? string.Empty : $"{senderName}: ") + e.MessageText;

			EventLog.WriteEntry(ServiceName, message, status);
		}
	}

#if EXTERN
	public
#else
	internal
#endif
	static class ServiceTools
	{

		public static void OnConsole<T>() where T : LXService, new()
		{
			var service = new T();
			service.RunInCmd();
		}
	}
}
