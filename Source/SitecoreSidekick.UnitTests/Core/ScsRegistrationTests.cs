using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;
using Xunit;

namespace Sidekick.Core.UnitTests.Core
{
	public class ScsRegistrationTests : TestBase
	{
		#region GenericScsRegistration
		private class GenericScsRegistration : ScsRegistration
		{			
			public GenericScsRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
			{
			}

			public override string Identifier => "Generic";
			public override string Directive => "Generic";
			public override NameValueCollection DirectiveAttributes { get; set; } = new NameValueCollection{{"Generic", "Generic"}};
			public override string ResourcesPath => "Generic";
			public override Type Controller => typeof(GenericScsRegistration);
			public override string Icon => "Generic";
			public override string Name => "Generic";
			public override string CssStyle => "Generic";
		}
		#endregion

		[Fact]
		public void ApplicableSidekick_IsCurrentAdmin_ReturnsTrue()
		{
			GetSubstitute<IAuthorizationService>().IsCurrentUserAdmin.Returns(true);
			var scsRegistration = CreateInstance<GenericScsRegistration>("", "", "");

			var actual = scsRegistration.ApplicableSidekick();

			actual.Should().BeTrue();
		}

		[Fact]
		public void ApplicableSidekick_NotCurrentAdmin_IsAdminOnly_ReturnsFalse()
		{
			GetSubstitute<IAuthorizationService>().IsCurrentUserAdmin.Returns(false);
			var scsRegistration = CreateInstance<GenericScsRegistration>("","true","");

			var actual = scsRegistration.ApplicableSidekick();

			actual.Should().BeFalse();
		}

		[Fact]
		public void ApplicableSidekick_NotCurrentAdmin_NotAdminOnly_NoRoles_ReturnsTrue()
		{
			GetSubstitute<IAuthorizationService>().IsCurrentUserAdmin.Returns(false);
			var scsRegistration = CreateInstance<GenericScsRegistration>("", "", "");

			var actual = scsRegistration.ApplicableSidekick();

			actual.Should().BeTrue();
		}

		[Fact]
		public void ApplicableSidekick_NotCurrentAdmin_NotAdminOnly_IsInRole_ReturnsTrue()
		{
			string role = "Potato";
			GetSubstitute<IAuthorizationService>().IsCurrentUserAdmin.Returns(false);
			GetSubstitute<IAuthorizationService>().IsCurrentUserInRole(Arg.Any<IEnumerable<string>>()).Returns(true);
			var scsRegistration = CreateInstance<GenericScsRegistration>(role, "", "");

			var actual = scsRegistration.ApplicableSidekick();

			actual.Should().BeTrue();
		}

		[Fact]
		public void ApplicableSidekick_NotCurrentAdmin_NotAdminOnly_IsNotInRole_ReturnsFalse()
		{
			string role = "Potato";
			GetSubstitute<IAuthorizationService>().IsCurrentUserAdmin.Returns(false);
			GetSubstitute<IAuthorizationService>().IsCurrentUserInRole(Arg.Any<IEnumerable<string>>()).Returns(false);
			var scsRegistration = CreateInstance<GenericScsRegistration>(role, "", "");

			var actual = scsRegistration.ApplicableSidekick();

			actual.Should().BeFalse();
		}
	}
}
