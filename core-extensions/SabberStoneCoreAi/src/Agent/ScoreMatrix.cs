using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCoreAi.Score;
using System.Linq;

using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SabberStoneCoreAi.Agent
{
	class ScoreParameter : IComparable<ScoreParameter>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Name of this Parameter</param>
		/// <param name="minAmount">inclusive</param>
		/// <param name="maxAmount">inclusive</param>
		/// <param name="splitAmount">number of discretes parts for this parameter, always adds one more area for 0 (because math)</param>
		public ScoreParameter(string name, int minAmount, int maxAmount, int splitAmount = 10, bool zeroSlot = false) 
		{
			this.Name = name;
			this.min = minAmount;
			this.max = maxAmount;
			this.DiscreteValues = splitAmount;
			diff = max - min;
			if (diff % DiscreteValues != 0)
				Console.WriteLine("Parameter " + name + " is not really well discretized");
			discreteDiff = diff / DiscreteValues;
			if (zeroSlot == true)
				DiscreteValues += 1;
		}

		public ScoreParameter()
		{

		}

		public int min;
		public int max;
		public int diff;
		public int DiscreteValues;
		public int discreteDiff;
		public string Name;

		public int GetIndex(int value)
		{
			int index = (diff - Math.Abs(value - max)) / discreteDiff;
			index = Math.Clamp(value, 0, DiscreteValues - 1);
			return index;
		}

		public int CompareTo(ScoreParameter other)
		{
			if (this.min == other.min && this.max == other.max && this.DiscreteValues == other.DiscreteValues && this.Name.Equals(other.Name))
			{
				return 0;
			}
			else
			{
				return 1;
			}
		}
	}

    class ScoreMatrix
    {
		private float[] values;
		private int[] lengths;

		public bool loadFromFile;

		private string dataString = "data";
		private string dataFileName;
		private string propertyString = "properties";
		private string propertyFileName;

		private List<ScoreParameter> parameters;

		List<int> visitedIndices;

		public ScoreMatrix(List<ScoreParameter> scoreParameters, string fileName, bool loadFromFile = true)
		{
			this.loadFromFile = loadFromFile;
			dataFileName = dataString + "_" + fileName;
			propertyFileName = propertyString + "_" + fileName + ".json";

			InitPropertiesAndValues(scoreParameters);
		}

		public void SaveScoreMatrix()
		{
			SaveMatrixPropertiesToFile(propertyFileName, parameters);
			SaveScoreMatrixToFile(dataFileName);
		}

		void InitPropertiesAndValues(List<ScoreParameter> scoreParameters)
		{
			bool canLoadFromFile = false;
			if (this.loadFromFile && CheckIfFilesExist())
			{
				List<ScoreParameter> loadedParameters = LoadMatrixPropertiesFromFile(propertyFileName);
				// I do hate Sequencial Comparisons... 
				bool parametersAreEqual = scoreParameters.Count == loadedParameters.Count;
				for(int i = 0; i < scoreParameters.Count; i++)
				{
					if (scoreParameters[i].CompareTo(loadedParameters[i]) != 0)
						parametersAreEqual = false;
				}
				if (parametersAreEqual)
				{
					canLoadFromFile = true;
				}
				else
				{
					Console.WriteLine("File " + dataFileName + " does not contain proper Parameters ");
				}
			}

			parameters = scoreParameters;
			lengths = new int[parameters.Count];
			long totalArrayLength = 1;
			for (int i = 0; i < parameters.Count; i++)
			{
				ScoreParameter param = parameters[i];
				lengths[i] = param.DiscreteValues;
				totalArrayLength *= lengths[i];
			}

			if (canLoadFromFile == false)
			{
				values = new float[totalArrayLength];
			}
			else
			{
				LoadScoreMatrixFromFile(dataFileName);
			}
			visitedIndices = new List<int>();
		}

		List<ScoreParameter> LoadMatrixPropertiesFromFile(string fileName)
		{
			string fileText = File.ReadAllText(Directory.GetCurrentDirectory() + @"\" + propertyFileName);
			List<ScoreParameter> result = JsonConvert.DeserializeObject < List<ScoreParameter> >(fileText);
			return result;
		}

		void SaveMatrixPropertiesToFile(string fileName, List<ScoreParameter> parameters)
		{
			string matrixParameters = JsonConvert.SerializeObject(parameters, Formatting.Indented);
			File.WriteAllText(Directory.GetCurrentDirectory() + @"\" + propertyFileName, matrixParameters);
		}

		void LoadScoreMatrixFromFile(string fileName)
		{
			Console.WriteLine("Loading ScoreMatrix from " + dataFileName);
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(Directory.GetCurrentDirectory() + @"\" + dataFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			values = (float[])formatter.Deserialize(stream);
			stream.Close();

			SaveScoreMatrixToFile(fileName + "_backup");
		}

		void SaveScoreMatrixToFile(string fileName)
		{
			Console.WriteLine("Saving Score Matrix to file " + fileName);

			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(Directory.GetCurrentDirectory() + @"\" + fileName, FileMode.Create, FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, values);
			stream.Close();
		}

		bool CheckIfFilesExist()
		{
			return File.Exists(Directory.GetCurrentDirectory() + @"\" + propertyFileName) && File.Exists(Directory.GetCurrentDirectory() + @"\" + dataFileName);
		}

		public float GetScoreFromMatrix(int[] values)
		{
			int index = ComputeIndexFromValues(values);

			return this.values[index];
		}

		/// <summary>
		/// Adds a state in the matrix to the visited states
		/// </summary>
		/// <param name="values"></param>
		public void VisitState(int[] values)
		{
			visitedIndices.Add(ComputeIndexFromValues(values));
		}

		/// <summary>
		/// Updates the Score Matrix after the game has ended
		/// </summary>
		/// <param name="reward"></param>
		public void UpdateScoreMatrix(float reward, float alpha)
		{
			foreach (int index in visitedIndices)
			{
				// using Constant Alpha Monte carlo Method
				values[index] += alpha * (reward - values[index]);
			}
			visitedIndices.Clear();
		}

		int ComputeIndexFromValues(int[] values)
		{
			if (values.Length != parameters.Count)
				throw new Exception("InvalidNumberOfValues");
			int[] indices = new int[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				indices[i] = parameters[i].GetIndex(values[i]);
			}
			return NDToOneD(indices, lengths);
		}

		int NDToOneD(int[] indices, int[] lengths)
		{
			int ID = 0;
			for (int i = 0; i < indices.Length; i++)
			{
				int offset = 1;
				for (int j = 0; j < i; j++)
				{
					offset *= lengths[j];
				}
				ID += indices[i] * offset;
			}
			return ID;
		}
	}
}
