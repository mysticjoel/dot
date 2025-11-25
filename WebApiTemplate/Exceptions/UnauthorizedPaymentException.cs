using System;

namespace WebApiTemplate.Exceptions
{
    /// <summary>
    /// Exception thrown when user is not authorized to confirm payment
    /// </summary>
    public class UnauthorizedPaymentException : PaymentException
    {
        public UnauthorizedPaymentException() 
            : base("User is not authorized to confirm this payment")
        {
        }

        public UnauthorizedPaymentException(string message) : base(message)
        {
        }

        public UnauthorizedPaymentException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public UnauthorizedPaymentException(int userId, int expectedUserId) 
            : base($"User {userId} is not authorized. Only user {expectedUserId} can confirm this payment.")
        {
        }
    }
}

