using System;
using System.IO;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Stream extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class StreamExt
	{
		/// <summary>
		/// Streamet másol egy másikba
		/// </summary>
		/// <param name="self">Honnan</param>
		/// <param name="destination">Hová</param>
		public static void CopyTo(this Stream self, Stream destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			byte[] buffer = new byte[0x4000];
			int bytesRead;

			long origPos = self.Position;

			self.Position = 0L;
			while ((bytesRead = self.Read(buffer, 0, 0x4000)) > 0)
			{
				destination.Write(buffer, 0, bytesRead);
			}

			self.Position = origPos;

			destination.Position = 0L;
		}

		/// <summary>
		/// Byte tömbbe másolja a stream tartalmát
		/// </summary>
		/// <returns>Kimeneti byte tömb</returns>
		public static byte[] ReadToEnd(this Stream self)
		{
			switch (self)
			{
				case MemoryStream stream:
					return stream.ToArray();
				default:
					using (var ms = new MemoryStream())
					{
						self.CopyTo(ms);
						return ms.ToArray();
					}
			}
		}
	}
}
