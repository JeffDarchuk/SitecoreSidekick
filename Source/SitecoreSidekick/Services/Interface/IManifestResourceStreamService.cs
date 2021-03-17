using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.Core.Services.Interface
{
	public interface IMainfestResourceStreamService
	{
		string GetManifestResourceText(Type callingAssembly, string fileName, Func<string> onNotFound = null);
		byte[] GetManifestResourceImage(Type callingAssembly, string fileName, ImageFormat imageFormat, Func<byte[]> onNotFound = null);
		IEnumerable<string> GetManifestResourceNames(Type callingAssembly);
	}
}
