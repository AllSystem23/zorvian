using Zorvian.Application.DTOs.ML;

namespace Zorvian.Application.Interfaces;

public interface IExpenseClassificationService
{
    ExpenseClassificationResponseDto Predict(string description, decimal amount);
}
