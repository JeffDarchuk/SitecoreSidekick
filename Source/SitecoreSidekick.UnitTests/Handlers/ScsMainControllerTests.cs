using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using NSubstitute;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Services.Interface;
using System.Web.Mvc;
using System.Web.Routing;
using NSubstitute.Extensions;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;
using Xunit;

namespace SitecoreSidekick.UnitTests.Handlers
{
	public class ScsMainControllerTests : TestBase
	{
		[Fact]
		public void Valid_InvalidTicket_RelogsIn()
		{
			string expectedTicketId = "Some Ticket Id";
			GetSubstitute<IAuthenticationService>().GetCurrentTicketId().Returns(expectedTicketId);
			var scsMainController = CreateInstance<ScsMainController>();

			scsMainController.Valid();

			GetSubstitute<IAuthenticationService>().Received(1).Relogin(expectedTicketId);
		}

		[Fact]
		public void Valid_ValidTicket_DoesNotRelogIn()
		{
			string expectedTicketId = "";
			GetSubstitute<IAuthenticationService>().GetCurrentTicketId().Returns(expectedTicketId);
			var scsMainController = CreateInstance<ScsMainController>();

			scsMainController.Valid();

			GetSubstitute<IAuthenticationService>().Received(0).Relogin(expectedTicketId);
		}

		[Fact]
		public void Valid_IsAuthenticated_ReturnsTrue()
		{
			GetSubstitute<IAuthenticationService>().IsAuthenticated.Returns(true);
			GetSubstitute<IJsonSerializationService>().SerializeObject(Arg.Any<object>()).Returns(o => o.Args()[0].ToString());
			var scsMainController = CreateInstance<ScsMainController>();

			var result = (ContentResult)scsMainController.Valid();

			result.Content.Should().Be(true.ToString());
		}

		[Fact]
		public void Valid_NotAuthenticated_ReturnsFalse()
		{
			GetSubstitute<IAuthenticationService>().IsAuthenticated.Returns(false);
			GetSubstitute<IJsonSerializationService>().SerializeObject(Arg.Any<object>()).Returns(o => o.Args()[0].ToString());
			var scsMainController = CreateInstance<ScsMainController>();

			var result = (ContentResult)scsMainController.Valid();

			result.Content.Should().Be(false.ToString());
		}

		[Fact]
		public void ScsMain_DesktopMode_AddsDesktopStyle()
		{
			string endTag = "</head>";
			string expectedStartTag = "<style>";
			GetSubstitute<IScsRegistrationService>().GetScsRegistration<ScsMainRegistration>().Returns(Substitute.For<ScsMainRegistration>("","",""));
			GetSubstitute<IMainfestResourceStreamService>().GetManifestResourceText(Arg.Any<string>()).Returns(endTag);

			var scsMainController = CreateInstance<ScsMainController>();
			scsMainController.ControllerContext = ContextSubstitute();
			scsMainController.Request.QueryString.Returns(new NameValueCollection {{"desktop", "true"}});
			
			var result = (ContentResult)scsMainController.ScsMain();

			result.Content.Should().NotStartWith(endTag);
			result.Content.Should().EndWith(endTag);
			result.Content.Should().StartWith(expectedStartTag);
		}

		[Fact]
		public void ScsMain_NotDesktopMode_DoesNotAddDesktopStyle()
		{
			string endTag = "<head>";
			string expectedStartTag = "<head>";
			GetSubstitute<IScsRegistrationService>().GetScsRegistration<ScsMainRegistration>().Returns(Substitute.For<ScsMainRegistration>("", "", ""));
			GetSubstitute<IMainfestResourceStreamService>().GetManifestResourceText(Arg.Any<string>()).Returns(endTag);

			var scsMainController = CreateInstance<ScsMainController>();
			scsMainController.ControllerContext = ContextSubstitute();
			scsMainController.Request.QueryString.Returns(new NameValueCollection { { "desktop", "false" } });

			var result = (ContentResult)scsMainController.ScsMain();

			result.Content.Should().EndWith(endTag);
			result.Content.Should().StartWith(expectedStartTag);
		}

		[Fact]
		public async Task SelectRelated_ServerNotDefined_GetsFromSitecoreDatabase()
		{
			var itemId = ID.NewID;
			var definition = new ItemDefinition(itemId, string.Empty, ID.Null, ID.Null);
			var data = new ItemData(definition, Language.Current, Sitecore.Data.Version.First, new FieldList());
			Item item = Substitute.For<Item>(itemId, data, Substitute.For<Database>());

			GetSubstitute<ISitecoreDataAccessService>().GetItem(Arg.Any<string>()).Returns(item);
			var scsMainController = CreateInstance<ScsMainController>();

			await scsMainController.SelectedRelated(new ContentSelectedRelatedModel { SelectedIds = new List<string> { "Id" } });

			GetSubstitute<ISitecoreDataAccessService>().GetItem(Arg.Any<string>()).Received(1);
		}

		private ControllerContext ContextSubstitute()
		{
			HttpRequestBase request = Substitute.For<HttpRequestBase>();
			HttpContextBase httpContext = Substitute.For<HttpContextBase>();
			httpContext.Request.Returns(request);
			ControllerContext controllerContext = Substitute.For<ControllerContext>();
			controllerContext.HttpContext.Returns(httpContext);
			return controllerContext;
		}
	}
}
