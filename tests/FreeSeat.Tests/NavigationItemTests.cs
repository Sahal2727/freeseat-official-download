using FreeSeat.App.ViewModels;
using System.ComponentModel;
using Xunit;

namespace FreeSeat.Tests;

public sealed class NavigationItemTests
{
    [Fact]
    public void IsSelected_RaisesPropertyChanged()
    {
        var item = new MainViewModel.NavigationItem("Dashboard", "home", typeof(object));
        var notified = false;
        item.PropertyChanged += (_, args) => notified |= args.PropertyName == nameof(item.IsSelected);

        item.IsSelected = true;

        Assert.True(notified);
    }
}
