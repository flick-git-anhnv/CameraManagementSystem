using CommunityToolkit.Mvvm.ComponentModel;

namespace CMS.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "KZTEK Camera Management System (CMS / VMS)";

    [ObservableProperty]
    private string _systemStatus = "Ready - Multi-brand VMS & Edge AI";
}
