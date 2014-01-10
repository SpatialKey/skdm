using System;
using System.Text;
using System.Security.Cryptography;

namespace skdm
{
	public class OAuth
	{
		protected static String oAuthHeader = "{\"alg\":\"SH256\"}";
		protected static String claimTemplate = "{{\"iss\": \"{0}\", \"prn\": \"{1}\", \"aud\": \"{2}\", \"exp\": \"{3}\", \"iat\": \"{4}\"}}";
		protected static DateTime utcStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// This method will return a JWT OAuth token for the passed user, org, and secret keys along with a time to live value (in seconds).
		/// </summary>
		/// <returns>The oAuth token.</returns>
		/// <param name="userAPIKey">User API key.</param>
		/// <param name="orgAPIKey">Org API key.</param>
		/// <param name="orgSecretKey">Org secret key.</param>
		/// <param name="ttl">Time to live in seconds, defaults to 60</param>
		public static String GetOAuthToken(String userAPIKey, String orgAPIKey, String orgSecretKey, int ttl = 60)
		{
			// create JWT Token
			StringBuilder token = new StringBuilder();

			// add header
			token.Append(UrlSafeBase64Encode(oAuthHeader));
			token.Append(".");

			// add JWT Claims Object
			Int64 currentUtcSeconds = CurrentUtcSeconds();
			String[] claimArray = new String[5];
			claimArray[0] = orgAPIKey;
			claimArray[1] = userAPIKey;
			claimArray[2] = "https://www.spatialkey.com";
			claimArray[3] = Convert.ToString(currentUtcSeconds + ttl);
			claimArray[4] = Convert.ToString(currentUtcSeconds);
			String payload = String.Format(claimTemplate, claimArray);
			token.Append(UrlSafeBase64Encode(payload));

			String encryptedPayload = HashMac(token.ToString(), orgSecretKey);
			token.Append(".").Append(encryptedPayload);

			return token.ToString();
		}

		/// <summary>
		/// Return a URL safe base64 encoding of the given string
		/// </summary>
		protected static String UrlSafeBase64Encode(String text)
		{
			return UrlSafeBase64Encode(Encoding.UTF8.GetBytes(text));
		}

		// pulled from https://code.google.com/p/google-api-dotnet-client/source/browse/Src/GoogleApis.Auth.DotNet4/OAuth2/ServiceAccountCredential.cs
		/// <summary>
		/// Return a URL safe base64 encoding of the given byte array
		/// </summary>
		protected static string UrlSafeBase64Encode(byte[] bytes)
		{
			return Convert.ToBase64String(bytes).Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');
		}

		/// <summary>
		/// Returns current seconds in UTC
		/// </summary>
		protected static Int64 CurrentUtcSeconds()
		{ 
			TimeSpan ts = (DateTime.UtcNow - utcStart);
			return Convert.ToInt64(ts.TotalSeconds);
		}

		/// <summary>
		/// Get the hash for the text using the secret key
		/// </summary>
		protected static String HashMac(String text, String secretKey)
		{
			HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
			hmac.Initialize();
			byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));

			return UrlSafeBase64Encode(hash);
		}
	}
}

