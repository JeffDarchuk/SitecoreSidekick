using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.Services
{
	public class MainfestResourceStreamService : IMainfestResourceStreamService
	{
		public string GetManifestResourceText(Type callingAssembly, string fileName, Func<string> onNotFound = null)
		{
			using (var stream = callingAssembly.Assembly.GetManifestResourceStream(fileName))
			{
				if (stream == null)
				{
					return onNotFound?.Invoke();
				}

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public byte[] GetManifestResourceImage(Type callingAssembly, string fileName, ImageFormat imageFormat, Func<byte[]> onNotFound = null)
		{
			using (var stream = callingAssembly.Assembly.GetManifestResourceStream(fileName))
			{
				if (stream == null)
				{
					return onNotFound?.Invoke();
				}

				using (var ms = new MemoryStream())
				{
					using (var bmp = new Bitmap(stream))
					{
						bmp.Save(ms, imageFormat);						
					}
					return ms.ToArray();
				}
			}
		}

		public IEnumerable<string> GetManifestResourceNames(Type callingAssembly)
		{
			return callingAssembly.Assembly.GetManifestResourceNames();
		}
	}
}
