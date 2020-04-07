using System;

namespace LX.Shared.WinService.Tasks
{
	[System.Diagnostics.DebuggerDisplay("{ " + nameof(TD_TASKTYPE) + "} ; {" + nameof(TD_TYPE) + "}")]
#if EXTERN
	public
#else
	internal
#endif
	class T_TASKS_DETAILS : IEquatable<T_TASKS_DETAILS>
	{
		public int TD_ID { get; set; }

		/// <summary>task azonosítója</summary>
		public int TD_TASKTYPE { get; set; }

		/// <summary>feladat altípusa</summary>
		public int TD_TYPE { get; set; }

		/// <summary>
		/// 0 - kész
		/// 1 - folyamatban
		/// </summary>
		public short TD_STATUS { get; set; }

		/// <summary> Opcionális felhasználói azonosító</summary>
		public int? TD_USERID { get; set; }
		public DateTime? TD_NEXTWORKTIME { get; set; }

		/// <summary>"relid"</summary>
		public int? TD_INPUT_ID { get; set; }

		/// <summary>tól dátum/idő</summary>
		public DateTime? TD_INPUT_TOL { get; set; }

		/// <summary>ig dátum/idő</summary>
		public DateTime? TD_INPUT_IG { get; set; }

		/// <summary>minden egyéb ide jöhet</summary>
		public string TD_INPUT_EGYEB { get; set; }
		public DateTime? TD_CHANGETIME { get; set; }

		public bool Equals(T_TASKS_DETAILS other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return TD_ID == other?.TD_ID;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return obj is T_TASKS_DETAILS other && Equals(other);
		}

		public override int GetHashCode()
			=> TD_ID;

		public static bool operator ==(T_TASKS_DETAILS left, T_TASKS_DETAILS right)
			=> Equals(left, right);

		public static bool operator !=(T_TASKS_DETAILS left, T_TASKS_DETAILS right)
			=> !Equals(left, right);
	}
}
