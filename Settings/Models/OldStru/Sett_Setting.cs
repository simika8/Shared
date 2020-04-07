using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using LX.Common.Extensions;
using Microsoft.EntityFrameworkCore;


namespace Settings.OldStru
{
	/// <summary>
	/// régi t_settings adattábla, hozzá tartozó OnModelCreating specilaitásokkal
	/// Azokat a mezőket, amiket a táblában tölteni kell, de kódból nem kell látni, privatere raktam
	/// A Value-t a blob S_SZOVEG mezőbe, és a S_VALUE mezőbe is berakja mindkettőből megtalálja
	/// </summary>
	[Table("T_SETTINGS")]
	public class Sett_Setting : Settings.Sett_Setting
	{
		private string? _value;

		//rövid régi settings mező. ha csak itt van adat, akkor ezt rakom valueba
		[Column("S_VALUE")]
		private string? S_VALUE { get; set; }

		[Column("S_TYPE")]
		private string? S_TYPE { get; set; } = "C";

		[Column("S_USERID")]
		private int S_USERID { get; set; } = -1;
		[Column("S_SZOVEG")]
		public new string? Value { get { return _value ?? S_VALUE; } set { S_VALUE = value?.Copy(0, 60); _value = value; } }

		/// <summary>
		/// ezt a DbContext OnModelCreating-ben meg kell hívni, ha régi struktúrába (T_SETTINGS) akrsz menteni
		/// </summary>
		/// <param name="modelBuilder"></param>
		public static void OnModelCreating(ModelBuilder modelBuilder)
		{
			//private mezőket is kezelje az EFCore!
			modelBuilder.Entity<Sett_Setting>().Property(typeof(string), "S_VALUE").HasColumnName("S_VALUE");
			modelBuilder.Entity<Sett_Setting>().Property(typeof(string), "S_TYPE").HasColumnName("S_TYPE");
			modelBuilder.Entity<Sett_Setting>().Property(typeof(int), "S_USERID").HasColumnName("S_USERID");

			//örökölt mezők mezőneveinek átállítása
			modelBuilder.Entity<Sett_Setting>().Property(x => x.Id).HasColumnName("S_ID");
			modelBuilder.Entity<Sett_Setting>().Property(x => x.Name).HasColumnName("S_PARAM");
			modelBuilder.Entity<Sett_Setting>().Property(x => x.ReferenceId).HasColumnName("REFERENCEID");
		}
	}
}
