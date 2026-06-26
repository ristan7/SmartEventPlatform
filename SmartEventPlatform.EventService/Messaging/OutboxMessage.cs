namespace SmartEventPlatform.EventService.Messaging
{
    public class OutboxMessage
    {
        public long Id { get; set; }
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// The RabbitMQ routing key that determines which queue this message is delivered to.
        /// Set at creation time in the controller so the OutboxMessagePublisher
        /// does not need any routing logic — it simply reads this value and publishes.
        /// </summary>
        public string RoutingKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}