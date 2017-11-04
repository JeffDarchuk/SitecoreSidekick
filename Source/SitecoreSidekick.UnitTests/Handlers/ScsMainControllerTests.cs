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

			var scsMainRegistration =  CreateInstance<ScsMainRegistration>("", "", "");
			GetSubstitute<IScsRegistrationService>().GetScsRegistration<ScsMainRegistration>().Returns(scsMainRegistration);
			GetSubstitute<IMainfestResourceStreamService>().GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>()).Returns(endTag);

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
			var scsMainRegistration = CreateInstance<ScsMainRegistration>("", "", "");
			GetSubstitute<IScsRegistrationService>().GetScsRegistration<ScsMainRegistration>().Returns(scsMainRegistration);
			GetSubstitute<IMainfestResourceStreamService>().GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>()).Returns(endTag);

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
			var scsMainController = CreateInstance<ScsMainController>();
			GetSubstitute<ISitecoreDataAccessService>().GetScsSitecoreItem(Arg.Any<string>()).Returns(new ScsSitecoreItem());

			await scsMainController.SelectedRelated(new ContentSelectedRelatedModel { SelectedIds = new List<string> { "Id" } });

			GetSubstitute<ISitecoreDataAccessService>().Received(1).GetScsSitecoreItem(Arg.Any<string>());
		}

		[Fact]
		public async Task SelectRelated_ServerDefined_GetsFromServer()
		{
			string server = "http://google.com";
			var scsMainController = CreateInstance<ScsMainController>();			
			GetSubstitute<ISitecoreDataAccessService>().GetScsSitecoreItem(Arg.Any<string>()).Returns(new ScsSitecoreItem());

			await scsMainController.SelectedRelated(new ContentSelectedRelatedModel { SelectedIds = new List<string> { "Id" }, Server = server});

			await GetSubstitute<IHttpClientService>().Received(1).Post(Arg.Any<string>(), Arg.Any<string>());
		}
	}
}
