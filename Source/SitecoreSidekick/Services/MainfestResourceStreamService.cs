using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;

namespace SitecoreSidekick.Services
{
	public class MainfestResourceStreamService : IMainfestResourceStreamService
	{
		public string GetManifestResourceText(string fileName)
		{
			using (var stream = GetType().Assembly.GetManifestResourceStream(fileName))
			{
				if (stream == null) throw new ScsEmbeddedResourceNotFoundException();

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public byte[] GetManifestResourceImage(string fileName, ImageFormat imageFormat)
		{
			using (var stream = GetType().Assembly.GetManifestResourceStream(fileName))
			{
				if (stream == null) throw new ScsEmbeddedResourceNotFoundException();

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
	}
}
