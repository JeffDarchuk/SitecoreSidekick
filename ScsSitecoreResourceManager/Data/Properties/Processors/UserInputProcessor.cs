using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Processors
{
	public class UserInputProcessor : PropertyProcessor
	{
		public override void ProcessFile(StringBuilder fileContents, string input, ConfigurationNode Node)
		{
			if (Node.Processor == "UserInput")
			{
				fileContents.Replace(Node.Name, input);
			}
		}
	}
}
