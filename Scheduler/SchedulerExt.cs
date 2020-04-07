using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Database.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Scheduler
{
    public enum TaskType : long
    {
        #region Statements taskok
        StatementsExport = 15001,
        #endregion
    }

    /// <summary>
    /// Feladatütemező KEZDEMÉNY Statementshez Entity frameworkkel. Idővek kukázni kell majd, és készíteni egy általános, teljes körűen működőt helyette
    /// </summary>
    public static class SchedulerExt
    {
		/// <summary>
		/// Visszaadja, hogy kell e dolgozni az adott feladattal.
		/// </summary>
		public static bool NeedToWork<T>(this DbSet<T> schedulers, TaskType taskType) where T : class, IScheduler, new()
		{
			var task = schedulers.GetData(x => x.TaskType = taskType);

			if (!task.Active)
				return false;

			return true;
		}

		public static void SuccesfullRun<T>(this DbSet<T> schedulers, TaskType taskType) where T : class, IScheduler, new()
		{
			var task = schedulers.GetData(x => x.TaskType = taskType);

			task.LastWorkTime = DateTime.Now;

		}



	}

}
