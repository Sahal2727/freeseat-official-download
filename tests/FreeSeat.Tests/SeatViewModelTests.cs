using FreeSeat.App.ViewModels;
using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Seats.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace FreeSeat.Tests;

public sealed class SeatViewModelTests
{
    [Fact]
    public void LoadingExistingSeat_DoesNotWriteBackAssignments()
    {
        var service = new Mock<ISeatConfigurationService>();
        service.Setup(x => x.GetAvailableMonitors()).Returns(Array.Empty<MonitorDevice>());
        service.Setup(x => x.GetAvailableKeyboards()).Returns(Array.Empty<KeyboardDevice>());
        service.Setup(x => x.GetAvailableMice()).Returns(Array.Empty<MouseDevice>());
        service.Setup(x => x.GetAvailableSessions()).Returns(Array.Empty<FreeSeat.Core.Sessions.Models.ManagedSession>());

        var seat = Seat.CreateDefault("Seat 1")
            .WithEnabled(true)
            .WithMonitor("monitor-offline")
            .WithKeyboard("keyboard-offline")
            .WithMouse("mouse-offline")
            .WithSession(23);

        var viewModel = new SeatViewModel(seat, service.Object);

        viewModel.MonitorId.Should().Be("monitor-offline");
        viewModel.KeyboardId.Should().Be("keyboard-offline");
        viewModel.MouseId.Should().Be("mouse-offline");
        viewModel.SessionId.Should().Be(23);
        viewModel.IsEnabled.Should().BeTrue();

        service.Verify(x => x.AssignMonitorAsync(It.IsAny<SeatId>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(x => x.AssignKeyboardAsync(It.IsAny<SeatId>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(x => x.AssignMouseAsync(It.IsAny<SeatId>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(x => x.AssignSessionAsync(It.IsAny<SeatId>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(x => x.SetSeatEnabledAsync(It.IsAny<SeatId>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
