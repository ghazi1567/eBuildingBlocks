namespace eBuildingBlocks.Application.Events
{
    /// <summary>
    /// Configuration options for the event bus.
    /// </summary>
    public class EventBusOptions
    {
        /// <summary>
        /// Gets or sets the failure mode for event handlers.
        /// </summary>
        public EventHandlerFailureMode FailureMode { get; set; } = EventHandlerFailureMode.FailFast;
    }
    
    /// <summary>
    /// Defines how the event bus should handle handler failures.
    /// </summary>
    public enum EventHandlerFailureMode
    {
        /// <summary>
        /// Continue processing other handlers even if one fails.
        /// Errors are logged but don't stop execution.
        /// </summary>
        Continue,
        
        /// <summary>
        /// Stop processing on first handler failure.
        /// This is the default behavior.
        /// </summary>
        FailFast
    }
}
