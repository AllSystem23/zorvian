using Zorvian.Core.Entities;
using Xunit;

namespace Zorvian.Tests.Services;

public class CashMovementTests
{
    [Fact]
    public void CashMovement_Properties_ShouldBeSetCorrectly()
    {
        // ARRANGE & ACT
        var movement = new CashMovement
        {
            ApprovalStatus = "draft",
            DocumentReference = "DOC-001"
        };

        // ASSERT
        Assert.Equal("draft", movement.ApprovalStatus);
        Assert.Equal("DOC-001", movement.DocumentReference);
    }
}
