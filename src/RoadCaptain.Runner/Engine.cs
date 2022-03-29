using System;
using System.Linq.Expressions;
using System.Reflection;
using RoadCaptain.Commands;
using RoadCaptain.GameStates;
using RoadCaptain.Ports;
using RoadCaptain.UseCases;

namespace RoadCaptain.Runner
{
    internal class Engine
    {
        private readonly Configuration _configuration;
        private readonly ConnectToZwiftUseCase _connectUseCase;
        private readonly IGameStateReceiver _gameStateReceiver;
        private readonly HandleZwiftMessagesUseCase _handleMessageUseCase;
        private readonly DecodeIncomingMessagesUseCase _listenerUseCase;
        private readonly LoadRouteUseCase _loadRouteUseCase;
        private readonly MonitoringEvents _monitoringEvents;
        private readonly NavigationUseCase _navigationUseCase;
        private readonly IWindowService _windowService;
        
        private TaskWithCancellation _gameStateReceiverTask;
        private TaskWithCancellation _initiatorTask;
        private TaskWithCancellation _listenerTask;
        private TaskWithCancellation _messageHandlingTask;
        private TaskWithCancellation _navigationTask;

        private GameState _previousGameState;

        public Engine(
            MonitoringEvents monitoringEvents,
            LoadRouteUseCase loadRouteUseCase,
            Configuration configuration,
            IWindowService windowService,
            DecodeIncomingMessagesUseCase listenerUseCase,
            ConnectToZwiftUseCase connectUseCase,
            HandleZwiftMessagesUseCase handleMessageUseCase,
            NavigationUseCase navigationUseCase,
            IGameStateReceiver gameStateReceiver)
        {
            _monitoringEvents = monitoringEvents;
            _loadRouteUseCase = loadRouteUseCase;
            _configuration = configuration;
            _windowService = windowService;
            _listenerUseCase = listenerUseCase;
            _connectUseCase = connectUseCase;
            _handleMessageUseCase = handleMessageUseCase;
            _navigationUseCase = navigationUseCase;
            _gameStateReceiver = gameStateReceiver;
        }

        private void GameStateReceived(GameState gameState)
        {
            _monitoringEvents.StateTransition(_previousGameState, gameState);

            if (gameState is LoggedInState)
            {
                _monitoringEvents.Information("User logged in");

                // Once the user has logged in we need to do two things:
                // 1. Start the connection listener (DecodeIncomingMessagesUseCase)
                // 2. Start the connection initiator (ConnectToZwiftUseCase)
                // When the listener picks up a new connection it will
                // dispatch the ConnectedToZwift state.
                StartZwiftConnectionListener();
                StartZwiftConnectionInitiator();
            }
            else if (gameState is NotLoggedInState)
            {
                // Stop the connection initiator and listener
                CancelAndCleanUp(() => _listenerTask);
                CancelAndCleanUp(() => _initiatorTask);

                if (_messageHandlingTask.IsRunning())
                {
                    CancelAndCleanUp(() => _messageHandlingTask);
                }
            }
            else if (gameState is WaitingForConnectionState)
            {
                _monitoringEvents.Information("Waiting for connection from Zwift");
            }
            else if (gameState is ConnectedToZwiftState)
            {
                _monitoringEvents.Information("Connected to Zwift");

                _loadRouteUseCase.Execute(new LoadRouteCommand { Path = _configuration.Route });

                // Start handling Zwift messages
                StartMessageHandler();
            }

            if (gameState is InGameState && _previousGameState is not InGameState)
            {
                _monitoringEvents.Information("User entered the game");

                // Start navigation if it is not running
                if (!_navigationTask.IsRunning())
                {
                    StartNavigation();
                }
            }

            if (gameState is ErrorState errorState)
            {
                _windowService.ShowErrorDialog(errorState.Exception.Message);
            }

            _previousGameState = gameState;
        }

        private void StartZwiftConnectionListener()
        {
            if (_listenerTask.IsRunning())
            {
                return;
            }

            _monitoringEvents.Information("Starting connection listener");

            _listenerTask = TaskWithCancellation.Start(cancellationToken => _listenerUseCase.ExecuteAsync(cancellationToken));
        }

        private void StartZwiftConnectionInitiator()
        {
            if (_initiatorTask.IsRunning())
            {
                return;
            }

            _monitoringEvents.Information("Starting connection initiator");

            _initiatorTask = _listenerTask.StartLinkedTask(
                token => _connectUseCase
                    .ExecuteAsync(
                        new ConnectCommand { AccessToken = _configuration.AccessToken },
                        token)
                    .GetAwaiter()
                    .GetResult());
        }

        private void StartMessageHandler()
        {
            if (_messageHandlingTask.IsRunning())
            {
                return;
            }

            _monitoringEvents.Information("Starting message handler");
            
            _messageHandlingTask = TaskWithCancellation.Start(token => _handleMessageUseCase.Execute(token));
        }

        private void StartNavigation()
        {
            if (_navigationTask.IsRunning())
            {
                return;
            }

            _monitoringEvents.Information("Starting navigation");
            
            _navigationTask = TaskWithCancellation.Start(token => _navigationUseCase.Execute(token));
        }

        private void CancelAndCleanUp(Expression<Func<TaskWithCancellation>> func)
        {
            if (func.Body is MemberExpression { Member: FieldInfo fieldInfo })
            {
                if (fieldInfo.GetValue(this) is TaskWithCancellation task)
                {
                    task.Cancel();

                    fieldInfo.SetValue(this, null);
                }
            }
        }

        public void Stop()
        {
            CancelAndCleanUp(() => _gameStateReceiverTask);
            CancelAndCleanUp(() => _messageHandlingTask);
            CancelAndCleanUp(() => _listenerTask);
            CancelAndCleanUp(() => _initiatorTask);
            CancelAndCleanUp(() => _navigationTask);
        }

        public void Start()
        {
            _gameStateReceiver.Register(null, null, GameStateReceived);

            _gameStateReceiverTask = TaskWithCancellation.Start(token => _gameStateReceiver.Start(token));
        }
    }
}