using System;
using System.Diagnostics;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;

using System.Text;
using System.IO;

namespace SabberStoneCoreAi.POGame
{
	class POGameHandler
	{
		private bool debug;

		private AbstractAgent player1;
		private AbstractAgent player2;

		private GameConfig gameConfig;
		private bool setupHeroes = true;

		private GameStats gameStats;
		private static readonly Random Rnd = new Random();


		public POGameHandler(GameConfig gameConfig, AbstractAgent player1, AbstractAgent player2,  bool setupHeroes = true, bool debug=false)
		{
			this.gameConfig = gameConfig;
			this.setupHeroes = setupHeroes;
			this.player1 = player1;
			player1.InitializeAgent();

			this.player2 = player2;
			player2.InitializeAgent();

			gameStats = new GameStats();
			this.debug = debug;

			// empty the logfile
			File.WriteAllText(Directory.GetCurrentDirectory() + @"\dump.log", "Starting new Log\n");
		}

		public bool PlayGame(bool addToGameStats=true)
		{
			Game game = new Game(gameConfig, setupHeroes);
			player1.InitializeGame();
			player2.InitializeGame();

			AbstractAgent currentAgent;
			Stopwatch currentStopwatch;
			POGame poGame;
			PlayerTask playertask = null;
			Stopwatch[] watches = new[] {new Stopwatch(), new Stopwatch()};
			
			game.StartGame();
			try
			{
				while (game.State != State.COMPLETE && game.State != State.INVALID)
				{
					if (gameConfig.Logging)
						game.Log(LogLevel.INFO, BlockType.SCRIPT, "POGameHandler", "Turn " + game.Turn);

					currentAgent = game.CurrentPlayer == game.Player1 ? player1 : player2;
					Controller currentPlayer = game.CurrentPlayer;
					currentStopwatch = game.CurrentPlayer == game.Player1 ? watches[0] : watches[1];
					poGame = new POGame(game, gameConfig.Logging);

					currentStopwatch.Start();
					playertask = currentAgent.GetMove(poGame);
					currentStopwatch.Stop();

					game.CurrentPlayer.Game = game;
					game.CurrentOpponent.Game = game;

					if (gameConfig.Logging)
						game.Log(LogLevel.INFO, BlockType.SCRIPT, "POGameHandler", playertask.ToString());
					game.Process(playertask);
					ShowLog(game, gameConfig, LogLevel.INFO);
				}
			}
			catch (Exception e)
			//Current Player loses if he throws an exception
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
				game.State = State.COMPLETE;
				game.CurrentPlayer.PlayState = PlayState.CONCEDED;
				game.CurrentOpponent.PlayState = PlayState.WON;

				if (addToGameStats && game.State != State.INVALID)
					gameStats.registerException(game, e);
			}

			if (game.State == State.INVALID)
				return false;

			if (addToGameStats)
				gameStats.addGame(game, watches);

			player1.FinalizeGame();
			player2.FinalizeGame();
			return true;
		}

		public void PlayGames(int nr_of_games, bool addToGameStats=true)
		{
			for (int i = 0; i < nr_of_games; i++)
			{
				if (!PlayGame(addToGameStats))
					i -= 1;		// invalid game
			}
		}

		public GameStats getGameStats()
		{
			return gameStats;
		}


		internal static void ShowLog(Game game, GameConfig gameConfig, LogLevel level)
		{
			var str = new StringBuilder();
			while (game.Logs.Count > 0)
			{
				LogEntry logEntry = game.Logs.Dequeue();
				if (logEntry.Level <= level)
				{
					ConsoleColor foreground = ConsoleColor.White;
					switch (logEntry.Level)
					{
						case LogLevel.DUMP:
							foreground = ConsoleColor.DarkCyan;
							break;
						case LogLevel.ERROR:
							foreground = ConsoleColor.Red;
							break;
						case LogLevel.WARNING:
							foreground = ConsoleColor.DarkRed;
							break;
						case LogLevel.INFO:
							foreground = logEntry.Location.Equals("Game") ? ConsoleColor.Yellow :
										 logEntry.Location.StartsWith("Quest") ? ConsoleColor.Cyan :
										 ConsoleColor.Green;
							break;
						case LogLevel.VERBOSE:
							foreground = ConsoleColor.DarkGreen;
							break;
						case LogLevel.DEBUG:
							foreground = ConsoleColor.DarkGray;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					Console.ForegroundColor = foreground;

					string logStr = $"{logEntry.TimeStamp.ToLongTimeString()} - {logEntry.Level} [{logEntry.BlockType}] - {logEntry.Location}: {logEntry.Text}";
					str.Append(logStr + "\n");
					if(gameConfig.Logging == true)
						Console.WriteLine(logStr);
				}
			}
			Console.ResetColor();

			File.AppendAllText(Directory.GetCurrentDirectory() + @"\dump.log", str.ToString());
		}
	}

}
