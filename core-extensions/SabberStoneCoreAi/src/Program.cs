using System;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCoreAi.POGame;
using SabberStoneCoreAi.Agent.ExampleAgents;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.Meta;

namespace SabberStoneCoreAi
{
	internal class Program
	{

		private static void Main(string[] args)
		{
			
			Console.WriteLine("Setup gameConfig");

			//todo: rename to Main
			GameConfig gameConfig = new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.MAGE,
				Player1Deck = Decks.RenoKazakusMage,
				// Player1Deck = Decks.AggroPirateWarrior,					
				// Player1Deck = Decks.MidrangeJadeShaman,
				Player2HeroClass = CardClass.MAGE,
				Player2Deck = Decks.RenoKazakusMage,
				// Player2Deck = Decks.AggroPirateWarrior,					
				// Player2Deck = Decks.MidrangeJadeShaman,
				FillDecks = true,
				Logging = true
			};

			Console.WriteLine("Setup POGameHandler");
			AbstractAgent player1 = new TheTrueGalahad();
			AbstractAgent player2 = new FaceHunter();
			var gameHandler = new POGameHandler(gameConfig, player1, player2, debug:true);

			Console.WriteLine("PlayGame");
			//gameHandler.PlayGame();
			gameHandler.PlayGames(10);
			GameStats gameStats = gameHandler.getGameStats();

			gameStats.printResults();


			Console.WriteLine("Test successful");
			Console.ReadLine();
		}
	}
}
