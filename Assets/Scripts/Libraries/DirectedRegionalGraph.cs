using System.Collections.Generic;

namespace Faizan314.GraphTheory
{
    public class DirectedRegionalGraph
    {
        private List<Node<int>> nodes = new List<Node<int>>();

        public int AddNode()
        {
            int nodeID = nodes.Count;
            Node<int> addition = new Node<int>(nodeID);
            nodes.Add(addition);
            return nodeID;
        }
        public void AddEdge(int node1, int node2)
        {
            if (nodes.Count <= node1 || nodes.Count <= node2)
                return;
            if (!nodes[node1].next.Contains(nodes[node2]))
            {
                nodes[node1].next.Add(nodes[node2]);
                nodes[node2].next.Add(nodes[node1]);
            }
        }
        // returns the ID of the nodes that got separated after the removal
        public int[] TryRemoveNode(int nodeID)
        {
            return null;
        }
        private void BFS(int rootNodeID, int[] openNodes)
        {

        }
    }
    public class Node<T> where T : struct
    {
        public T regionID;
        public List<Node<T>> next;
        public Node(T regionID)
        {
            this.regionID = regionID;
            next = new List<Node<T>>();
        }
    }
}
