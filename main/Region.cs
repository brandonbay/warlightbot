using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace main
{



    public class Region
    {

        public const string gUnknownRegion = "unknown";
        public const string gNeutralRegion = "neutral";
        
        private int id;
        private List<Region> neighbours;
        private SuperRegion superRegion;
        private int armies;
        private String playerName;

        public Map.Territories Name
        {
            get { return (Map.Territories)(id - 1); }
        }

        public Region(int id, SuperRegion superRegion)
        {
            this.id = id;
            this.superRegion = superRegion;
            this.neighbours = new List<Region>();
            this.playerName = gUnknownRegion;
            this.armies = 0;

            superRegion.AddSubRegion(this);
        }

        public Region(int id, SuperRegion superRegion, String playerName, int armies)
        {
            this.id = id;
            this.superRegion = superRegion;
            this.neighbours = new List<Region>();
            this.playerName = playerName;
            this.armies = armies;

            superRegion.AddSubRegion(this);
        }

        public void AddNeighbour(Region neighbour)
        {
            if (!neighbours.Contains(neighbour))
            {
                neighbours.Add(neighbour);
                neighbour.AddNeighbour(this);
            }
        }

        /**
         * @param region a Region object
         * @return True if this Region is a neighbour of given Region, false otherwise
         */
        public bool IsNeighbour(Region region)
        {
            if (neighbours.Contains(region))
                return true;
            return false;
        }

        /**
         * @param playerName A string with a player's name
         * @return True if this region is owned by given playerName, false otherwise
         */
        public bool OwnedByPlayer(String playerName)
        {
            if (playerName.Equals(this.playerName))
                return true;
            return false;
        }

        public int Armies
        {
            set { armies = value; }
            get { return armies; }
        }

        public String PlayerName
        {
            set { playerName = value; }
            get { return playerName; }
        }

        public int Id
        {
            get { return id; }
        }

        public List<Region> Neighbours
        {
            get { return neighbours; }
        }

        public SuperRegion SuperRegion
        {
            get { return superRegion; }
        }

    }

}