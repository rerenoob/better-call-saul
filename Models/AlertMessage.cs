namespace better_call_saul.Models
{
    public enum AlertType
    {
        Success,
        Warning,
        Error,
        Info
    }

    public class AlertMessage
    {
        public string Message { get; set; } = string.Empty;
        public AlertType Type { get; set; }
        public bool Dismissible { get; set; } = true;
    }
}