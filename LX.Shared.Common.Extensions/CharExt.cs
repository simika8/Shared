using System;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Gyors char => byte konverzió
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class CharExt
	{
		private static readonly short[] s_charValues =
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
			-1, -1, -1, -1, -1, -1, -1,
			0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,	// nagybetűk
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1,
			0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F	// kisbetűk
		};

		/// <summary>
		/// Gyors char => byte konverzió
		/// </summary>
		/// <param name="self">Átváltandó karakter</param>
		/// <returns>Byte érték</returns>
		public static byte NToByte(this char self)
		{
			if (self < '0' || self > 'f')
			{
				throw new ArgumentException($"'{self}' érvénytelen karakter.", nameof(self));
			}

			short result = s_charValues[self - '0'];

			if (result < 0)
			{
				throw new ArgumentException($"'{self}' érvénytelen karakter.", nameof(self));
			}

			return (byte)result;
		}
	}
}
