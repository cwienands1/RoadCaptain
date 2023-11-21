// Copyright (c) 2023 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.IdentityModel.JsonWebTokens;
using ReactiveUI;
using RoadCaptain.App.Shared;
using RoadCaptain.App.Shared.Commands;
using RoadCaptain.App.Shared.Models;
using RoadCaptain.Commands;
using RoadCaptain.Ports;
using RoadCaptain.UseCases;

namespace RoadCaptain.App.RouteBuilder.ViewModels
{
    public class SaveRouteDialogViewModel : ViewModelBase
    {
        private readonly IWindowService _windowService;
        private RouteViewModel _route;
        private ImmutableList<string>? _repositoryNames;
        private string? _selectedRepositoryName;
        private readonly RetrieveRepositoryNamesUseCase _retrieveRepositoryNamesUseCase;
        private readonly SaveRouteUseCase _saveRouteUseCase;
        private readonly IZwiftCredentialCache _credentialCache;
        private readonly IZwift _zwift;
        private readonly IUserPreferences _userPreferences;
        private readonly IEnumerable<IRouteRepository> _repositories;
        private string? _outputFilePath;
    
        public SaveRouteDialogViewModel(
            IWindowService windowService,
            RouteViewModel route,
            RetrieveRepositoryNamesUseCase retrieveRepositoryNamesUseCase, 
            SaveRouteUseCase saveRouteUseCase, 
            IZwiftCredentialCache credentialCache, 
            IZwift zwift, 
            IUserPreferences userPreferences,
            IEnumerable<IRouteRepository> repositories)
        {
            _windowService = windowService;
            _route = route;
            _retrieveRepositoryNamesUseCase = retrieveRepositoryNamesUseCase;
            _saveRouteUseCase = saveRouteUseCase;
            _credentialCache = credentialCache;
            _zwift = zwift;
            _userPreferences = userPreferences;
            _repositories = repositories;
        }

        public ICommand SaveRouteCommand => new AsyncRelayCommand(
                _ => SaveRoute(),
                _ => !string.IsNullOrEmpty(RouteName) &&
                    (SelectedRepositoryName != null || (SelectedRepositoryName == null && !string.IsNullOrEmpty(OutputFilePath))))
            .OnSuccess(async _ =>
            {
                await CloseWindow();
            })
            .OnFailure(async result =>
                await _windowService.ShowErrorDialog($"Unable to save route: {result.Message}", _windowService.GetCurrentWindow()!))
            .SubscribeTo(this, () => SelectedRepositoryName)
            .SubscribeTo(this, () => RouteName)
            .SubscribeTo(this, () => OutputFilePath);
        
        public ICommand SelectFileCommand => new AsyncRelayCommand(
            _ => SelectFile(),
            _ => !string.IsNullOrEmpty(RouteName))
            .OnSuccess(_ =>
            {
                SelectedRepositoryName = null;
                return Task.CompletedTask;
            })
            .OnFailure(async result =>
                await _windowService.ShowErrorDialog($"Unable to select a file to save to: {result.Message}", _windowService.GetCurrentWindow()!))
            .SubscribeTo(this, () => RouteName);
        
        private async Task<CommandResult> SaveRoute()
        {
            if (string.IsNullOrEmpty(RouteName))
            {
                return CommandResult.Failure("Route name is empty");
            }
            if (string.IsNullOrEmpty(SelectedRepositoryName) && string.IsNullOrEmpty(OutputFilePath))
            {
                return CommandResult.Failure("No route repository selected and no local file given, can't save this route without either of those");
            }
            
            try
            {
                TokenResponse? token = null;

                if(!string.IsNullOrEmpty(SelectedRepositoryName))
                {
                    var repository = _repositories.SingleOrDefault(r => r.Name == SelectedRepositoryName);

                    if (repository == null)
                    {
                        throw new InvalidOperationException(
                            "I don't know what happened but a repository name was selected that I can't find...");
                    }

                    if (repository.RequiresAuthentication)
                    {
                        token = await AuthenticateToZwiftAsync();
                    }
                }
                
                await _saveRouteUseCase.ExecuteAsync(new SaveRouteCommand(_route.AsPlannedRoute()!, RouteName, SelectedRepositoryName, token?.AccessToken, OutputFilePath));
                
                _route.Save();
                
                return CommandResult.Success();
            }
            catch (Exception e)
            {
                return CommandResult.Failure(e.Message);
            }
        }

        private async Task<CommandResult> SelectFile()
        {
            var outputFilePath = await _windowService.ShowSaveFileDialog(_userPreferences.LastUsedFolder, RouteName + ".json");

            if (string.IsNullOrEmpty(outputFilePath))
            {
                return CommandResult.Aborted();
            }

            OutputFilePath = outputFilePath;
            
            return CommandResult.Success();
        }

        public string? OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                if (value == _outputFilePath)
                {
                    return;
                }
                
                _outputFilePath = value;
                
                this.RaisePropertyChanged();
            }
        }

        private async Task<TokenResponse?> AuthenticateToZwiftAsync()
        {
            var tokenResponse = await _credentialCache.LoadAsync();

            if (tokenResponse != null)
            {
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    var accessToken = new JsonWebToken(tokenResponse.AccessToken);

                    if (accessToken.ValidTo < DateTime.UtcNow.AddHours(1))
                    {
                        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                        {
                            var refreshToken = new JsonWebToken(tokenResponse.RefreshToken);

                            if (refreshToken.ValidTo < DateTime.UtcNow.AddHours(1))
                            {
                                tokenResponse = null;
                            }
                            else
                            {
                                try
                                {
                                    var refreshedTokens = await _zwift.RefreshTokenAsync(tokenResponse.RefreshToken);

                                    tokenResponse = new TokenResponse
                                    {
                                        AccessToken = refreshedTokens.AccessToken,
                                        RefreshToken = refreshedTokens.RefreshToken,
                                        ExpiresIn = (long)refreshedTokens.ExpiresOn.Subtract(DateTime.UtcNow).TotalSeconds,
                                        UserProfile = tokenResponse.UserProfile
                                    };

                                    await _credentialCache.StoreAsync(tokenResponse);
                                }
                                catch
                                {
                                    tokenResponse = null;
                                }
                            }
                        }
                        else
                        {
                            tokenResponse = null;
                        }
                    }
                }
                else
                {
                    tokenResponse = null;
                }
            }

            if (tokenResponse != null)
            {
                return tokenResponse;
            }

            var currentWindow = _windowService.GetCurrentWindow();
            if (currentWindow == null)
            {
                throw new InvalidOperationException(
                    "Unable to determine what the current window and I can't parent a dialog to an unknown window");
            }

            tokenResponse = await _windowService.ShowLogInDialog(currentWindow);

            if (tokenResponse != null &&
                !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                // Keep this in memory so that when the app navigates
                // from the in-game window to the main window the user
                // remains logged in.
                await _credentialCache.StoreAsync(tokenResponse);
            }

            return tokenResponse;
        }

        public string? RouteName
        {
            get => _route.Name;
            set
            {
                if (_route.Name == value) return;
                _route.Name = value ?? string.Empty;
                this.RaisePropertyChanged();
            }
        }

        public RouteViewModel Route
        {
            get => _route;
            set
            {
                if (_route == value) return;
                _route = value;
                this.RaisePropertyChanged();
            }
        }

        public ImmutableList<string> Repositories
        {
            get => _repositoryNames ?? ImmutableList<string>.Empty;
            set
            {
                if (value == _repositoryNames)
                {
                    return;
                }
                
                _repositoryNames = value;
                
                this.RaisePropertyChanged();
            }
        }

        public string? SelectedRepositoryName
        {
            get => _selectedRepositoryName;
            set
            {
                if (value == _selectedRepositoryName)
                {
                    return;
                }
                
                _selectedRepositoryName = value;

                if (value != null)
                {
                    OutputFilePath = null;
                }
                
                this.RaisePropertyChanged();
            }
        }
        public event EventHandler? ShouldClose;

        private Task CloseWindow()
        {
            ShouldClose?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public void Initialize()
        {
            Repositories = _retrieveRepositoryNamesUseCase.Execute(new RetrieveRepositoryNamesCommand(RetrieveRepositoriesIntent.Store)).ToImmutableList();
        }
    }
}
