using System;

namespace WebApiTemplate.Exceptions
{
    /// <summary>
    /// Exception thrown when payment window has expired
    /// </summary>
    public class PaymentWindowExpiredException : PaymentException
    {
        public PaymentWindowExpiredException() 
            : base("Payment window has expired")
        {
        }

        public PaymentWindowExpiredException(string message) : base(message)
        {
        }

        public PaymentWindowExpiredException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public PaymentWindowExpiredException(DateTime expiryTime) 
            : base($"Payment window expired at {expiryTime:yyyy-MM-dd HH:mm:ss} UTC")
        {
        }
    }
}

