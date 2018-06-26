using SabberStoneCore.Enums;
using SabberStoneCore.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabberStoneCoreAi.Agent
{
    class MCTSNode
    {
		public int visits;
		// TODO: swap this with an average that only takes the top 10 - 40% of board states after simulation
		int assumedWins;

		float ucbValue;
		POGame.POGame poGame;
		public List<MCTSNode> childNodes = new List<MCTSNode>();
		public PlayerTask playerTask;
		public MCTSNode parent;

		Random rnd = new Random();

		public MCTSNode() { }
		
		public MCTSNode(POGame.POGame poGame)
		{
			this.poGame = poGame;
		}

		public MCTSNode BuildTreeForGame(POGame.POGame poGame, float maxTime)
		{
			MCTSNode root = new MCTSNode();
			MCTSNode rootNode = root.GetRootNode();

			DateTime startTime = DateTime.UtcNow;
			while ((DateTime.UtcNow - startTime).TotalSeconds < maxTime)
			{
				// Tree Selection
				MCTSNode promisingLeaf = root.FindRandomLeafToExpand(rootNode);
				// Expansion
				//if (root.poGame.State != State.COMPLETE && root.poGame.State != State.INVALID)
				if (promisingLeaf.poGame.CurrentPlayer.Options().Count > 1    ) 
				{
					root.ExpandNode(promisingLeaf);
				}
				// Monte Carlo Simulation
				MCTSNode nodeToExplore = promisingLeaf;
				if (nodeToExplore.childNodes.Count > 0)
				{
					nodeToExplore = promisingLeaf.GetRandomChild();
				}
				float additonalNodeScore = SimulateNode(nodeToExplore);
				// BackPropagation
				nodeToExplore.BackPropagation(nodeToExplore, additonalNodeScore);
			}

			MCTSNode winnerNode = rootNode.GetChildNodeWithMaxScore();
			return winnerNode;
		}

		// TODO: simulate some random 
		public float SimulateNode(MCTSNode nodetoexplore)
		{
			return 0;
		}

		public void BackPropagation(MCTSNode nodeToExplore, float additionalNodeScore)
		{
			while (nodeToExplore.parent != null)
			{
				nodeToExplore.visits++;
				nodeToExplore = nodeToExplore.parent;
			}
		}

		public void ExpandNode(MCTSNode nodeToExpend)
		{
			nodeToExpend.poGame = nodeToExpend.parent.poGame.getCopy();
			nodeToExpend.poGame.Process(nodeToExpend.playerTask);
			if (nodeToExpend.playerTask.PlayerTaskType != PlayerTaskType.END_TURN)
			{
				List<PlayerTask> options = nodeToExpend.poGame.CurrentPlayer.Options();
				foreach (PlayerTask option in options)
				{
					MCTSNode child = new MCTSNode();
					child.playerTask = option;
					nodeToExpend.childNodes.Add(child);
					child.parent = nodeToExpend;
				}
			}
			else
			{
				// TODO: please insert code for END_TURN
			}
		}

		public MCTSNode GetRandomChild()
		{
			return childNodes[rnd.Next(childNodes.Count)];
		}

		public MCTSNode GetChildNodeWithMaxScore()
		{
			MCTSNode node = new MCTSNode();
			return node;
		}

		// Selection
		public MCTSNode FindRandomLeafToExpand(MCTSNode node)
		{
			MCTSNode child = node.childNodes[rnd.Next(node.childNodes.Count)];
			if(child.visits > 0)
			{
				return FindRandomLeafToExpand(child);
			}
			return child;
		}

		public MCTSNode GetRootNode()
		{
			if (parent == null)
				return this;
			return parent.GetRootNode();
		}
    }
}
