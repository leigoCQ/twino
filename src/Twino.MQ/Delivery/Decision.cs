namespace Twino.MQ.Delivery
{
    /// <summary>
    /// Sendin Acknowledge decision for delivery decision
    /// </summary>
    public enum DeliveryAcknowledgeDecision : byte
    {
        /// <summary>
        /// Acknowledge message is not sent
        /// </summary>
        None,
        
        /// <summary>
        /// Acknowledge message is sent, even message is not saved or failed 
        /// </summary>
        Always,
        
        /// <summary>
        /// Acknowledge message is sent only if message save is successful
        /// </summary>
        IfSaved
    }
    
    /// <summary>
    /// Decision description for each step in message delivery
    /// </summary>
    public struct Decision
    {
        /// <summary>
        /// If true, operation will continue
        /// </summary>
        public bool Allow;

        /// <summary>
        /// If true, message will be saved.
        /// If message already saved, second save will be discarded.
        /// </summary>
        public bool SaveMessage;

        /// <summary>
        /// If true, message will be kept in front of the queue.
        /// Settings this value always true may cause infinity same message send operation.
        /// </summary>
        public bool KeepMessage;

        /// <summary>
        /// If true, server will send an acknowledge message to producer.
        /// Sometimes acknowledge is required after save operation instead of receiving ack from consumer.
        /// This can be true in similar cases.
        /// </summary>
        public DeliveryAcknowledgeDecision SendAcknowledge;

        public Decision(bool allow, bool save)
        {
            Allow = allow;
            SaveMessage = save;
            KeepMessage = false;
            SendAcknowledge = DeliveryAcknowledgeDecision.None;
        }

        public Decision(bool allow, bool save, bool keep, DeliveryAcknowledgeDecision sendAcknowledge)
        {
            Allow = allow;
            SaveMessage = save;
            KeepMessage = keep;
            SendAcknowledge = sendAcknowledge;
        }
    }
}