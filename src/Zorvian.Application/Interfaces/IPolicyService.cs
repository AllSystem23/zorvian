using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPolicyService
{
    Task IngestDocumentAsync(string title, string content);
}
