using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowSystem
{
    [CreateAssetMenu(menuName = "Flow/BlockGraph", fileName = "BlockGraph")]
    public class BlockGraph : ScriptableObject
    {
        [Serializable]
        public struct Rule
        {
            public FlowBlock from;
            public string outcomeKey;
            public FlowBlock to;
        }

        public List<Rule> rules = new();

        public FlowBlock Resolve(FlowBlock from, string outcomeKey)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (r.from == from && r.outcomeKey == outcomeKey) return r.to;
            }
            return null;
        }
    }
}