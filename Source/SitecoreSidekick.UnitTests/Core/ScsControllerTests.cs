using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FluentAssertions;
using NSubstitute;
using Sitecore.Mvc.Extensions;
using Sidekick.Core;
using Sidekick.Core.Handlers;
using Sidekick.Core.Services.Interface;
using Xunit;

namespace Sidekick.Core.UnitTests.Core
{
	public class ScsControllerTests : TestBase
	{
		#region GenericScsController
		private class GenericScsController : ScsController
		{
		}
		#endregion	

		[Theory]
		[InlineData(".scs")]
		[InlineData(".html")]
		[InlineData(".svg")]
		[InlineData(".js")]
		public void Resources_TextContent_GetsTextContent(string fileExtension)
		{
			var scsController = CreateInstance<GenericScsController>();			

			scsController.Resources($"FileName{fileExtension}");

			GetSubstitute<IMainfestResourceStreamService>().Received(1).GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>());
		}

		public static IEnumerable<object[]> ResourcesIMageContentTestCases
		{
			get
			{
				yield return new object[] { ".gif", ImageFormat.Gif};
				yield return new object[] { ".png", ImageFormat.Png };
				yield return new object[] { ".jpg", ImageFormat.Jpeg };
				yield return new object[] { ".bmp", ImageFormat.Bmp };
				yield return new object[] { ".emf", ImageFormat.Emf };
				yield return new object[] { ".ico", ImageFormat.Icon };
				yield return new object[] { ".tiff", ImageFormat.Tiff };
				yield return new object[] { ".wmf", ImageFormat.Wmf };
			}
		}

		[Theory]
		[MemberData(nameof(ResourcesIMageContentTestCases), MemberType = typeof(ScsControllerTests))]
		public void Resources_ImageContent_GetsImageContent(string imageExtension, ImageFormat imageFormat)
		{
			var scsController = CreateInstance<GenericScsController>();

			scsController.Resources($"FileName{imageExtension}");

			GetSubstitute<IMainfestResourceStreamService>().Received(1).GetManifestResourceImage(Arg.Any<Type>(), Arg.Any<string>(), imageFormat, Arg.Any<Func<byte[]>>());
		}

		[Fact]
		public void Resources_InvalidExtension_Returns404()
		{
			var scsController = CreateInstance<GenericScsController>();
			scsController.ControllerContext = ContextSubstitute();

			scsController.Resources("Unknown.fáke");
			scsController.Response.StatusCode.Should().Be(404);
		}

		[Fact]
		public void GetResource_ExistsInResourceCache_DoesNotReadFromManifestResource()
		{
			string fileName = "filename";
			string existingKey = "ExistingKey";
			var scsController = CreateInstance<GenericScsController>();
			ConcurrentDictionary<string, string> resourceCache = (ConcurrentDictionary<string,string>)typeof(ScsController).GetField("_resourceCache", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(scsController);
			resourceCache?.TryAdd(fileName, existingKey);

			string actual = scsController.GetResource(fileName);

			actual.Should().Be(existingKey);
			GetSubstitute<IMainfestResourceStreamService>().Received(0).GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>());
		}

		[Fact]
		public void GetResource_DoesNotExistInResourceCache_ReadsFromManifestResource()
		{
			string fileName = "filename";
			string existingKey = "ExistingKey";
			GetSubstitute<IMainfestResourceStreamService>().GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>()).Returns(existingKey);
			var scsController = CreateInstance<GenericScsController>();
			
			string actual = scsController.GetResource(fileName);

			actual.Should().Be(existingKey);
			GetSubstitute<IMainfestResourceStreamService>().Received(1).GetManifestResourceText(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<string>>());
		}
	}
}
