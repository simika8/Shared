using System;

namespace LX.Shared.WinService.Tasks
{
	/// <summary>
	/// T_TASKS tábla osztályként leképezve
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{ " + nameof(TS_TASKTYPE) + "} ; {" + nameof(TS_NAME) + "} ; {" + nameof(TS_AKTIV) + "}")]
	internal class T_TASKS : IEquatable<T_TASKS>
	{
		public int TS_ID { get; set; }

		/// <summary>
		/// lx_comm &amp; lx_oep (c# szolgáltatások)
		/// részletek lásd: C:\Develop\winlx\LX.Common\LXTask.cs
		/// </summary>
		public int TS_TASKTYPE { get; set; }

		/// <summary>task guin/logokban megjelenő neve</summary>
		public string TS_NAME { get; set; }

		/// <summary>aktív-e a task</summary>
		public string TS_AKTIV { get; set; }

		/// <summary>mikor futott utoljára, kapcsolódikra kattintva nullozódik</summary>
		public DateTime? TS_LASTWORKTIME { get; set; }

		/// <summary>részfeladatok lekérdezésének gyakorisága másodpercben</summary>
		public double? TS_FREQ { get; set; }

		/// <summary>
		/// null: utolsó futásnál nem történt hiba
		/// &gt; 0: utolsó futásnál hiba volt, ennyi mpt várunk, ts_freq-t helyettesíti
		/// </summary>
		public double? TS_ERRORWAITTIME { get; set; }

		/// <summary>
		/// t_settings.s_param, ha null vagy üres akkor bármikor dolgozhat
		/// pl.: PREFEREDWORKTIME_OEP
		/// </summary>
		public string TS_PREFWORKTIMENAME { get; set; }
		public DateTime? TS_CHANGETIME { get; set; }

		public bool Equals(T_TASKS other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return TS_ID == other?.TS_ID;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return obj is T_TASKS other && Equals(other);
		}

		public override int GetHashCode()
			=> TS_ID;

		public static bool operator ==(T_TASKS left, T_TASKS right)
			=> Equals(left, right);

		public static bool operator !=(T_TASKS left, T_TASKS right)
			=> !Equals(left, right);
	}
}
