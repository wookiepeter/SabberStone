using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCoreAi.Score;

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
		/// <param name="splitAmount">number of discretes parts for this parameter</param>
		public ScoreParameter(string name, int minAmount, int maxAmount, int splitAmount = 10) 
		{
			this.min = minAmount;
			this.max = maxAmount;
			this.DiscreteValues = splitAmount;
			diff = max - min;
			if (diff % DiscreteValues != 0)
				Console.WriteLine("Parameter " + name + " is not really well discretized");
			discreteDiff = diff / DiscreteValues;
		}

		int min;
		int max;
		int diff;
		public int DiscreteValues;
		int discreteDiff;
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
				return 0;
			else
				return 1;
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

		public ScoreMatrix(List<ScoreParameter> scoreParameters, string fileName, bool loadFromFile = true)
		{
			this.loadFromFile = loadFromFile;
			if (this.loadFromFile)
			{
				dataFileName = dataString + "_" + fileName;
				propertyFileName = propertyString + "_" + fileName + ".json";
			}
			else
			{

			}
			parameters = scoreParameters;
			lengths = new int[parameters.Count];
			int totalArrayLength = 1;
			for (int i = 0; i < parameters.Count; i++)
			{
				ScoreParameter param = parameters[i];
				lengths[i] = param.DiscreteValues;
				totalArrayLength *= param.DiscreteValues;
			}
			values = new float[totalArrayLength];
		}

		public List<ScoreParameter> LoadMatrixPropertiesFromFile(string fileName)
		{
			string fileText = File.ReadAllText(Directory.GetCurrentDirectory() + @"\" + propertyFileName);
			List<ScoreParameter> result = JsonConvert.DeserializeObject < List<ScoreParameter> >(fileText);
			return result;
		}

		public void SaveMatrixPropertiesToFile(string fileName, List<ScoreParameter> parameters)
		{
			string matrixParameters = JsonConvert.SerializeObject(parameters, Formatting.Indented);
			File.WriteAllText(Directory.GetCurrentDirectory() + @"\" + propertyFileName, matrixParameters);
		}


		public void LoadScoreMatrixFromFile(string fileName)
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(Directory.GetCurrentDirectory() + @"\" + dataFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			values = (float[])formatter.Deserialize(stream);
			stream.Close();
		}

		public void SaveScoreMatrixToFile(string fileName)
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream(Directory.GetCurrentDirectory() + @"\" + dataFileName, FileMode.Create, FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, values);
			stream.Close();
		}

		public void UpdateScoreMatrix(int[] values)
		{
			if (values.Length != parameters.Count)
				throw new Exception("InvalidNumberOfValues");
			int[] indices = new int[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				indices[i] = parameters[i].GetIndex(values[i]);
			}
			int index = NDToOneD(indices, lengths);

			float score = values[index];
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
