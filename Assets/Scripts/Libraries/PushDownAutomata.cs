using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Faizan314.Mathematics.Automata
{
    public class PushDownAutomata<T, V> where T : class
    {
        private struct Transition
        {
            public int startId, destId;
            public V requirement;
            public T toPush, toPop;
            public Action<PushDownAutomata<T, V>> action;
            public Transition(int startId, int destId, V requirement, T toPush, T toPop, Action<PushDownAutomata<T, V>> action)
            {
                this.startId = startId;
                this.destId = destId;
                this.requirement = requirement;
                this.toPush = toPush;
                this.toPop = toPop;
                this.action = action;
            }
        }

        private Stack<T> _stack = new Stack<T>();
        private List<string> _states = new List<string>();
        private List<Transition> _transitions = new List<Transition>();
        //This is the index of the state in the list of states NOT the id
        private int _currStateIndex = 0;

        public PushDownAutomata()
        {

        }
        public void AddState(string stateName)
        {
            _states.Add(stateName);
        }
        public void AddTransition(string startName, string destinationName, V requirement, T toPush, T toPop, Action<PushDownAutomata<T, V>> action)
        {
            int startStateIndex = _states.IndexOf(startName);
            int destStateIndex = _states.IndexOf(destinationName);

            
            if (startStateIndex == -1 || destStateIndex == -1)
            {
                Debug.LogError("You are adding transition between states that do not exist");
                return;
            }

            _transitions.Add(new Transition(startStateIndex, destStateIndex, requirement, toPush, toPop, action));
        }
        public void SetStartState(string startStateName)
        {
            int startStateIndex = _states.IndexOf(startStateName);

            if (startStateIndex == -1)
            {
                Debug.LogError("State name not found");
                return;
            }

            _currStateIndex = startStateIndex;
        }

    }
}