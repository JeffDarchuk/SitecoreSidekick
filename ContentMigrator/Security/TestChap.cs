using System;
using System.Web;
using MicroCHAP;
using MicroCHAP.Server;

namespace ScsContentMigrator.Security
{
	/// <summary>
	/// Functionalities needed to be a server of CHAP-authenticated data over HTTP.
	/// Requires a persistent store of challenge values so we can avoid replay attacks.
	/// </summary>
	public class TestChap : IChapServer
	{
		private readonly ISignatureService _responseService;
		private readonly IChallengeStore _challengeStore;

		public TestChap(ISignatureService responseService, IChallengeStore challengeStore)
		{
			_responseService = responseService;
			_challengeStore = challengeStore;
		}

		public int TokenValidityInMs { get; set; } = 600000;

		public virtual string GetChallengeToken()
		{
			var token = Guid.NewGuid().ToString("N");

			_challengeStore.AddChallenge(token, TokenValidityInMs);

			return token;
		}

		public bool ValidateRequest(HttpRequestBase request)
		{
			return ValidateRequest(request, (IChapServerLogger)null);
		}

		public bool ValidateRequest(HttpRequestBase request, IChapServerLogger logger)
		{
			return ValidateRequest(request, null, logger);
		}

		public virtual bool ValidateRequest(HttpRequestBase request, Func<HttpRequestBase, SignatureFactor[]> factorParser)
		{
			return ValidateRequest(request, factorParser, null);
		}

		public virtual bool ValidateRequest(HttpRequestBase request, Func<HttpRequestBase, SignatureFactor[]> factorParser, IChapServerLogger logger)
		{
			// fallback headers are for compatibility with MicroCHAP 1.0 client implementations
			// See https://github.com/kamsar/MicroCHAP/issues/1 for why the change to different headers
			var authorize = request.Headers["X-MC-MAC"] ?? request.Headers["Authorization"];
			var challenge = request.Headers["X-MC-Nonce"] ?? request.Headers["X-Nonce"];

			if (authorize == null || challenge == null)
			{
				logger?.RejectedDueToMissingHttpHeaders();
				return false;
			}

			SignatureFactor[] factors = null;
			if (factorParser != null) factors = factorParser(request);

			return ValidateToken(challenge, authorize, request.Url.AbsoluteUri, logger, factors);
		}

		public virtual bool ValidateToken(string challenge, string response, string url, params SignatureFactor[] additionalFactors)
		{
			return ValidateToken(challenge, response, url, null, additionalFactors);
		}

		public virtual bool ValidateToken(string challenge, string response, string url, IChapServerLogger logger, params SignatureFactor[] additionalFactors)
		{
			if (!_challengeStore.ConsumeChallenge(challenge))
			{
				logger?.RejectedDueToInvalidChallenge(challenge, url);
				return false; // invalid or expired challenge
			}

			// we now know the challenge was valid. But what about the response?
			var localMacOfRequest = _responseService.CreateSignature(challenge, url, additionalFactors);

			if (localMacOfRequest.SignatureHash.Equals(response)) return true;

			logger?.RejectedDueToInvalidSignature(challenge, response, localMacOfRequest);

			return false;
		}
	}
}