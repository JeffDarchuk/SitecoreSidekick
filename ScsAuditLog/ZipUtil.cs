using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Shell.Framework.Commands;

namespace ScsAuditLog
{
	public static class ZipUtil
	{
		public static byte[] Zip(string str)
		{
			var bytes = Encoding.UTF8.GetBytes(str);
			return bytes;
			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream())
			{
				using (var gs = new GZipStream(mso, CompressionMode.Compress))
				{
					msi.CopyTo(gs);
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
					gs.CopyTo(mso);
				}
				return Encoding.UTF8.GetString(mso.ToArray());
			}
		}
	}
}
