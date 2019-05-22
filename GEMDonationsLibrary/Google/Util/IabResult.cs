namespace AndroidDonationsLibrary.Google.Util
{

	/// <summary>
	/// Represents the result of an in-app billing operation.
	/// A result is composed of a response code (an integer) and possibly a
	/// message (String). You can get those by calling
	/// <seealso cref="#getResponse"/> and <seealso cref="#getMessage()"/>, respectively. You
	/// can also inquire whether a result is a success or a failure by
	/// calling <seealso cref="#isSuccess()"/> and <seealso cref="#isFailure()"/>.
	/// </summary>
	public class IabResult
	{
		private int mResponse;
        private string mMessage;

		public IabResult(int response, string message)
		{
			mResponse = response;
			if (message == null || message.Trim().Length == 0)
			{
				mMessage = IabHelper.getResponseDesc(response);
			}
			else
			{
				mMessage = message + " (response: " + IabHelper.getResponseDesc(response) + ")";
			}
		}
		public int Response
		{
			get
			{
				return mResponse;
			}
		}
		public string Message
		{
			get
			{
				return mMessage;
			}
		}
		public bool Success
		{
			get
			{
				return mResponse == IabHelper.BILLING_RESPONSE_RESULT_OK;
			}
		}
		public bool Failure
		{
			get
			{
				return !Success;
			}
		}
		public override string ToString()
		{
			return "IabResult: " + Message;
		}
	}
}