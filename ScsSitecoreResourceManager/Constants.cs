﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager
{
	public static class Constants
	{
		public static readonly HashSet<string> BinaryBlacklist = new HashSet<string>(new[]{
			"Antlr3.Runtime",
			"ComponentArt.Web.UI",
			"DotNetOpenAuth",
			"EcmaScript.NET",
			"Facebook",
			"FacebookAPI",
			"Google.Apis.Authentication.OAuth2",
			"Google.Apis",
			"Google.Apis.Plus.v1",
			"Hammock.ClientProfile",
			"HtmlAgilityPack",
			"ICSharpCode.SharpZipLib",
			"Iesi.Collections",
			"ITHit.WebDAV.Server",
			"Lucene.Net.Contrib.Analyzers",
			"Lucene.Net.Contrib.Core",
			"Lucene.Net.Contrib.FastVectorHighlighter",
			"Lucene.Net.Contrib.Highlighter",
			"Lucene.Net.Contrib.Memory",
			"Lucene.Net.Contrib.Queries",
			"Lucene.Net.Contrib.Regex",
			"Lucene.Net.Contrib.SimpleFacetedSearch",
			"Lucene.Net.Contrib.Snowball",
			"Lucene.Net.Contrib.SpellChecker",
			"Lucene.Net",
			"MarkdownSharp",
			"Microsoft.AspNet.WebApi.Extensions.Compression.Server",
			"Microsoft.Extensions.DependencyInjection.Abstractions",
			"Microsoft.Extensions.DependencyInjection",
			"Microsoft.OData.Core",
			"Microsoft.OData.Edm",
			"Microsoft.Practices.ServiceLocation",
			"Microsoft.Spatial",
			"Microsoft.Web.Infrastructure",
			"MongoDB.Bson",
			"MongoDB.Driver",
			"Mvp.Xml",
			"Netbiscuits.OnPremise",
			"Newtonsoft.Json",
			"OAuthLinkedIn",
			"protobuf-net",
			"RadEditor.Net2",
			"RazorGenerator.Mvc",
			"Sitecore.Abstractions",
			"Sitecore.Analytics.Aggregation",
			"Sitecore.Analytics.Automation.Aggregation",
			"Sitecore.Analytics.Automation",
			"Sitecore.Analytics.Automation.MongoDB",
			"Sitecore.Analytics.Core",
			"Sitecore.Analytics.DataAccess",
			"Sitecore.Analytics",
			"Sitecore.Analytics.Model",
			"Sitecore.Analytics.MongoDB",
			"Sitecore.Analytics.OmniChannel",
			"Sitecore.Analytics.Outcome",
			"Sitecore.Analytics.Processing",
			"Sitecore.Analytics.RobotDetection",
			"Sitecore.Analytics.Sql",
			"Sitecore.Apps.Loader",
			"Sitecore.Buckets.Client",
			"Sitecore.Buckets",
			"Sitecore.CES.DeviceDetection",
			"Sitecore.CES.DeviceDetection.Rules",
			"Sitecore.CES",
			"Sitecore.CES.GeoIp",
			"Sitecore.CES.GeoIp.LegacyLocation",
			"Sitecore.Cintel.Client",
			"Sitecore.Cintel",
			"Sitecore.Client",
			"Sitecore.Client.LicenseOptions",
			"Sitecore.Cloud.Nexus",
			"Sitecore.Cloud.RestClient",
			"Sitecore.Commerce",
			"Sitecore.Commerce.ExperienceAnalytics",
			"Sitecore.Commerce.ExperienceProfile",
			"Sitecore.ContentSearch.Analytics.Client",
			"Sitecore.ContentSearch.Analytics",
			"Sitecore.ContentSearch.Azure",
			"Sitecore.ContentSearch.Client",
			"Sitecore.ContentSearch",
			"Sitecore.ContentSearch.Linq",
			"Sitecore.ContentSearch.Linq.Lucene",
			"Sitecore.ContentSearch.Linq.Solr",
			"Sitecore.ContentSearch.LuceneProvider",
			"Sitecore.ContentSearch.SolrProvider",
			"Sitecore.ContentTesting",
			"Sitecore.ContentTesting.Intelligence",
			"Sitecore.ContentTesting.Model",
			"Sitecore.ContentTesting.Mvc",
			"Sitecore.ControlPanel",
			"Sitecore.ExperienceAnalytics.Aggregation",
			"Sitecore.ExperienceAnalytics.Api",
			"Sitecore.ExperienceAnalytics.Client",
			"Sitecore.ExperienceAnalytics.Core",
			"Sitecore.ExperienceAnalytics",
			"Sitecore.ExperienceAnalytics.ReAggregation",
			"Sitecore.ExperienceAnalytics.Reduce",
			"Sitecore.ExperienceContentManagement.Administration",
			"Sitecore.ExperienceEditor",
			"Sitecore.ExperienceEditor.Speak",
			"Sitecore.ExperienceEditor.Speak.Ribbon",
			"Sitecore.ExperienceExplorer.Business",
			"Sitecore.ExperienceExplorer.Web",
			"Sitecore.FXM.Client",
			"Sitecore.FXM",
			"Sitecore.FXM.Service",
			"Sitecore.FXM.Speak",
			"Sitecore.ItemWebApi",
			"Sitecore.Kernel",
			"Sitecore.LaunchPad",
			"Sitecore.ListManagement.Analytics",
			"Sitecore.ListManagement.Client",
			"Sitecore.ListManagement.ContentSearch",
			"Sitecore.ListManagement",
			"Sitecore.ListManagement.Services",
			"Sitecore.Logging.Client",
			"Sitecore.Logging",
			"Sitecore.Marketing.Campaigns.Client",
			"Sitecore.Marketing.Campaigns.Services",
			"Sitecore.Marketing.Client",
			"Sitecore.Marketing.Core",
			"Sitecore.Marketing.Definitions.MarketingAssets.Repositories",
			"Sitecore.Marketing",
			"Sitecore.Marketing.Search",
			"Sitecore.Marketing.Taxonomy",
			"Sitecore.Mvc.Analytics",
			"Sitecore.Mvc.DeviceSimulator",
			"Sitecore.Mvc",
			"Sitecore.Mvc.ExperienceEditor",
			"Sitecore.Nexus",
			"Sitecore.NVelocity",
			"Sitecore.Oracle",
			"Sitecore.PathAnalyzer.Client",
			"Sitecore.PathAnalyzer",
			"Sitecore.PathAnalyzer.Services",
			"Sitecore.Publishing.WebDeploy",
			"Sitecore.Security.AntiCsrf",
			"Sitecore.SegmentBuilder",
			"Sitecore.SequenceAnalyzer",
			"Sitecore.Services.Client",
			"Sitecore.Services.Core",
			"Sitecore.Services.Infrastructure",
			"Sitecore.Services.Infrastructure.Sitecore",
			"Sitecore.SessionProvider",
			"Sitecore.SessionProvider.Memory",
			"Sitecore.SessionProvider.MongoDB",
			"Sitecore.SessionProvider.Redis",
			"Sitecore.SessionProvider.Sql",
			"Sitecore.Shell.MarketingAutomation",
			"Sitecore.Social.Api",
			"Sitecore.Social.Client.Api",
			"Sitecore.Social.Client.Common",
			"Sitecore.Social.Client",
			"Sitecore.Social.Client.Domain",
			"Sitecore.Social.Client.Mvc",
			"Sitecore.Social.Client.PublishingService",
			"Sitecore.Social.Client.Speak",
			"Sitecore.Social.Configuration",
			"Sitecore.Social",
			"Sitecore.Social.Domain",
			"Sitecore.Social.ExperienceProfile.Client",
			"Sitecore.Social.Facebook.Api",
			"Sitecore.Social.Facebook.Client",
			"Sitecore.Social.Facebook.Client.Mvc",
			"Sitecore.Social.Facebook",
			"Sitecore.Social.GooglePlus.Client",
			"Sitecore.Social.GooglePlus.Client.Mvc",
			"Sitecore.Social.GooglePlus",
			"Sitecore.Social.Infrastructure",
			"Sitecore.Social.Infrastructure.Logging",
			"Sitecore.Social.Installation",
			"Sitecore.Social.Klout.Api",
			"Sitecore.Social.Klout.Client",
			"Sitecore.Social.LinkedIn.Client",
			"Sitecore.Social.LinkedIn.Client.Mvc",
			"Sitecore.Social.LinkedIn",
			"Sitecore.Social.NetworkProviders",
			"Sitecore.Social.SitecoreAccess",
			"Sitecore.Social.SocialMarketer.Api",
			"Sitecore.Social.SocialMarketer.Client",
			"Sitecore.Social.SocialMarketer",
			"Sitecore.Social.Twitter.Api",
			"Sitecore.Social.Twitter.Client",
			"Sitecore.Social.Twitter.Client.Mvc",
			"Sitecore.Social.Twitter",
			"Sitecore.Speak.Applications",
			"Sitecore.Speak.Client",
			"Sitecore.Speak.Components",
			"Sitecore.Speak.Components.Guidance",
			"Sitecore.Speak.Components.Web",
			"Sitecore.Speak.ItemWebApi",
			"Sitecore.Speak.Web",
			"Sitecore.Update",
			"Sitecore.Web",
			"Sitecore.Xdb.Client",
			"Sitecore.Xdb.Configuration",
			"Sitecore.Zip",
			"SolrNet",
			"StackExchange.Redis.StrongName",
			"Stimulsoft.Base",
			"Stimulsoft.Database",
			"Stimulsoft.Report",
			"Stimulsoft.Report.Web",
			"Stimulsoft.Report.WebDesign",
			"System.IdentityModel.Tokens.Jwt",
			"System.Net.Http.Extensions.Compression.Core",
			"System.Net.Http.Formatting",
			"System.Web.Cors",
			"System.Web.Helpers",
			"System.Web.Http.Cors",
			"System.Web.Http",
			"System.Web.Http.WebHost",
			"System.Web.Mvc",
			"System.Web.OData",
			"System.Web.Optimization",
			"System.Web.Razor",
			"System.Web.WebPages.Deployment",
			"System.Web.WebPages",
			"System.Web.WebPages.Razor",
			"Telerik.Web.UI",
			"Telerik.Web.UI.Skins",
			"TweetSharp",
			"WebActivatorEx",
			"WebGrease",
			"Yahoo.Yui.Compressor"});
	}
}
