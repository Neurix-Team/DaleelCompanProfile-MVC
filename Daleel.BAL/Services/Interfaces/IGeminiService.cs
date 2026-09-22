using Daleel.BAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Daleel.BAL.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> GetChatResponseAsync(string prompt, List<ChatMessageDto> history);
    }
}
