using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace SitecoreSidekick.Handlers
{
	/// <summary>
	/// base handler for http requests to the SCS 
	/// </summary>
	public class ScsMainRegistration : ScsRegistration
	{
		internal static List<IScsRegistration> Sidekicks = new List<IScsRegistration>();
		private static readonly StringBuilder Js = new StringBuilder();
		private static readonly StringBuilder Css = new StringBuilder();
		private static bool _addedSelf = false;

		public override string Directive => string.Empty;
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string Icon => "";
		public override string Name => "Sitecore Sidekick";
		public override string ResourcesPath => "SitecoreSidekick.Resources";
		public override string CssStyle => "600px";
		public ScsMainRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			if (!_addedSelf)
			{
				Js.Insert(0, CompileEmbeddedResource(".js"));
				Css.Insert(0, CompileEmbeddedResource(".css"));
				_addedSelf = true;
			}
		}
		public static void RegisterSideKick(IScsRegistration sk, bool addSidekick = true)
		{
			if (addSidekick)
			{
				ScsController.Registration[sk.GetType()] = sk;
				ScsController.Registration[sk.Controller] = sk;
				Sidekicks.Add(sk);
			}
			Js.Insert(0, sk.CompileEmbeddedResource(".js"));
			Css.Insert(0, sk.CompileEmbeddedResource(".css"));
		}
		public string GetJs => Js.ToString();
		public string GetCss => Css.ToString();
		public List<IScsRegistration> GetSidekicks => Sidekicks;

		public override string Identifier => "scs";
		public override Type Controller => typeof(ScsMainController);
	}
}
