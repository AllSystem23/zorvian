using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IPolicyService
{
    Task IngestDocumentAsync(string title, string content);
}
