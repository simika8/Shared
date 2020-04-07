using System.IO;
using System.IO.Compression;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Tömörítő extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class CompressionExt
	{
		/// <summary>
		/// Byte tömb tömörítése GZip módszerrel
		/// </summary>
		/// <returns>Tömörített adatot tartalmazó byte tömb</returns>
		public static byte[] Compress(this byte[] self)
		{
			var ms = new MemoryStream();

			using (var gs = new GZipStream(ms, CompressionMode.Compress, true))
			{
				gs.Write(self, 0, self.Length);
			}

			ms.Position = 0L;
			return ms.ToArray();
		}

		/// <summary>
		/// Byte tömb kitömörítése GZip módszerrel
		/// </summary>
		/// <returns>Adatot tartalmazó byte tömb</returns>
		public static byte[] Decompress(this byte[] self)
		{
			var ms = new MemoryStream(self);
			try
			{
				using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
				{
					ms = null;
					return gzip.ReadToEnd();
				}
			}
			finally
			{
				ms?.Dispose();
			}
		}

#if NETFX_45
		/// <summary>
		/// Byte tömbből készít tömörített Zip filet
		/// </summary>
		/// <param name="self">Bemeneti byte tömb</param>
		/// <param name="dataFileName">Bemeneti adat fileneve a zipen belül</param>
		/// <returns>Zip file byte tömbben</returns>
		public static byte[] CreateZipFileFromData(this byte[] self, string dataFileName)
		{
			var ms = new MemoryStream();
			try
			{
				var res = ms;
				// a zipArchive elbontja a MemoryStream-et belül
				using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, true))
				{
					ms = null;

					var entry = zipArchive.CreateEntry(dataFileName, CompressionLevel.Optimal);

					using (var writer = new BinaryWriter(entry.Open()))
					{
						writer.Write(self);
					}
				}

				return res.ToArray();
			}
			finally
			{
				ms?.Dispose();
			}
		}
#endif
	}
}
