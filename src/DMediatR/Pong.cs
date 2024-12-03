namespace DMediatR
{
    /// <summary>
    /// Response to a Ping request. Echoes the Ping message and the ASPNETCORE_ENVIRONMENT of the responding Node if present.
    /// </summary>
    public class Pong
    {
        public string? Message { get; set; }
    }
}