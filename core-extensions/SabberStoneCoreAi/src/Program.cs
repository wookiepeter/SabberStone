using System;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCoreAi.POGame;
using SabberStoneCoreAi.Agent.ExampleAgents;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.Meta;
using System.Collections.Generic;
using System.Diagnostics;

namespace SabberStoneCoreAi
{
	internal class Program
	{

		private static void Main(string[] args)
		{
			
			Console.WriteLine("Setup gameConfig");

			//todo: rename to Main
			/*GameConfig gameConfig = new GameConfig
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
			};*/
			int numberOfFights = 10;
			int gamesPerFight = 100;
			for (int i = 0; i < numberOfFights; i++)
			{

				Console.WriteLine("Setup POGameHandler");
				AbstractAgent player1 = new Galahad();
				AbstractAgent player2 = GetRandomAgent();
				Console.WriteLine("Fight " + i + " out of " + numberOfFights + " ==> " + player1.ToString() + " vs. " + player2.ToString());
				GameConfig randomGameConfig = GetRandomGameConfig();
				Console.WriteLine(randomGameConfig.Player1HeroClass.ToString() + " vs. " + randomGameConfig.Player2HeroClass.ToString());
				var gameHandler = new POGameHandler(GetRandomGameConfig(), player1, player2, debug: true);

				gameHandler.PlayGames(gamesPerFight);

				GameStats gameStats = gameHandler.getGameStats();


				gameStats.printResults();
				Console.WriteLine(randomGameConfig.Player1HeroClass.ToString() + " vs. " + randomGameConfig.Player2HeroClass.ToString());

				player1.FinalizeAgent();

			}
			Console.WriteLine("Test successful");
			Process.Start(@"powershell", $@"-c (New-Object Media.SoundPlayer 'E:\music\sampleTracks\taylor_swift_goat.wav').PlaySync();");
			Console.ReadLine();
		}

		public static AbstractAgent GetRandomAgent()
		{
			Random rnd = new Random();
			int i = rnd.Next(5);
			switch (i)
			{
				case 0: return new FaceHunter();
				default: return new RandomAgentLateEnd();
			}
		}

		public static GameConfig GetRandomGameConfig()
		{
			Tuple<CardClass, List<SabberStoneCore.Model.Card>> tuple1 = GetRandomHeroWithDeck();
			Random rnd = new Random();
			int startPlayer = rnd.Next(1, 3);
			GameConfig gameConfig = new GameConfig
			{
				StartPlayer = startPlayer,
				Player1HeroClass = tuple1.Item1,
				Player1Deck = tuple1.Item2,
				Player2HeroClass = tuple1.Item1,
				Player2Deck = tuple1.Item2,
				FillDecks = true,
				Logging = true
			};
			return gameConfig;
		}

		public static Tuple<CardClass, List<SabberStoneCore.Model.Card>> GetRandomHeroWithDeck()
		{
			Random rnd = new Random();
			//int i = rnd.Next(3);
			int i = 2;
			switch (i)
			{
				case 0: return new Tuple<CardClass, List<SabberStoneCore.Model.Card>>(CardClass.MAGE, Decks.RenoKazakusMage);
				case 1: return new Tuple<CardClass, List<SabberStoneCore.Model.Card>>(CardClass.WARRIOR, Decks.AggroPirateWarrior);
				case 2: return new Tuple<CardClass, List<SabberStoneCore.Model.Card>>(CardClass.SHAMAN, Decks.MidrangeJadeShaman);
				default: return new Tuple<CardClass, List<SabberStoneCore.Model.Card>>(CardClass.MAGE, Decks.RenoKazakusMage);
			}
		}
	}
}
