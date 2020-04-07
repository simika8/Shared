using System;

namespace LX.Shared.WinService
{
	/// <summary>
	/// Egyszerű task, amely szabadon használható
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	interface ITask
	{
		void Start();

		void Stop();
	}

	/// <summary>
	/// Ciklikus task, amelyek sorban, egymás után futnak le
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	interface ILoopTask : ITask
	{
		void TaskAction(TaskActionParams taskActionParams);
	}

	/// <summary>
	/// Ciklikus task, időzíthető végrehajtási határidővel
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	interface ILoopTaskDL : ILoopTask
	{
		/// <summary>
		/// Visszaadja az első és második határidő időpontját
		/// </summary>
		/// <param name="forced">Meg "kapcsolódikozták" a taskot</param>
		/// <returns></returns>
		(DateTime firstDeadline, DateTime secondDeadline)? GetDeadlines(bool forced);
	}
}
