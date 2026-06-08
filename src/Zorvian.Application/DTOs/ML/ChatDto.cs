namespace Zorvian.Application.DTOs.ML;

public sealed class ChatRequestDto
{
    public string Question { get; set; } = string.Empty;
}

public sealed class ChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
}
