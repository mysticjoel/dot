using System;

namespace WebApiTemplate.Exceptions
{
    /// <summary>
    /// Exception thrown when confirmed payment amount doesn't match expected amount
    /// </summary>
    public class InvalidPaymentAmountException : PaymentException
    {
        public decimal ExpectedAmount { get; }
        public decimal ConfirmedAmount { get; }

        public InvalidPaymentAmountException() 
            : base("Confirmed payment amount does not match expected amount")
        {
        }

        public InvalidPaymentAmountException(string message) : base(message)
        {
        }

        public InvalidPaymentAmountException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public InvalidPaymentAmountException(decimal expectedAmount, decimal confirmedAmount) 
            : base($"Payment amount mismatch. Expected: {expectedAmount:C}, Confirmed: {confirmedAmount:C}")
        {
            ExpectedAmount = expectedAmount;
            ConfirmedAmount = confirmedAmount;
        }
    }
}

