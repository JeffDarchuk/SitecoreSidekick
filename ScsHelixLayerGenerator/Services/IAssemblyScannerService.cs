using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Services
{
	public interface IAssemblyScannerService
	{
		IEnumerable<T> ScanForImplementsInterface<T>();

	}
}
