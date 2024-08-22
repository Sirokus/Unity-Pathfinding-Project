using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mine
{
    public class Tile : MonoBehaviour
    {
        private SpriteRenderer sr;
        public bool walkable;
        public bool visited;
        public Tile pre;

        public float cost = 1;
        public bool randomCost = true;

        //A*Ыљаш
        public float G;
        public float H;
        public float F => G + H;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.color = SetCostAndGetColor(randomCost ? Random.Range(0, 5) : 1);
        }

        Color SetCostAndGetColor(float cost)
        {
            this.cost = cost;

            return randomCost ? Color.Lerp(Color.white, Color.gray, (float)(cost / 5)) : Color.white;
        }

        public void setWalkable(bool canWalk)
        {
            walkable = canWalk;
            Color color = canWalk ? Color.white : Color.black;
            sr.color = Color.Lerp(SetCostAndGetColor(cost), color, canWalk ? 0.1f : 1f);
        }

        public void setColor(Color color, float lerp = 0.1f)
        {
            sr.color = Color.Lerp(SetCostAndGetColor(cost), color, lerp);
        }

        public void setRealColor(Color color)
        {
            sr.color = color;
        }

        public int GetManhattanDistance(Tile tile)
        {
            Vector2Int aCoord = Grid.getCoordByTile(this);
            Vector2Int bCoord = Grid.getCoordByTile(tile);
            return Mathf.Abs(aCoord.x - bCoord.x) + Mathf.Abs(aCoord.y - bCoord.y);
        }
    }
}

