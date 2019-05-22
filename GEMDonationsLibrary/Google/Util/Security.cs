using Android.Text;
using Android.Util;
using Java.Security;
using Java.Security.Spec;
using System;
using System.Text;

namespace AndroidDonationsLibrary.Google.Util
{
	/// <summary>
	/// Security-related methods. For a secure implementation, all of this code
	/// should be implemented on a server that communicates with the
	/// application on the device. For the sake of simplicity and clarity of this
	/// example, this code is included here and is executed on the device. If you
	/// must verify the purchases on the phone, you should obfuscate this code to
	/// make it harder for an attacker to replace the code with stubs that treat all
	/// purchases as verified.
	/// </summary>
	public class Security
	{
		private const string TAG = "IABUtil/Security";

		private const string KEY_FACTORY_ALGORITHM = "RSA";
		private const string SIGNATURE_ALGORITHM = "SHA1withRSA";

		/// <summary>
		/// Verifies that the data was signed with the given signature, and returns
		/// the verified purchase. The data is in JSON format and signed
		/// with a private key. The data also contains the <seealso cref="PurchaseState"/>
		/// and product ID of the purchase. </summary>
		/// <param name="base64PublicKey"> the base64-encoded public key to use for verifying. </param>
		/// <param name="signedData"> the signed JSON string (signed, not encrypted) </param>
		/// <param name="signature"> the signature for the data, signed with the private key </param>
		public static bool verifyPurchase(string base64PublicKey, string signedData, string signature)
		{
			if (signedData == null)
			{
				Log.Error(TAG, "data is null");
				return false;
			}

			bool verified = false;
			if (!TextUtils.IsEmpty(signature))
			{
				IPublicKey key = Security.generatePublicKey(base64PublicKey);
				verified = Security.verify(key, signedData, signature);
				if (!verified)
				{
					Log.Warn(TAG, "signature does not match data.");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Generates a PublicKey instance from a string containing the
		/// Base64-encoded public key.
		/// </summary>
		/// <param name="encodedPublicKey"> Base64-encoded public key </param>
		/// <exception cref="IllegalArgumentException"> if encodedPublicKey is invalid </exception>
		public static IPublicKey generatePublicKey(string encodedPublicKey)
		{
			try
			{
                byte[] decodedKey = Array.ConvertAll(Base64.decode(encodedPublicKey), q => Convert.ToByte(q));
				KeyFactory keyFactory = KeyFactory.GetInstance(KEY_FACTORY_ALGORITHM);

				return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
			}
			catch (NoSuchAlgorithmException e)
			{
				throw new Java.Lang.Exception(e);
			}
			catch (InvalidKeySpecException e)
			{
				Log.Error(TAG, "Invalid key specification.");
				throw new System.ArgumentException(e.Message);
			}
			catch (Base64DecoderException e)
			{
				Log.Error(TAG, "Base64 decoding failed.");
				throw new System.ArgumentException(e.Message);
			}
		}

		/// <summary>
		/// Verifies that the signature from the server matches the computed
		/// signature on the data.  Returns true if the data is correctly signed.
		/// </summary>
		/// <param name="publicKey"> public key associated with the developer account </param>
		/// <param name="signedData"> signed data from server </param>
		/// <param name="signature"> server signature </param>
		/// <returns> true if the data and signature match </returns>
		public static bool verify(IPublicKey publicKey, string signedData, string signature)
		{
			Signature sig;
			try
			{
				sig = Signature.GetInstance(SIGNATURE_ALGORITHM);
				sig.InitVerify(publicKey);
				sig.Update(Encoding.Unicode.GetBytes(signedData));
                if (!sig.Verify(Array.ConvertAll(Base64.decode(signature), q => Convert.ToByte(q))))
				{
					Log.Error(TAG, "Signature verification failed.");
					return false;
				}
				return true;
			}
			catch (NoSuchAlgorithmException e)
			{
				Log.Error(TAG, "NoSuchAlgorithmException.");
			}
			catch (InvalidKeyException e)
			{
                Log.Error(TAG, "Invalid key specification.");
			}
			catch (SignatureException e)
			{
                Log.Error(TAG, "Signature exception.");
			}
			catch (Base64DecoderException e)
			{
                Log.Error(TAG, "Base64 decoding failed.");
			}
			return false;
		}
	}
}