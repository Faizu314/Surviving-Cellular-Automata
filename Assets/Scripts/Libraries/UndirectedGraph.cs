using System.Collections;
using System.Collections.Generic;

namespace Faizan314.Mathematics.GraphTheory
{
    public class UndirectedGraph<T> : IEnumerable<T> where T : class
    {
        private Dictionary<string, int> nodeIDs;
        private List<T> nodes;
        private List<int[]> edges;

        public IReadOnlyList<T> Nodes => nodes.AsReadOnly();


        public UndirectedGraph() {
            nodeIDs = new Dictionary<string, int>();
            nodes = new List<T>();
            edges = new List<int[]>();
        }
        
        /// <summary>
        /// Maps the nodeID to the node for later reference
        /// </summary>
        /// <param name="nodeID">ID of the node to add</param>
        /// <param name="node">Node to add</param>
        /// <returns>Returns false if nodeID is not unique</returns>
        public bool Add(string nodeID, T node)
        {
            if (nodeIDs.ContainsKey(nodeID))
                return false;
            nodeIDs.Add(nodeID, nodes.Count);
            nodes.Add(node);

            return true;
        }

        /// <summary>
        /// Adds a node and creates an edge from it to the given one
        /// </summary>
        /// <param name="nodeID">ID of the node to add</param>
        /// <param name="node">Node to add</param>
        /// <param name="attachTo">ID of the node to create the edge to</param>
        /// <returns>Returns false if the new nodeID is not unique or if the other ID was not found</returns>
        public bool AddAndAttachTo(string nodeID, T node, string attachTo)
        {
            if (!nodeIDs.ContainsKey(attachTo))
                return false;

            if (!Add(nodeID, node))
                return false;

            return CreateEdge(nodeID, attachTo);
        }

        /// <summary>
        /// This method will not cache an ID for the node
        /// </summary>
        /// <param name="node">Node to add</param>
        /// <returns>Returns false if the node does not get added</returns>
        public bool Add(T node)
        {
            if (nodes.Contains(node))
                return false;

            nodes.Add(node);

            return true;
        }

        /// <summary>
        /// This method will not cache an ID for the node
        /// </summary>
        /// <param name="node">Node to add</param>
        /// <param name="attachTo">Index of the node to attach the new node</param>
        /// <returns>Returns false if the node does not get added</returns>
        public bool AddAndAttachTo(T node, int attachTo)
        {
            if (!Add(node))
                return false;

            return CreateEdge(nodes.Count - 1, attachTo);
        }


        public bool CreateEdge(string nodeA, string nodeB)
        {
            int[] edge = new int[2] { 
                GetIndex(nodeA), 
                GetIndex(nodeB) 
            };

            if (edge[0] < 0 || edge[1] < 0)
                return false;

            edges.Add(edge);
            return true;
        }

        private bool CreateEdge(int nodeA, int nodeB)
        {
            int[] edge = new int[2] {
                nodeA,
                nodeB
            };

            edges.Add(edge);
            return true;
        }

        /// <summary>
        /// Removes the node with the given nodeID and all the connections to it
        /// </summary>
        /// <param name="nodeID">ID of the node to remove</param>
        /// <returns>Returns false if the given nodeID was not found</returns>
        public bool RemoveNode(string nodeID)
        {
            int nodeIndex = GetIndex(nodeID);
            if (nodeIndex < 0)
                return false;
            nodes.RemoveAt(nodeIndex);
            RemoveEdges(nodeIndex);
            return true;
        }

        /// <summary>
        /// Removes all edges to the node with this index
        /// </summary>
        /// <param name="nodeIndex">All edges containing node of this index will be removed</param>
        private void RemoveEdges(int nodeIndex)
        {
            for (int i = 0; i < edges.Count;)
            {
                if (edges[i][0] == nodeIndex || edges[i][1] == nodeIndex)
                    edges.RemoveAt(i);
                else
                    i++;
            }
        }

        /// <summary>
        /// Gets the index of the node with the given nodeID
        /// </summary>
        /// <param name="nodeID">ID of the node</param>
        /// <returns>Returns -1 if the nodeID was not found</returns>
        private int GetIndex(string nodeID)
        {
            if (!nodeIDs.ContainsKey(nodeID))
                return -1;
            return nodeIDs[nodeID];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}