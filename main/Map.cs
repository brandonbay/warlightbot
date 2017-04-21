using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace main
{


    public class Map
    {

        public List<Region> regions;
        public List<SuperRegion> superRegions;

        public Map()
        {
            this.regions = new List<Region>();
            this.superRegions = new List<SuperRegion>();
        }

        public Map(List<Region> regions, List<SuperRegion> superRegions)
        {
            this.regions = regions;
            this.superRegions = superRegions;
        }

        public enum Territories
        {
            Alaska,
            NorthwestTerritory,
            Greenland,
            Alberta,
            Ontario,
            Quebec,
            WesternUnitedStates,
            EasternUnitedStates,
            CentralAmerica,
            Venezuela,
            Peru,
            Brazil,
            Argentina,
            Iceland,
            GreatBritain,
            Scandinavia,
            Ukraine,
            WesternEurope,
            NorthernEurope,
            SouthernEurope,
            NorthAfrica,
            Egypt,
            EastAfrica,
            Congo,
            SouthAfrica,
            Madagascar,
            Ural,
            Siberia,
            Yakutsk,
            Kamchatka,
            Irkutsk,
            Kazakhstan,
            China,
            Mongolia,
            Japan,
            MiddleEast,
            India,
            Siam,
            Indonesia,
            NewGuinea,
            WesternAustralia,
            EasternAustralia
        }

        /**
         * Add a Region to the map
         * @param region : Region to be Added
         */
        public void Add(Region region)
        {
            foreach (var r in regions)
                if (r.Id == region.Id)
                {
                    Console.Error.WriteLine("Region cannot be Added: id already exists.");
                    return;
                }
            regions.Add(region);
        }

        /**
         * Add a SuperRegion to the map
         * @param superRegion : SuperRegion to be Added
         */
        public void Add(SuperRegion superRegion)
        {
            foreach (var sr in superRegions)
                if (sr.Id == superRegion.Id)
                {
                    Console.Error.WriteLine("SuperRegion cannot be Added: id already exists.");
                    return;
                }
            superRegions.Add(superRegion);
        }

        /**
         * @return : a new Map object exactly the same as this one
         */
        public Map GetMapCopy()
        {
            var newMap = new Map();
            foreach (var sr in superRegions) //copy superRegions
            {
                var newSuperRegion = new SuperRegion(sr.Id, sr.ArmiesReward);
                newMap.Add(newSuperRegion);
            }
            foreach (var r in regions) //copy regions
            {
                var newRegion = new Region(r.Id, newMap.GetSuperRegion(r.SuperRegion.Id), r.PlayerName, r.Armies);
                newMap.Add(newRegion);
            }
            foreach (var r in regions) //Add neighbours to copied regions
            {
                var newRegion = newMap.GetRegion(r.Id);
                foreach (var neighbour in r.Neighbours)
                    newRegion.AddNeighbour(newMap.GetRegion(neighbour.Id));
            }
            return newMap;
        }

        /**
         * @param id : a Region id number
         * @return : the matching Region object
         */
        public Region GetRegion(int id)
        {
            foreach (var region in regions)
                if (region.Id == id)
                    return region;
            return null;
        }

        /**
         * @param id : a SuperRegion id number
         * @return : the matching SuperRegion object
         */
        public SuperRegion GetSuperRegion(int id)
        {
            foreach (var superRegion in superRegions)
                if (superRegion.Id == id)
                    return superRegion;
            return null;
        }

        public List<Region> Regions
        {
            get { return regions; }
        }

        public List<SuperRegion> SuperRegions
        {
            get { return superRegions; }
        }

        public String MapString
        {
            get { return string.Join(" ", regions.Select(region => region.Id + ";" + region.PlayerName + ";" + region.Armies)); }
        }
    }
}