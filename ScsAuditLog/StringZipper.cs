﻿using System.IO;
using System.IO.Compression;
using System.Text;

namespace Sidekick.AuditLog
{
	/// <summary>
	/// found here http://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
	/// </summary>
	public static class StringZipper
	{
		public static void CopyTo(Stream src, Stream dest)
		{
			byte[] bytes = new byte[4096];

			int cnt;

			while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
			{
				dest.Write(bytes, 0, cnt);
			}
		}

		public static byte[] Zip(string str)
		{
			var bytes = Encoding.UTF8.GetBytes(str);

			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream())
			{
				using (var gs = new GZipStream(mso, CompressionMode.Compress))
				{
					CopyTo(msi, gs);
				}

				return mso.ToArray();
			}
		}

		public static string Unzip(byte[] bytes)
		{
			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream())
			{
				using (var gs = new GZipStream(msi, CompressionMode.Decompress))
				{
					CopyTo(gs, mso);
				}

				return Encoding.UTF8.GetString(mso.ToArray());
			}
		}
	}
}
