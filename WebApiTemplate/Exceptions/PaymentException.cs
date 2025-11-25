using System;

namespace WebApiTemplate.Exceptions
{
    /// <summary>
    /// Base exception for payment-related errors
    /// </summary>
    public class PaymentException : Exception
    {
        public PaymentException() : base()
        {
        }

        public PaymentException(string message) : base(message)
        {
        }

        public PaymentException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}

