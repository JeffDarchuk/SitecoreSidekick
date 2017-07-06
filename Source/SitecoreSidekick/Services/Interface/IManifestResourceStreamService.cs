using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitecoreSidekick.Services.Interface
{
	public interface IMainfestResourceStreamService
	{
		string GetManifestResourceText(string fileName);
		byte[] GetManifestResourceImage(string fileName, ImageFormat imageFormat);
	}
}
