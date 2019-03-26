using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NodeType { Null, Land, Air, Floating, WaterCave, Cave }

namespace WormsClone
{
    public class Node
    {

        public bool Solid;
        public NodeType Type;
        public Vector2Int Position;
        //public bool IsTopNode;
        //public bool IsBottomNode;
        //public bool IsUnderAir;
		public bool InRegion;
		public RectObject Object;

        public Node(bool solid, Vector2Int pos)
        {
            Solid = solid;
            Position = pos;
        }

		public bool IsSolid(RectObject self, bool ignoreObjects = false, System.Func<RectObject, bool> ignoreCondition = null)
		{
			if (Solid)
				return true;
			if (Object && Object != self && !ignoreObjects)
			{
				if (ignoreCondition != null)
				{
					bool ignoreObject = ignoreCondition(Object);
					if (!ignoreObject)
						return true;
					else if (ignoreObject)
						return false;
				}
				else
					return true;
			}
			return false;
		}

        public static implicit operator bool(Node n)
        {
            if (n == null)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "Node Type : " + Type + ", pos : " + Position.ToString() + ", solid : " + Solid;
        }
    }

	public class NodeMass
	{
		public bool Solid;
		public NodeType Type;
		public List<Node> Mass;
		public int Size
		{
			get
			{
				if (Mass != null)
					return Mass.Count;
				else return -1;
			}
		}

		public NodeMass(List<Node> nodes, bool solid, NodeType type, int size)
		{
			Mass = nodes;
			Solid = solid;
			Type = type;
		}

		public override string ToString()
		{
			return "Mass, type : " + Type + ", solid : " + Solid + ", size : " + Size;
		}
	}
}
