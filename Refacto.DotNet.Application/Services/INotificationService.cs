namespace Refacto.DotNet.Controllers.Services
{
    // WARN: Should not be changed during the exercise
    public interface INotificationService
    {
        public void SendDelayNotification(int leadTime, string productName);

        public void SendOutOfStockNotification(string productName);

        public void SendExpirationNotification(string productName, DateTime expiryDate);
    }
}
