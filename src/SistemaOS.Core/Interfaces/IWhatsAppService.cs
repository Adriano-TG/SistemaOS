using System.Threading.Tasks;

namespace SistemaOS.Core.Interfaces;

public record WhatsAppMessage(
    string PhoneNumber,
    string Message,
    string? MediaUrl = null);

public record WhatsAppResult(
    bool Success,
    string? MessageId = null,
    string? Error = null);

public interface IWhatsAppService
{
    Task<WhatsAppResult> SendTextAsync(string phoneNumber, string message);
    Task<WhatsAppResult> SendMediaAsync(string phoneNumber, string mediaUrl, string? caption = null);
    Task<bool> IsConnectedAsync();
}