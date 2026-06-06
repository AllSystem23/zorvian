using FluentAssertions;
using Zorvian.Core.Domain;
using Zorvian.Core.Enums;

namespace Zorvian.Tests.Services;

public sealed class WarrantyStateMachineTests
{
    [Theory]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.PendingReview)]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.Rejected)]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.PendingReview, WarrantyStatus.InDiagnosis)]
    [InlineData(WarrantyStatus.PendingReview, WarrantyStatus.Rejected)]
    [InlineData(WarrantyStatus.PendingReview, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.SentToWorkshop)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.InRepair)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.PendingParts)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.ReplacementApproved)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.Rejected)]
    [InlineData(WarrantyStatus.InDiagnosis, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.SentToWorkshop, WarrantyStatus.InRepair)]
    [InlineData(WarrantyStatus.SentToWorkshop, WarrantyStatus.PendingParts)]
    [InlineData(WarrantyStatus.SentToWorkshop, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.InRepair, WarrantyStatus.PendingParts)]
    [InlineData(WarrantyStatus.InRepair, WarrantyStatus.Repaired)]
    [InlineData(WarrantyStatus.InRepair, WarrantyStatus.ReplacementApproved)]
    [InlineData(WarrantyStatus.InRepair, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.PendingParts, WarrantyStatus.InRepair)]
    [InlineData(WarrantyStatus.PendingParts, WarrantyStatus.ReplacementApproved)]
    [InlineData(WarrantyStatus.PendingParts, WarrantyStatus.Rejected)]
    [InlineData(WarrantyStatus.PendingParts, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.Repaired, WarrantyStatus.ReadyForDelivery)]
    [InlineData(WarrantyStatus.Repaired, WarrantyStatus.ReplacementApproved)]
    [InlineData(WarrantyStatus.ReplacementApproved, WarrantyStatus.ReadyForDelivery)]
    [InlineData(WarrantyStatus.ReadyForDelivery, WarrantyStatus.Delivered)]
    [InlineData(WarrantyStatus.Delivered, WarrantyStatus.Closed)]
    public void CanTransition_ValidTransition_ReturnsTrue(WarrantyStatus from, WarrantyStatus to)
    {
        var result = WarrantyStateMachine.CanTransition(from, to);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.InRepair)]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.Delivered)]
    [InlineData(WarrantyStatus.Registered, WarrantyStatus.Closed)]
    [InlineData(WarrantyStatus.PendingReview, WarrantyStatus.Delivered)]
    [InlineData(WarrantyStatus.SentToWorkshop, WarrantyStatus.ReadyForDelivery)]
    [InlineData(WarrantyStatus.SentToWorkshop, WarrantyStatus.Delivered)]
    [InlineData(WarrantyStatus.Repaired, WarrantyStatus.Closed)]
    [InlineData(WarrantyStatus.ReadyForDelivery, WarrantyStatus.Closed)]
    [InlineData(WarrantyStatus.Delivered, WarrantyStatus.Cancelled)]
    [InlineData(WarrantyStatus.Closed, WarrantyStatus.Registered)]
    [InlineData(WarrantyStatus.Cancelled, WarrantyStatus.Registered)]
    public void CanTransition_InvalidTransition_ReturnsFalse(WarrantyStatus from, WarrantyStatus to)
    {
        var result = WarrantyStateMachine.CanTransition(from, to);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_TerminalStates_HaveNoOutgoingTransitions()
    {
        WarrantyStateMachine.CanTransition(WarrantyStatus.Rejected, WarrantyStatus.Registered).Should().BeFalse();
        WarrantyStateMachine.CanTransition(WarrantyStatus.Closed, WarrantyStatus.Registered).Should().BeFalse();
        WarrantyStateMachine.CanTransition(WarrantyStatus.Cancelled, WarrantyStatus.Registered).Should().BeFalse();
    }

    [Fact]
    public void EnsureCanTransition_ValidTransition_DoesNotThrow()
    {
        var act = () => WarrantyStateMachine.EnsureCanTransition(WarrantyStatus.Registered, WarrantyStatus.PendingReview);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanTransition_InvalidTransition_ThrowsInvalidWarrantyStateTransitionException()
    {
        var act = () => WarrantyStateMachine.EnsureCanTransition(WarrantyStatus.Registered, WarrantyStatus.Delivered);
        act.Should().Throw<InvalidWarrantyStateTransitionException>()
            .WithMessage("*Registered*Delivered*");
    }

    [Fact]
    public void GetAllowedTransitions_FromRegistered_ReturnsCorrectSet()
    {
        var allowed = WarrantyStateMachine.GetAllowedTransitions(WarrantyStatus.Registered);
        allowed.Should().BeEquivalentTo(new[]
        {
            WarrantyStatus.PendingReview,
            WarrantyStatus.Rejected,
            WarrantyStatus.Cancelled
        });
    }

    [Fact]
    public void GetAllowedTransitions_FromTerminalState_ReturnsEmptySet()
    {
        WarrantyStateMachine.GetAllowedTransitions(WarrantyStatus.Closed).Should().BeEmpty();
        WarrantyStateMachine.GetAllowedTransitions(WarrantyStatus.Cancelled).Should().BeEmpty();
        WarrantyStateMachine.GetAllowedTransitions(WarrantyStatus.Rejected).Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedTransitions_FromInDiagnosis_ContainsAllForwardPaths()
    {
        var allowed = WarrantyStateMachine.GetAllowedTransitions(WarrantyStatus.InDiagnosis);
        allowed.Should().Contain(new[]
        {
            WarrantyStatus.SentToWorkshop,
            WarrantyStatus.InRepair,
            WarrantyStatus.PendingParts,
            WarrantyStatus.ReplacementApproved,
            WarrantyStatus.Rejected,
            WarrantyStatus.Cancelled
        });
        allowed.Should().HaveCount(6);
    }
}
