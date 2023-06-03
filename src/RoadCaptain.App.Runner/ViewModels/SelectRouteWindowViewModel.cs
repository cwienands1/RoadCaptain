using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using RoadCaptain.App.Shared.Commands;
using RoadCaptain.Commands;
using RoadCaptain.Ports;
using RoadCaptain.UseCases;

namespace RoadCaptain.App.Runner.ViewModels
{
    public class SelectRouteWindowViewModel : ViewModelBase
    {
        private readonly SearchRoutesUseCase _useCase;
        private readonly RetrieveRepositoryNamesUseCase _retrieveRepositoryNamesUseCase;
        private readonly IWindowService _windowService;
        private readonly IWorldStore _worldStore;
        private RouteViewModel[] _routes = Array.Empty<RouteViewModel>();
        private string[] _repositories = Array.Empty<string>();
        private RouteViewModel? _selectedRoute;
        private World[] _availableWorlds = Array.Empty<World>();
        private World? _filterWorld;
        private string? _filterRouteName;
        private string? _filterCreatorName;
        private string? _filterZwiftRouteName;
        private double? _filterDistanceMax;
        private double? _filterDescentMax;
        private double? _filterAscentMax;
        private bool _isLoopYesChecked;
        private bool _isLoopNoChecked;
        private bool _isLoopBothChecked;
        private double? _filterDistanceMin;
        private double? _filterAscentMin;
        private double? _filterDescentMin;
        private string? _filterRepository;

        public SelectRouteWindowViewModel(SearchRoutesUseCase useCase,
            RetrieveRepositoryNamesUseCase retrieveRepositoryNamesUseCase,
            IWindowService windowService, IWorldStore worldStore)
        {
            _useCase = useCase;
            _retrieveRepositoryNamesUseCase = retrieveRepositoryNamesUseCase;
            _windowService = windowService;
            _worldStore = worldStore;
        }

        public AsyncRelayCommand SearchRoutesCommand => new AsyncRelayCommand(
                async parameter => await LoadRoutesForRepositoryAsync(parameter as string ?? "(unknown)"),
                _ => true)
            .OnFailure(async _ =>
            {
                await _windowService.ShowErrorDialog(_.Message);
                Routes = Array.Empty<RouteViewModel>();
            })
            .OnSuccess(_ => SelectedRoute = null);

        public string WindowTitle => "RoadCaptain - Route selection";

        public void Initialize()
        {
            Repositories = _retrieveRepositoryNamesUseCase.Execute();
            var allWorlds = new World { Id = "all", Name = "All" };
            AvailableWorlds = new[] { allWorlds }
                .Concat(_worldStore.LoadWorlds())
                .ToArray();
            FilterWorld = allWorlds;
            FilterRepository = "All";
        }

        public RouteViewModel[] Routes
        {
            get => _routes;
            set
            {
                if (value == _routes)
                {
                    return;
                }

                _routes = value;

                this.RaisePropertyChanged();
            }
        }

        public string[] Repositories
        {
            get => _repositories;
            set
            {
                if (value == _repositories)
                {
                    return;
                }

                _repositories = value;

                this.RaisePropertyChanged();
            }
        }

        public RouteViewModel? SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                if (value == _selectedRoute)
                {
                    return;
                }

                _selectedRoute = value;

                this.RaisePropertyChanged();
            }
        }

        public World[] AvailableWorlds
        {
            get => _availableWorlds;
            set
            {
                if (value == _availableWorlds)
                {
                    return;
                }
                
                _availableWorlds = value;

                this.RaisePropertyChanged();
            }
        }

        public string? FilterRepository
        {
            get => _filterRepository;
            set
            {
                if (value == _filterRepository)
                {
                    return;
                }
                
                _filterRepository = value;

                this.RaisePropertyChanged();
            }
        }

        public World? FilterWorld
        {
            get => _filterWorld;
            set
            {
                if (value == _filterWorld)
                {
                    return;
                }
                
                _filterWorld = value;
                
                this.RaisePropertyChanged();
            }
        }

        public string? FilterRouteName
        {
            get => _filterRouteName;
            set
            {
                if (value == _filterRouteName)
                {
                    return;
                }
                
                _filterRouteName = value;
                
                this.RaisePropertyChanged();
            }
        }

        public string? FilterCreatorName
        {
            get => _filterCreatorName;
            set
            {
                if (value == _filterCreatorName)
                {
                    return;
                }
                
                _filterCreatorName = value;
                
                this.RaisePropertyChanged();
            }
        }

        public string? FilterZwiftRouteName
        {
            get => _filterZwiftRouteName;
            set
            {
                if (value == _filterZwiftRouteName)
                {
                    return;
                }
                
                _filterZwiftRouteName = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterDistanceMin
        {
            get => _filterDistanceMin;
            set
            {
                if (value == _filterDistanceMin)
                {
                    return;
                }
                
                _filterDistanceMin = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterDistanceMax
        {
            get => _filterDistanceMax;
            set
            {
                if (value == _filterDistanceMax)
                {
                    return;
                }
                
                _filterDistanceMax = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterAscentMin
        {
            get => _filterAscentMin;
            set
            {
                if (value == _filterAscentMin)
                {
                    return;
                }
                
                _filterAscentMin = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterAscentMax
        {
            get => _filterAscentMax;
            set
            {
                if (value == _filterAscentMax)
                {
                    return;
                }
                
                _filterAscentMax = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterDescentMin
        {
            get => _filterDescentMin;
            set
            {
                if (value == _filterDescentMin)
                {
                    return;
                }
                
                _filterDescentMin = value;
                
                this.RaisePropertyChanged();
            }
        }

        public double? FilterDescentMax
        {
            get => _filterDescentMax;
            set
            {
                if (value == _filterDescentMax)
                {
                    return;
                }
                
                _filterDescentMax = value;
                
                this.RaisePropertyChanged();
            }
        }

        public bool IsLoopYesChecked
        {
            get => _isLoopYesChecked;
            set
            {
                if (value == _isLoopYesChecked)
                {
                    return;
                }
                
                _isLoopYesChecked = value;

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(FilterIsLoop));
            }
        }

        public bool IsLoopNoChecked
        {
            get => _isLoopNoChecked;
            set
            {
                if (value == _isLoopNoChecked)
                {
                    return;
                }
                
                _isLoopNoChecked = value;

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(FilterIsLoop));
            }
        }

        public bool IsLoopBothChecked
        {
            get => _isLoopBothChecked;
            set
            {
                if (value == _isLoopBothChecked)
                {
                    return;
                }
                
                _isLoopBothChecked = value;

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(FilterIsLoop));
            }
        }

        public bool? FilterIsLoop
        {
            get
            {
                if (IsLoopYesChecked)
                {
                    return true;
                }

                if (IsLoopNoChecked)
                {
                    return false;
                }

                return null;
            }
        }


        public async Task<CommandResult> LoadRoutesForRepositoryAsync(string repository)
        {
            try
            {
                var command = new SearchRouteCommand(
                    repository,
                    FilterWorld?.Name,
                    FilterCreatorName,
                    FilterRouteName,
                    FilterZwiftRouteName,
                    FilterDistanceMin == null ? null : (decimal)FilterDistanceMin,
                    FilterDistanceMax == null ? null : (decimal)FilterDistanceMax,
                    FilterAscentMin == null ? null : (decimal)FilterAscentMin,
                    FilterAscentMax == null ? null : (decimal)FilterAscentMax,
                    FilterDescentMin == null ? null : (decimal)FilterDescentMin,
                    FilterDescentMax == null ? null : (decimal)FilterDescentMax,
                    FilterIsLoop,
                    null,
                    null
                    );

                Routes = (await _useCase.ExecuteAsync(command))
                    .Select(routeModel => new RouteViewModel(routeModel))
                    .ToArray();
            }
            catch (Exception e)
            {
                return CommandResult.Failure(e.Message);
            }

            return CommandResult.Success();
        }
    }
}