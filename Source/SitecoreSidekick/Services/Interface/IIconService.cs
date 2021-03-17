using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Pipelines.GetAboutInformation;

namespace Sidekick.Core.Services.Interface
{
	public interface IIconService
	{
		Dictionary<string, ZipArchiveEntry> Images { get; set; }
	}
}
