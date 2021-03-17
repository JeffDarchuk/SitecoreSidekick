using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Web;
using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.Services
{
	public class IconService : IIconService
	{
		public Dictionary<string, ZipArchiveEntry> Images { get; set; } = new Dictionary<string, ZipArchiveEntry>();

		public IconService()
		{
			string iconRoot = HttpContext.Current.Server.MapPath("~/") + "\\sitecore\\shell\\Themes\\Standard";
			foreach (string file in Directory.GetFiles(iconRoot, "*.zip"))
			{
				var zip = ZipFile.OpenRead(file);
				foreach (var entry in zip.Entries)
				{
					if (entry.FullName.EndsWith(".png"))
					{
						Images[entry.FullName] = entry;
					}

				}
			}
		}
	}
}
