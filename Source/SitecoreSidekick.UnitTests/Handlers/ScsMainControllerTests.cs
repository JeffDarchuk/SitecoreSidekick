using FluentAssertions;
using NSubstitute;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Services;
using System.Web.Mvc;
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
		public void ScsMain_()
		{
			var scsMainController = CreateInstance<ScsMainController>();

			var result = (ContentResult)scsMainController.ScsMain();

			result.Content.Should().Be("Something");
		}
	}
}
