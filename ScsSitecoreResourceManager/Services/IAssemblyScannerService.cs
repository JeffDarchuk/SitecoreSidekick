using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Services
{
	public interface IAssemblyScannerService
	{
		IEnumerable<T> ScanForImplementsInterface<T>();

	}
}
