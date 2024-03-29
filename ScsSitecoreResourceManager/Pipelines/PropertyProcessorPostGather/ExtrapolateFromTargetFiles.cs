﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager;

namespace Sidekick.SitecoreResourceManager.Pipelines.PropertyProcessorPostGather
{
	public class ExtrapolateFromTargetFiles
	{
		private const string NamespaceRegex = @"namespace[\s]*(.*)";
		public string Layers { get; set; } = "foundation,feature,project";
		public void Process(SitecoreResourceManagerArgs args)
		{
			if (args.TargetControllerPath != null)
			{

				var controllerContent = File.ReadAllText(args.TargetControllerPath);
				args["_CONTROLLERNAMESPACE_"] = Regex.Match(controllerContent, NamespaceRegex).Groups[1].Value;
				args["_LAYER_"] = GetLayer(args.TargetControllerPath);
				args["_PARENTPROJECTNAME_"] = Path.GetFileName(args.TargetControllerPath).Split(new []{"Controller"}, StringSplitOptions.RemoveEmptyEntries)[0];
			}

			if (args.TargetCsProjPath != null)
			{
				var csProjContent = File.ReadAllText(args.TargetCsProjPath);
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(csProjContent);
				XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(doc.NameTable);
				xmlnsManager.AddNamespace("def", doc.DocumentElement.NamespaceURI);
				doc.LoadXml(csProjContent);
				args["_ASSEMBLYNAME_"] = doc.SelectSingleNode("//def:AssemblyName", xmlnsManager)?.InnerText;
				args["_PROJECTROOTNAMESPACE_"] = doc.SelectSingleNode("//def:RootNamespace", xmlnsManager)?.InnerText;
				args["_OVERLAYTARGET_"] = Path.GetDirectoryName(args.TargetCsProjPath);
				if (args.TargetControllerPath == null)
				{
					args["_LAYER_"] = GetLayer(args.TargetCsProjPath);
					args["_PARENTPROJECTNAME_"] = args.TargetCsProjPath.Split('.')[2];
				}
			}
		}

		private string GetLayer(string targetPath)
		{
			string layer = "";
			int lastIndex = 0;
			foreach (string layerOption in Layers.Split(',').Select(x => x.ToLower()))
			{
				int tmp = targetPath.ToLower().LastIndexOf(layerOption, StringComparison.Ordinal);
				if (tmp > lastIndex)
				{
					layer = layerOption;
				}
			}
			return layer;
		}
	}
}
