using System;
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
	class BoardStats
	{
		public BoardStats(POGame.POGame poGame)
		{
			
		}

		public float getValue()
		{
			float value = 0;
			return value;
		}
	}

    class Galahad : AbstractAgent
	{
		private Random Rnd = new Random();

		bool playerLogging = true;
		LogLevel logLevel = LogLevel.INFO;
		string logFileName = "agent_Name_dump.log";

		public override void FinalizeAgent()
		{
		}

		public override void FinalizeGame()
		{
			Log(LogLevel.INFO, "Game is over");
		}

		public override PlayerTask GetMove(SabberStoneCoreAi.POGame.POGame poGame)
		{
			foreach(PlayerTask task in poGame.CurrentPlayer.Options())
			{
				Log(LogLevel.INFO, getStringFromPlayerTask(task));
			}
			
			return poGame.CurrentPlayer.Options()[Rnd.Next(poGame.CurrentPlayer.Options().Count)];
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
