using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Data.Properties
{
	public abstract class PropertyProcessor
	{
		public abstract void ProcessFile(StringBuilder fileContents, string input, ConfigurationNode node);
	}
}
