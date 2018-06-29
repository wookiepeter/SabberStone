using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.POGame;

// Required for Logger
using System.Text;
using System.Linq;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Model.Zones;
using System.IO;

using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SabberStoneCoreAi.Agent
{
    class Galahad : AbstractAgent
	{
		private Random Rnd = new Random();

		bool playerLogging = true;
		LogLevel logLevel = LogLevel.WARNING;
		string logFileName = "agent_Name_dump.log";

		ScoreMatrix gameStateScoreMatrix;

		float reward = 0;

		public override void FinalizeAgent()
		{
			Log(LogLevel.WARNING, "finalizing Agent");
			gameStateScoreMatrix.SaveScoreMatrix();
		}

		public override void FinalizeGame()
		{
			Log(LogLevel.INFO, "Game is over");
			if (reward > 0)
				gameStateScoreMatrix.UpdateScoreMatrix(reward, 0.05f);
		}

		public override PlayerTask GetMove(SabberStoneCoreAi.POGame.POGame poGame)
		{
			// reset possible reward
			reward = 0;

			List<PlayerTask> options = poGame.CurrentPlayer.Options();

			List<float[]> optionRatings = new List<float[]>();
			optionRatings.Add(ValuedOptionRating(poGame, options));
			optionRatings.Add(RandomOptionRating(poGame, options));
			optionRatings.Add(HardCodedSimulationRating(poGame, options));
			optionRatings.Add(HardCodedOptionRating(poGame, options));

			// Compute best option from all inputs
			List<float> inputValues = new List<float>() { 0.3f, 0.00f, 0.1f, 0.7f };
			for (int i = 0; i < optionRatings.Count; i++)
			{
				optionRatings[i] = VectorMult(optionRatings[i], inputValues[i]);
			}
			float[] result = VectorAdd(optionRatings);
			List<int> bestOptionIndices = GetBestOptionsIndices(result, options);

			if (bestOptionIndices.Count <= 0)
				Log(LogLevel.WARNING, "No good Options available");

			// select Task based on Options
			PlayerTask selectedTask = options[bestOptionIndices[Rnd.Next(bestOptionIndices.Count)]];

			// try to simulate next task and update Scorematrix
			PlayerTask actualTask = SimulateNextTaskAndUpdateScoreMatrix(poGame, selectedTask, options);

			return selectedTask;
		}

		/// <summary>
		/// simulate the chosen move and update scorematrix with it
		/// </summary>
		/// <param name="poGame"></param>
		/// <param name="task"></param>
		/// <param name="options"></param>
		/// <returns>returns different Task if simulated Task produces errors</returns>
		PlayerTask SimulateNextTaskAndUpdateScoreMatrix(POGame.POGame poGame, PlayerTask task, List<PlayerTask> options)
		{
			POGame.POGame simulatedGame = poGame.getCopy();
			try
			{
				Dictionary<PlayerTask, POGame.POGame> simulatedGames = simulatedGame.Simulate(new List<PlayerTask>() { task });
				int tries = 0;
				while ((simulatedGames[task] == null || simulatedGames[task].State == SabberStoneCore.Enums.State.INVALID) && options.Count > 1)
				{
					if (tries > 3)
					{
						break;
					}
					task = options[Rnd.Next(options.Count)];
					simulatedGames = simulatedGame.Simulate(new List<PlayerTask>() { task });
					tries++;
				}
				simulatedGame.Process(task);
			}
			catch { }

			if(simulatedGame != null)
				gameStateScoreMatrix.VisitState(GetExtensiveScoreParametersOfGame(simulatedGame));
			// check if game is ending to computer reward
			if (simulatedGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.WON)
			{
				Log(LogLevel.INFO, "Might be winning next round, with health: " + simulatedGame.CurrentPlayer.Hero.Health);
				float additionalReward = Math.Clamp(simulatedGame.CurrentPlayer.Hero.Health / 30, 0, 1) * 100f;
				reward = 100f + additionalReward;
			}

			return task;
		}

		List<int> GetBestOptionsIndices(float[] array, List<PlayerTask> options)
		{
			float currentMax = -100;
			List<int> bestOptionIndices = new List<int>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] > currentMax)
				{
					bestOptionIndices.Clear();
					currentMax = array[i];
					bestOptionIndices.Add(i);
				}
				else if (array[i] == currentMax)
				{
					bestOptionIndices.Add(i);
				}
			}
			return bestOptionIndices;
		}

		float[] VectorMult(float[] array, float value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] *= value;
			}
			return array;
		}

		float[] VectorAdd(List<float[]> arrayList)
		{
			int arrayLength = arrayList[0].Length;
			float[] result = new float[arrayLength];
			for (int i = 0; i < arrayLength; i++)
			{
				for (int j = 0; j < arrayList.Count; j++)
				{
					result[i] += arrayList[j][i];
				}
			}
			return result;
		}

		float[] RandomOptionRating(SabberStoneCoreAi.POGame.POGame poGame, List<PlayerTask> options)
		{
			float[] result = new float[options.Count];
			for (int i = 0; i < options.Count; i++)
			{
				result[i] = (float)Rnd.NextDouble() * 2f - 1f;
			}
			return result;
		}

		float[] ValuedOptionRating(POGame.POGame poGame, List<PlayerTask> options)
		{
			float[] result = new float[options.Count];

			try
			{
				Dictionary<PlayerTask, POGame.POGame> dict = poGame.Simulate(options);
				for (int i = 0; i < options.Count; i++)
				{
					if (dict[options[i]] != null)
						result[i] = gameStateScoreMatrix.GetScoreFromMatrix(GetExtensiveScoreParametersOfGame(dict[options[i]]));
				}
			}
			catch
			{

			}

			// 
			float highestValue = 0f;
			for(int i = 0; i < result.Length; i++)
			{
				if (result[i] > highestValue)
				{
					highestValue = result[i];
				}
			}
			if (highestValue > 0)
			{
				for (int i = 0; i < result.Length; i++)
				{
					result[i] /= 200f;
				}
			}

			return result;
		}

		float[] HardCodedSimulationRating(POGame.POGame poGame, List<PlayerTask> options)
		{
			float[] result = new float[options.Count];

			try
			{
				Dictionary<PlayerTask, POGame.POGame> dict = poGame.Simulate(options);
				for (int i = 0; i < options.Count; i++)
				{
					if (dict[options[i]] != null)
						result[i] = HardCodedGameRating(dict[options[i]]);
				}
			}
			catch
			{

			}

			return result;
		}

		float HardCodedGameRating(POGame.POGame poGame)
		{
			Controller me = poGame.CurrentPlayer;
			Controller op = poGame.CurrentOpponent;

			int hp = me.Hero.Health;
			int opHp = op.Hero.Health;
			int heroDamage = me.Hero.TotalAttackDamage;
			int opHeroDamage = op.Hero.TotalAttackDamage;
			int handZoneCount = me.HandZone.Count;
			int opHandZoneCount = op.HandZone.Count;
			float averageHandCost = me.HandZone.GetAll().Sum(p => p.Cost) / ((handZoneCount == 0) ? 1 : handZoneCount);
			int remainingCards = me.DeckZone.Count;
			int opRemainingCards = op.DeckZone.Count;
			int numberOfMinions = me.BoardZone.Count;
			int minionTotHealth = me.BoardZone.Sum(p => p.Health);
			int minionTotAttack = me.BoardZone.Sum(p => p.AttackDamage);
			int minionTotTauntHp = me.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health);
			int opNumberOfMinions = op.BoardZone.Count;
			int opMinionTotHealth = op.BoardZone.Sum(p => p.Health);
			int opMinionTotAttack = op.BoardZone.Sum(p => p.AttackDamage);
			int opMinionTotTauntHp = op.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health);

			float result = 0f;

			if (opHp < 1)
				return 10f;

			if (hp < 1)
				return -10f;

			if (opNumberOfMinions == 0 && numberOfMinions > 0)
				result += 0.5f;

			if (opMinionTotTauntHp > 0)
				result *= 0.2f;

			result *= minionTotAttack;

			result += (hp - opHp) * 0.1f;

			result += (minionTotHealth - opMinionTotHealth) * 0.1f;

			return result;
		}

		float[] HardCodedOptionRating(POGame.POGame poGame, List<PlayerTask> options)
		{
			Controller me = poGame.CurrentPlayer;
			Controller op = poGame.CurrentOpponent;

			int hp = me.Hero.Health;
			int opHp = op.Hero.Health;
			int heroDamage = me.Hero.TotalAttackDamage;
			int opHeroDamage = op.Hero.TotalAttackDamage;
			int handZoneCount = me.HandZone.Count;
			int opHandZoneCount = op.HandZone.Count;
			float averageHandCost = me.HandZone.GetAll().Sum(p => p.Cost) / ((handZoneCount == 0)? 1 : handZoneCount);
			int remainingCards = me.DeckZone.Count;
			int opRemainingCards = op.DeckZone.Count;
			int numberOfMinions = me.BoardZone.Count;
			int minionTotHealth = me.BoardZone.Sum(p => p.Health);
			int minionTotAttack = me.BoardZone.Sum(p => p.AttackDamage);
			int minionTotTauntHp = me.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health);
			int opNumberOfMinions = op.BoardZone.Count;
			int opMinionTotHealth = op.BoardZone.Sum(p => p.Health);
			int opMinionTotAttack = op.BoardZone.Sum(p => p.AttackDamage);
			int opMinionTotTauntHp = op.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health);

			float[] result = new float[options.Count];
			for (int i = 0; i < options.Count; i++)
			{
				PlayerTask curOption = options[i];
				if(curOption.PlayerTaskType == PlayerTaskType.MINION_ATTACK)
				{
					Minion minion = (Minion)curOption.Source;
					if(curOption.Target.Card.Type == SabberStoneCore.Enums.CardType.HERO)
					{
						if(minion.AttackDamage > opHp)
						{
							result[i] = 1f;
						}
						if(minionTotAttack > 1.3f * opMinionTotAttack && minionTotHealth > 1.3f * opMinionTotHealth)
						{
							result[i] += 0.5f;
						}
						result[i] += 0.3f;
					}
					if(curOption.Target.Card.Type == SabberStoneCore.Enums.CardType.MINION)
					{
						Minion targetMinion = (Minion)curOption.Target;
						if (minionTotAttack < 0.8f * opMinionTotAttack && minionTotHealth < 0.8f * opMinionTotHealth && minion.Health > targetMinion.AttackDamage)
						{
							result[i] += 0.5f;
						}
						if (minion.Health > targetMinion.AttackDamage && minion.AttackDamage > targetMinion.Health)
						{
							result[i] += 0.5f;
						}
					}
					if (minionTotAttack > 1.2f * opMinionTotAttack && minionTotHealth > 1.2f * opMinionTotHealth && opMinionTotTauntHp < 5)
					{
						result[i] += 0.1f;
					}
					result[i] += 0.1f;
				}
				if(curOption.PlayerTaskType == PlayerTaskType.PLAY_CARD)
				{
					if(curOption.Source.Card.Type == SabberStoneCore.Enums.CardType.MINION)
					{
						if (opNumberOfMinions > numberOfMinions && opMinionTotHealth > minionTotHealth || opMinionTotAttack > minionTotAttack)
							result[i] += 0.5f;
						result[i] += 0.5f;
						if (numberOfMinions == 0 && numberOfMinions == 0)
							result[i] += 0.4f;
					}
					if(curOption.Source.Card.Type == SabberStoneCore.Enums.CardType.WEAPON)
					{
						if(me.Hero.Damage <= 0)
						{
							result[i] += 0.4f;
						}
					}
					if(curOption.Source.Card.Type == SabberStoneCore.Enums.CardType.SPELL)
					{
						Spell curSpell = (Spell)curOption.Source;
						if (curOption.HasTarget && curOption.Target.Card.Type == SabberStoneCore.Enums.CardType.HERO)
						{
							result[i] += 0.2f;
						}
						if (curOption.HasTarget == false)
						{
							result[i] += 0.1f;
						}
					}
				}
				if(curOption.PlayerTaskType == PlayerTaskType.HERO_POWER)
				{
					if (me.HeroClass == SabberStoneCore.Enums.CardClass.SHAMAN)
						result[i] += 0.2f;
					if (me.HeroClass == SabberStoneCore.Enums.CardClass.MAGE && curOption.Target.Controller == me)
						result[i] = -1f;
					result[i] += 0.1f;
				}
				if(me.BaseMana <= 3 && me.RemainingMana >= 2 && (curOption.PlayerTaskType == PlayerTaskType.HERO_POWER || curOption.PlayerTaskType == PlayerTaskType.PLAY_CARD))
				{
					result[i] += 0.5f;
				}
				if (curOption.PlayerTaskType == PlayerTaskType.END_TURN && me.RemainingMana > 0.5f * me.BaseMana)
					result[i] = -0.5f;
				if(curOption.PlayerTaskType == PlayerTaskType.HERO_ATTACK)
				{
					if(curOption.Target is Minion)
					{
						Minion curTarget = (Minion)curOption.Target;
						if (hp > curTarget.AttackDamage && curTarget.Health < heroDamage)
							result[i] += 0.5f;
					}
					if (curOption.Target is Hero)
					{
						Hero curTarget = (Hero)curOption.Target;
						if (hp > curTarget.AttackDamage && curTarget.Health < heroDamage)
							result[i] += 0.5f;
					}
					else result[i] += 0.2f;
				}
				
			}
			return result;

			/*for (int i = 0; i < options.Count; i++)
			{

				if (options[i].PlayerTaskType == PlayerTaskType.PLAY_CARD)
				{
					if (options[i].Source.Card.Type == SabberStoneCore.Enums.CardType.MINION)
					{

						if (bestCard == null || options[i].Source.Card.Cost > bestCard.Cost)
						{
							bestCard = options[i].Source.Card;
							id = i;
						}
					}

				}

				if (options[i].PlayerTaskType == PlayerTaskType.MINION_ATTACK)
				{
					if (options[i].Target != null && options[i].Target.Card.Type == SabberStoneCore.Enums.CardType.HERO)
						hardMinionAttackOptionRating[i] = 1;
				}
			}*/
		}

		string getStringFromPlayerTask(PlayerTask task)
		{
			string result = task.FullPrint();
			return result;
		}

		public override void InitializeAgent()
		{
			logFileName = "agent_" + GetType().Name + "_dump.log";
			File.WriteAllText(Directory.GetCurrentDirectory() + @"\"+logFileName, "Starting new AgentLog for Agent "+GetType().Name+"\n");

			List<ScoreParameter> scoreParameters = new List<ScoreParameter>() {
				new ScoreParameter("Hero", 0, 3, 3, true),
				new ScoreParameter("OpHero", 0, 3, 3, true), 
				new ScoreParameter("HeroHP", 0, 30, 3, false),
				new ScoreParameter("OpHeroHp", 0, 30, 3, false),
				//new ScoreParameter("HeroAtk", 0, 3, 3),
				//new ScoreParameter("OpHeroAtk", 0, 3, 3),
				new ScoreParameter("HandCount", 0, 10, 5, false),
				new ScoreParameter("OpHandCount", 0, 10, 5, false),
				//new ScoreParameter("HandAverageCost", 0, 10, 5),
				//new ScoreParameter("DeckCount", 0, 25, 5),
				//new ScoreParameter("OpDeckcount", 0, 25, 5),
				new ScoreParameter("NumberOfMinions", 0, 8, 4, true),
				new ScoreParameter("MinionTotHealth", 0, 25, 5, false),
				new ScoreParameter("MinionTotAttack", 0, 25, 5, false),
				//new ScoreParameter("MinionTotHealthTaunt", 0, 20, 4),
				new ScoreParameter("OpNumberOfMinions", 0, 8, 4, true),
				new ScoreParameter("OpMinionTotHealth", 0, 25, 5, false),
				new ScoreParameter("OpMinionTotAttack", 0, 25, 5),
				//new ScoreParameter("OpMinionTotHealthTaunt", 0, 20, 4)
			};
			gameStateScoreMatrix = new ScoreMatrix(scoreParameters, "gamestate");

			Rnd = new Random();
		}

		public int[] GetExtensiveScoreParametersOfGame(POGame.POGame poGame)
		{
			Controller me = poGame.CurrentPlayer;
			Controller op = poGame.CurrentOpponent;
			int[] result = new int[]
			{
				GetHeroID(me.Hero),
				GetHeroID(op.Hero),
				me.Hero.Health,
				op.Hero.Health,
				//me.Hero.TotalAttackDamage,
				//op.Hero.TotalAttackDamage,
				me.HandZone.Count,
				op.HandZone.Count,
				//ComputeAverageCostOfHand(poGame.CurrentPlayer.Controller.HandZone),
				//poGame.CurrentPlayer.Controller.DeckZone.Count,
				//op.DeckZone.Count,
				me.BoardZone.Count,
				me.BoardZone.Sum(p => p.Health),
				me.BoardZone.Sum(p => p.AttackDamage),
				//poGame.CurrentPlayer.Controller.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health),
				op.BoardZone.Count,
				op.BoardZone.Sum(p => p.Health),
				op.BoardZone.Sum(p => p.AttackDamage),
				//poGame.CurrentOpponent.Controller.BoardZone.Where(p => p.HasTaunt).Sum(p => p.Health)
			};
			
			return result;
		}


		int GetHeroID(Hero hero)
		{
			int heroID;
			switch (hero.Card.Class)
			{
				case SabberStoneCore.Enums.CardClass.MAGE:
					heroID = 1;
					break;
				case SabberStoneCore.Enums.CardClass.WARRIOR:
					heroID = 2;
					break;
				case SabberStoneCore.Enums.CardClass.SHAMAN:
					heroID = 3;
					break;
				default:
					heroID = 0;
					break;
			}
			return heroID;
		}

		int ComputeAverageCostOfHand(HandZone hand)
		{
			float averageCost = 0;
			int n = 0;
			foreach (IPlayable item in hand.GetAll())
			{
				n++;
				averageCost += item.Cost;
			}
			return (int)Math.Round(averageCost / (float)n, 1, MidpointRounding.ToEven);
		}

		public override void InitializeGame()
		{
			Log(LogLevel.INFO, "Starting new Game");
		}

		void Log(LogLevel level, string text)
		{
			var str = new StringBuilder();
				if (logLevel >= level)
				{
					ConsoleColor foreground = ConsoleColor.White;
					switch (level)
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
							foreground = ConsoleColor.Cyan;
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

					string logStr = $"{DateTime.Now.ToLongTimeString()} - {level} [{"AgentLog"}] - {GetType().Name}: {text}";
					str.Append(logStr + "\n");
					if (playerLogging == true)
						Console.WriteLine(logStr);
			}
			Console.ResetColor();

			File.AppendAllText(Directory.GetCurrentDirectory() + @"\"+logFileName, str.ToString());
			File.AppendAllText(Directory.GetCurrentDirectory() + @"\" + "dump.log", str.ToString());
		}
	}




}
