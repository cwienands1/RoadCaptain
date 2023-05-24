using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RoadCaptain.App.Runner.ViewModels;
using RoadCaptain.App.Shared.Models;

namespace RoadCaptain.App.Runner
{
    public class DesignTimeWindowService : IWindowService
    {
        public Task<string?> ShowOpenFileDialog(string? previousLocation)
        {
            throw new System.NotImplementedException();
        }

        public void ShowInGameWindow(InGameNavigationWindowViewModel viewModel)
        {
            throw new System.NotImplementedException();
        }

        public Task<TokenResponse?> ShowLogInDialog(Window owner)
        {
            throw new System.NotImplementedException();
        }

        public Task ShowErrorDialog(string message, Window owner)
        {
            throw new System.NotImplementedException();
        }

        public Task ShowErrorDialog(string message)
        {
            throw new System.NotImplementedException();
        }

        public void ShowMainWindow()
        {
            throw new System.NotImplementedException();
        }

        public Task ShowNewVersionDialog(Release release)
        {
            throw new System.NotImplementedException();
        }

        public Task ShowAlreadyRunningDialog()
        {
            throw new System.NotImplementedException();
        }

        public void SetLifetime(IApplicationLifetime applicationLifetime)
        {
            throw new System.NotImplementedException();
        }

        public void Shutdown(int exitCode)
        {
            throw new System.NotImplementedException();
        }

        public Task ShowWhatIsNewDialog(Release release)
        {
            throw new System.NotImplementedException();
        }

        public void ToggleElevationPlot(PlannedRoute? plannedRoute, bool? show)
        {
            throw new System.NotImplementedException();
        }
    }
}