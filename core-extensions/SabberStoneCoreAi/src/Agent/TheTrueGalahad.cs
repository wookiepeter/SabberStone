﻿using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.POGame;

// Required for Logger
using System.Text;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Model.Zones;
using System.IO;

namespace SabberStoneCoreAi.Agent
{

    class TheTrueGalahad : AbstractAgent
	{
		private Random Rnd = new Random();

		bool playerLogging = true;
		LogLevel logLevel = LogLevel.INFO;
		string logFileName = "agent_Name_dump.log";

		ScoreMatrix gameStateScoreMatrix;

		public override void FinalizeAgent()
		{

		}

		public override void FinalizeGame()
		{
			Log(LogLevel.INFO, "Game is over");
			//gameStateScoreMatrix.SaveScoreMatrix();
		}

		public override PlayerTask GetMove(SabberStoneCoreAi.POGame.POGame poGame)
		{

			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			float[] hardCodingOptionRating = new float[options.Count];
			float[] hardAngriffOptionRating = new float[options.Count];
			float[] hardMinionAttackOptionRating = new float[options.Count];

			Card bestCard = null;
			int id = -1;

			for (int i = 0; i < options.Count; i++)
			{

				if(options[i].PlayerTaskType == PlayerTaskType.PLAY_CARD)
				{
					if(options[i].Source.Card.Type == SabberStoneCore.Enums.CardType.MINION){

						if(bestCard == null ||  options[i].Source.Card.Cost > bestCard.Cost)
						{
							bestCard = options[i].Source.Card;
							id = i;
						}
					}
					
				}

				if(options[i].PlayerTaskType == PlayerTaskType.MINION_ATTACK)
				{
					if(options[i].Target != null && options[i].Target.Card.Type == SabberStoneCore.Enums.CardType.HERO)
						hardMinionAttackOptionRating[i] = 1;
				}
			}

			if (id != -1)
				hardCodingOptionRating[id] = 1;

			List<float[]> optionRatings = new List<float[]>();

			optionRatings.Add(hardMinionAttackOptionRating);
			optionRatings.Add(hardAngriffOptionRating);
			optionRatings.Add(RandomOptionRating(poGame, options));
			optionRatings.Add(hardCodingOptionRating);

			List<float> inputValues = new List<float>(){0.5f, 0,0,0.5f};


			for (int i = 0; i < optionRatings.Count; i++)
			{
				optionRatings[i] = VectorMult(optionRatings[i], inputValues[i]);
			}


			float[] result = VectorAdd(optionRatings);

			List<int> bestOptionIndices = GetBestOptionsIndices(result, options);
			return options[bestOptionIndices[0]];
		}

		List<int> GetBestOptionsIndices(float[] array, List<PlayerTask> options)
		{

			float currentMax = -1;
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

		float[] RandomOptionRating(SabberStoneCoreAi.POGame.POGame poGame, List<PlayerTask> playerTasks)
		{
			float[] result = new float[playerTasks.Count];

			result[Rnd.Next(playerTasks.Count)] = 1;

			return result;
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

			Rnd = new Random();
		}

		public override void InitializeGame()
		{
			Log(LogLevel.INFO, "Starting new Game");
		}

		void Log(LogLevel level, string text)
		{
			var str = new StringBuilder();
				if (logLevel <= level)
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
