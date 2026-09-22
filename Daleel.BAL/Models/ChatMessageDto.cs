namespace Daleel.BAL.Models
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "model" (or "AI")
        public string Content { get; set; } = string.Empty;
    }
}
