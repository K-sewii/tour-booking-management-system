using DongNamTrip.Contracts;
using DongNamTrip.Modules.Itinerary;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public class ScheduleCheckerTests
{
    private static readonly DateOnly Start = new(2030, 1, 10);
    private static readonly TimeOnly Open = new(8, 0), Close = new(18, 0);

    private static StopPlan Stop(int id, int day = 1, TimeOnly? opens = null, TimeOnly? closes = null, int visit = 60) =>
        new(id, day, id, visit, opens ?? Open, closes ?? Close);

    private static SlotDetail Slot(int id, int dest, int startHour, int startMin, int endHour, int endMin, int dayOffset = 0, string status = ContentStatus.Published) =>
        new(id, id, "Hoạt động " + id, dest, Start.AddDays(dayOffset), new(startHour, startMin), new(endHour, endMin), status, 60);

    private static bool Has(ItineraryCheck check, string code) => check.Warnings.Any(warning => warning.Code == code);

    [Fact]
    public void NoConflictFits()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1), Stop(2)], [Slot(10, 1, 9, 0, 10, 0), Slot(11, 2, 11, 0, 12, 0)],
            [new(1, 10, null), new(2, 11, null)]);
        Assert.True(check.Fits);
        Assert.True(check.Complete);
        Assert.Empty(check.Warnings);
    }

    [Fact]
    public void OverlapAndShortGapAreWarned()
    {
        var checker = new ScheduleChecker(30);
        var overlap = checker.Check(Start, [Stop(1), Stop(2)], [Slot(10, 1, 9, 0, 10, 0), Slot(11, 2, 9, 30, 10, 30)], [new(1, 10, null), new(2, 11, null)]);
        Assert.True(Has(overlap, ScheduleWarningCodes.Overlap));
        Assert.False(overlap.Fits);
        var shortGap = checker.Check(Start, [Stop(1), Stop(2)], [Slot(10, 1, 9, 0, 10, 0), Slot(11, 2, 10, 15, 11, 0)], [new(1, 10, null), new(2, 11, null)]);
        Assert.True(Has(shortGap, ScheduleWarningCodes.Overlap));
        var enoughGap = checker.Check(Start, [Stop(1), Stop(2)], [Slot(10, 1, 9, 0, 10, 0), Slot(11, 2, 10, 30, 11, 0)], [new(1, 10, null), new(2, 11, null)]);
        Assert.False(Has(enoughGap, ScheduleWarningCodes.Overlap));
    }

    [Fact]
    public void DifferentDaysDoNotOverlap()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1, 1), Stop(2, 2)], [Slot(10, 1, 9, 0, 10, 0), Slot(11, 2, 9, 0, 10, 0, 1)], [new(1, 10, null), new(2, 11, null)]);
        Assert.True(check.Fits);
    }

    [Fact]
    public void WrongDateIsWarned()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1, 1)], [Slot(10, 1, 9, 0, 10, 0, 3)], [new(1, 10, null)]);
        Assert.True(Has(check, ScheduleWarningCodes.WrongDate));
    }

    [Fact]
    public void DuplicatePickIsWarned()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1)], [Slot(10, 1, 9, 0, 10, 0)], [new(1, 10, null), new(1, 10, "lặp")]);
        Assert.True(Has(check, ScheduleWarningCodes.Duplicate));
    }

    [Fact]
    public void InactiveAndMissingSlotsAreWarned()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1)], [Slot(10, 1, 9, 0, 10, 0, status: ContentStatus.Inactive)], [new(1, 10, null), new(1, 99, null)]);
        Assert.Equal(2, check.Warnings.Count(warning => warning.Code == ScheduleWarningCodes.Inactive));
        Assert.False(check.Fits);
    }

    [Fact]
    public void OutOfHoursIsWarned()
    {
        var check = new ScheduleChecker(30).Check(Start, [Stop(1)], [Slot(10, 1, 7, 0, 8, 30), Slot(11, 1, 17, 30, 18, 30)], [new(1, 10, null), new(1, 11, null)]);
        Assert.Equal(2, check.Warnings.Count(warning => warning.Code == ScheduleWarningCodes.OutOfHours));
    }

    [Fact]
    public void MissingHoursOrVisitIsIncompleteButNotAConflict()
    {
        var noHours = new StopPlan(1, 1, 1, 60, null, null);
        var check = new ScheduleChecker(30).Check(Start, [noHours], [Slot(10, 1, 9, 0, 10, 0)], [new(1, 10, null)]);
        Assert.False(check.Complete);
        Assert.True(check.Fits);
        Assert.True(Has(check, ScheduleWarningCodes.Incomplete));
        var noVisit = new StopPlan(1, 1, 1, 0, Open, Close);
        Assert.False(new ScheduleChecker(30).Check(Start, [noVisit], [Slot(10, 1, 9, 0, 10, 0)], [new(1, 10, null)]).Complete);
    }

    [Fact]
    public void StopWithoutSlotsIsNotIncomplete()
    {
        var check = new ScheduleChecker(30).Check(Start, [new StopPlan(1, 1, 1, 60, null, null)], [], []);
        Assert.True(check.Complete);
        Assert.True(check.Fits);
    }
}
