using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaskControl
{
	/// <summary>
	/// Oldstruct táblákat tartalmazó Firebird adatbázis
	/// </summary>
	public interface ITaskControlContext
	{
		public DbSet<TaskControl_Head> TaskControl_Heads { get; set; }
		public DbSet<TaskControl_Item> TaskControl_Items { get; set; }
		public DbSet<TaskControl_TimeRange> TaskControl_TimeRanges { get; set; }

		public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry(object entity);

		public int SaveChanges();
	}

}
