using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Scheduler.OldStru
{
	[Table("T_TASKS")]
	public class Sched_Scheduler: Scheduler.Sched_Scheduler
	{
		/// <summary>
		/// ezt a DbContext OnModelCreating-ben meg kell hívni, ha régi struktúrába (T_SETTINGS) akrsz menteni
		/// </summary>
		/// <param name="modelBuilder"></param>
		public static void OnModelCreating(ModelBuilder modelBuilder)
		{
			//örökölt mezők mezőneveinek átállítása
			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.Id).HasColumnName("TS_ID");
			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.TaskType).HasColumnName("TS_TASKTYPE");
			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.Name).HasColumnName("TS_NAME");
			
			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.Active).HasColumnName("TS_AKTIV")
				.HasConversion(new BoolToStringConverter("F", "T"));
				/*.HasConversion(
					x => x ? "T" : "F",
					x => x == "T"
					);*/

			


			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.LastWorkTime).HasColumnName("TS_LASTWORKTIME");
			modelBuilder.Entity<Sched_Scheduler>().Property(x => x.Freq).HasColumnName("TS_FREQ");
		}
	}
}
