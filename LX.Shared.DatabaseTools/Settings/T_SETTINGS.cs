using System;

namespace LX.Common.Database.Settings
{
	[System.Diagnostics.DebuggerDisplay("{" + nameof(S_PARAM) + "} = {" + nameof(S_VALUE) + "} ; {" + nameof(S_SZOVEG) + "}")]
	internal class T_SETTINGS : IEquatable<T_SETTINGS>
	{
		public int S_ID { get; set; }
		public string S_PARAM { get; set; }
		public string S_VALUE { get; set; }
		public string S_SZOVEG { get; set; }

		public bool Equals(T_SETTINGS other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return S_ID == other?.S_ID;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return obj is T_SETTINGS other && Equals(other);
		}

		public override int GetHashCode() => S_ID;

		public static bool operator ==(T_SETTINGS left, T_SETTINGS right)
			=> Equals(left, right);

		public static bool operator !=(T_SETTINGS left, T_SETTINGS right)
			=> !Equals(left, right);
	}
}
