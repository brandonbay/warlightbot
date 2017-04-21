using System;
using System.Collections.Generic;
using System.Linq;
using main;
using move;

namespace bot
{
    public class BotState
    {
        private readonly Map fullMap = new Map();
            // This map is known from the start, contains all the regions and how they are connected, doesn't change after initialization

        private readonly List<Move> opponentMoves; // List of all the opponent's moves, reset at the end of each round
        private readonly List<Region> pickableStartingRegions;
            // 2 randomly chosen regions from each superregion are given, which the bot can chose to start with
            // This map represents everything the player can see, and assumptions based on previous information.
        private String myName = "";
        private String opponentName = "";

        private int roundNumber;
        private int startingArmies; // Number of armies the player can place on map
        private int defaultStartingArmies = 5; // Number of armies the player can place on map by default
        private int armiesOpponentDeployed = 5;
        private Map visibleMap; // This map represents everything the player can see, updated at the end of each round.
        private Map knowledgeMap;

        private bool aggressiveModeOn = false;
        private bool opponentAttacked = false;

        private Dictionary<Region, int> threatenedRegions;
        private Dictionary<int, Region> expansionRegions;
        private List<Region> internalRegions;
        private Dictionary<Map.Territories, int> regionRank;

        public BotState()
        {
            pickableStartingRegions = new List<Region>();
            opponentMoves = new List<Move>();
            roundNumber = 0;
        }

        public String MyPlayerName
        {
            get { return myName; }
        }

        public String GetOpponentPlayerName
        {
            get { return opponentName; }
        }

        public int StartingArmies
        {
            get { return startingArmies; }
        }

        public int DefaultStartingArmies
        {
            get { return defaultStartingArmies; }
        }

        public int RoundNumber
        {
            get { return roundNumber; }
        }

        public bool AggressiveMode
        {
            get { return aggressiveModeOn; }
            set { aggressiveModeOn = value; }
        }

        public Map KnowledgeMap
        {
            get { return knowledgeMap; }
        }

        public Map VisibleMap
        {
            get { return visibleMap; }
        }

        public Map FullMap
        {
            get { return fullMap; }
        }

        public List<Move> OpponentMoves
        {
            get { return opponentMoves; }
        }

        public List<Region> PickableStartingRegions
        {
            get { return pickableStartingRegions; }
        }

        public Dictionary<Region, int> ThreatenedRegions
        {
            get { return threatenedRegions; }
        }

        public Dictionary<int, Region> ExpansionRegions
        {
            get { return expansionRegions; }
        }

        public List<Region> InternalRegions
        {
            get { return internalRegions; }
        }

        public Dictionary<Map.Territories, int> RegionRank
        {
            get { return regionRank; }
        }

        public void UpdateSettings(String key, String value)
        {
            switch(key)
            {
                case "your_bot":
                    myName = value;
                    break;
                case "opponent_bot":
                    opponentName = value;
                    break;
                case "starting_armies":
                    startingArmies = int.Parse(value);
                    roundNumber++; // Next round
                    break;
            }
        }

        // Initial map is given to the bot with all the information except for player and armies info
        public void SetupMap(String[] mapInput)
        {
            int i, regionId, superRegionId, reward;

            if(mapInput[1].Equals("super_regions"))
            {
                for(i = 2; i < mapInput.Length; i++)
                {
                    try
                    {
                        superRegionId = int.Parse(mapInput[i]);
                        i++;
                        reward = int.Parse(mapInput[i]);
                        fullMap.Add(new SuperRegion(superRegionId, reward));
                    }
                    catch(Exception e)
                    {
                        Console.Error.WriteLine("Unable to parse SuperRegions " + e);
                    }
                }
            }
            else if(mapInput[1].Equals("regions"))
            {
                for(i = 2; i < mapInput.Length; i++)
                {
                    try
                    {
                        regionId = int.Parse(mapInput[i]);
                        i++;
                        superRegionId = int.Parse(mapInput[i]);
                        var superRegion = fullMap.GetSuperRegion(superRegionId);
                        fullMap.Add(new Region(regionId, superRegion));
                    }
                    catch(Exception e)
                    {
                        Console.Error.WriteLine("Unable to parse Regions " + e);
                    }
                }
            }
            else if(mapInput[1].Equals("neighbors"))
            {
                for(i = 2; i < mapInput.Length; i++)
                {
                    try
                    {
                        var region = fullMap.GetRegion(int.Parse(mapInput[i]));
                        i++;
                        var neighbourIds = mapInput[i].Split(',');
                        for(var j = 0; j < neighbourIds.Length; j++)
                        {
                            var neighbour = fullMap.GetRegion(int.Parse(neighbourIds[j]));
                            region.AddNeighbour(neighbour);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.Error.WriteLine("Unable to parse Neighbours " + e);
                    }
                }
            }
        }

        // Regions from wich a player is able to pick his preferred starting regions
        public void SetPickableStartingRegions(String[] mapInput)
        {
            for(var i = 2; i < mapInput.Length; i++)
            {
                try
                {
                    var regionId = int.Parse(mapInput[i]);
                    var pickableRegion = fullMap.GetRegion(regionId);
                    pickableStartingRegions.Add(pickableRegion);
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine("Unable to parse pickable regions " + e);
                }
            }
        }

        // Visible regions are given to the bot with player and armies info
        public void UpdateMap(String[] mapInput)
        {
            if(knowledgeMap == null)
            {
                knowledgeMap = fullMap.GetMapCopy();
            }
            visibleMap = knowledgeMap.GetMapCopy();
            for(var i = 1; i < mapInput.Length; i++)
            {
                try
                {
                    var region = visibleMap.GetRegion(int.Parse(mapInput[i]));
                    var playerName = mapInput[i + 1];
                    var armies = int.Parse(mapInput[i + 2]);

                    region.PlayerName = playerName;
                    region.Armies = armies;
                    i += 2;
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine("Unable to parse Map Update " + e);
                }
            }
            var unknownRegions =
                visibleMap.Regions.Where(region => region.PlayerName.Equals(Region.gUnknownRegion)).ToList();

            // Remove regions which are unknown.
            foreach(var unknownRegion in unknownRegions)
            {
                visibleMap.Regions.Remove(unknownRegion);
            }

            // Update assumed map
            foreach(var region in visibleMap.Regions.Where(region => !region.PlayerName.Equals(Region.gUnknownRegion)))
            {
                knowledgeMap.GetRegion(region.Id).PlayerName = region.PlayerName;
                knowledgeMap.GetRegion(region.Id).Armies = region.Armies;
            }

            foreach(
                var region in
                    knowledgeMap.Regions.Where(r => r.PlayerName.Equals(myName) && visibleMap.Regions.All(s => s.Id != r.Id)))
            {
                region.PlayerName = GetOpponentPlayerName;
                region.Armies = 0;
            }
        }

        // Parses a list of the opponent's moves every round.
        // Clears it at the start, so only the moves of this round are stored.
        public void ReadOpponentMoves(String[] moveInput)
        {
            opponentMoves.Clear();
            armiesOpponentDeployed = 0;
            for(var i = 1; i < moveInput.Length; i++)
            {
                try
                {
                    var move = new Move();
                    string playerName;
                    int armies;

                    switch(Move.GetMoveType((moveInput[i + 1])))
                    {
                        case Move.gMoveTypes.Place:
                            var region = visibleMap.GetRegion(int.Parse(moveInput[i + 2]));
                            playerName = moveInput[i];
                            armies = int.Parse(moveInput[i + 3]);
                            move = new PlaceArmiesMove(playerName, region, armies);
                            armiesOpponentDeployed += armies;
                            i += 3;
                            break;
                        case Move.gMoveTypes.AttackTransfer:
                            var fromRegion = visibleMap.GetRegion(int.Parse(moveInput[i + 2])) ??
                                                fullMap.GetRegion(int.Parse(moveInput[i + 2]));
                            var toRegion = visibleMap.GetRegion(int.Parse(moveInput[i + 3])) ??
                                              fullMap.GetRegion(int.Parse(moveInput[i + 3]));
                            playerName = moveInput[i];
                            armies = int.Parse(moveInput[i + 4]);
                            move = new AttackTransferMove(playerName, fromRegion, toRegion, armies);
                            i += 4;
                            break;
                    }

                    opponentMoves.Add(move);
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine("Unable to parse Opponent moves " + e);
                }
            }
            if(opponentMoves.Any(m => m.MoveType.Equals(Move.gMoveTypes.AttackTransfer) &&
                                      MyRegions().Contains(((AttackTransferMove)m).ToRegion)))
            {
                opponentAttacked = true;
            }
            else
            {
                opponentAttacked = false;
            }
        }

        public List<Region> MyRegions()
        {
            return VisibleMap.Regions.Where(region => region.OwnedByPlayer(myName)).ToList();
        }

        public List<Region> KnownEnemyRegions()
        {
            return KnowledgeMap.Regions.Where(region => region.OwnedByPlayer(opponentName)).ToList();
        }

        public List<Region> BorderTerritories()
        {
            return MyRegions().Where(region => region.Neighbours.Any(r => r.OwnedByPlayer(opponentName))).ToList();
        }

        public int EnemyReinforcements()
        {
            var enemyReinforcements = defaultStartingArmies;
            foreach(var superRegion in KnowledgeMap.SuperRegions.Where(sr => sr.OwnedByPlayer().Equals(GetOpponentPlayerName)))
            {
                enemyReinforcements += superRegion.ArmiesReward;
            }
            return Math.Max(enemyReinforcements, armiesOpponentDeployed);
        }

        public void UpdateAggression()
        {
            if(threatenedRegions.Count > 1 || opponentAttacked || RoundNumber > 10 
                || KnowledgeMap.SuperRegions.Count(s => s.OwnedByPlayer().Equals(myName)) > 1)
            {
                AggressiveMode = true;
            }
            else if(BorderTerritories().Count == 0)
            {
                AggressiveMode = false;
            }
        }

        public void UpdateRegions()
        {

            UpdateThreat();

            internalRegions = MyRegions().Where(r => r.Neighbours.All(rn => rn.OwnedByPlayer(myName))).ToList();

            UpdateExpansionRegions();

            UpdateRegionRank();
        }

        public void UpdateThreat()
        {
            threatenedRegions = new Dictionary<Region, int>();
            foreach(var borderRegion in BorderTerritories())
            {
                var possibleThreat = 0;
                foreach(var enemyNeighbour in borderRegion.Neighbours.Where(r => r.OwnedByPlayer(opponentName)))
                {
                    possibleThreat += enemyNeighbour.Armies - 1;
                }
                possibleThreat += EnemyReinforcements();
                if(possibleThreat >= borderRegion.Armies)
                {
                    threatenedRegions.Add(borderRegion, possibleThreat);
                }
            }
        }

        private void UpdateExpansionRegions()
        {
            expansionRegions = new Dictionary<int, Region>();

            List<Region> tempExpansionRegions = MyRegions().Where(region => !BorderTerritories().Contains(region) &&
                                                                            !internalRegions.Contains(region)).ToList();
            var iterator = 0;

            foreach(var tempRegion in
                tempExpansionRegions.OrderBy(region => BotStarter.FavouriteTerritories.IndexOf(region.Name)))
            {
                expansionRegions.Add(iterator, tempRegion);
                iterator++;
            }

            if(expansionRegions.Count == 0)
            {
                expansionRegions.Add(0, BorderTerritories().FirstOrDefault());
                if(expansionRegions[0] == null)
                    expansionRegions[0] =
                        MyRegions().OrderBy(r => BotStarter.FavouriteTerritories.IndexOf(r.Name)).FirstOrDefault();
            }

            if(expansionRegions.Count == 1)
            {
                expansionRegions.Add(1, expansionRegions[0]);
            }
        }

        private void UpdateRegionRank()
        {
            if(regionRank == null)
            {
                regionRank = new Dictionary<Map.Territories, int>();
                foreach(var region in FullMap.Regions)
                {
                    regionRank.Add(region.Name, 0);
                }
            }

            //Reset regions
            foreach(var region in MyRegions())
            {
                regionRank[region.Name] = 0;
            }

            //Rank border regions highly
            foreach(var region in BorderTerritories())
            {
                if(ThreatenedRegions.ContainsKey(region))
                {
                    regionRank[region.Name] = ThreatenedRegions[region] * 10;
                }
                else
                {
                    regionRank[region.Name] = region.Neighbours.Count(r => r.OwnedByPlayer(opponentName)) * 10
                                         + region.Neighbours.Sum(r => r.Armies) 
                                         + EnemyReinforcements()
                                         - region.Armies;
                }
                if(AggressiveMode)
                {
                    regionRank[region.Name] = 5 * regionRank[region.Name];
                }
            }

            //Rank expansion territories medium
            foreach(var region in ExpansionRegions.Values)
            {
                var favouriteRank = 1 /
                                    (((double)BotStarter.FavouriteTerritories.IndexOf(region.Name) + 1) /
                                        (double)BotStarter.FavouriteTerritories.Count);
                if(favouriteRank > 20)
                    favouriteRank = favouriteRank / 2;
                favouriteRank += 5 * region.Armies;
                regionRank[region.Name] = Math.Max(regionRank[region.Name], (int)favouriteRank);
            }

            UpdateRankForSuperRegions();

            //Rank internal regions based on proximity to front line
            while(MyRegions().Any(r => regionRank[r.Name] == 0))
            {
                foreach(var region in MyRegions().Where(r => regionRank[r.Name] == 0))
                {
                    var topNeighbour = region.Neighbours.OrderByDescending(r => regionRank[r.Name]).First();
                    if(topNeighbour == null)
                        throw new Exception("The region has no top neighbour!");
                    regionRank[region.Name] = (int)Math.Ceiling(regionRank[topNeighbour.Name] / 2.0);
                }
            }
        }

        private void UpdateRankForSuperRegions()
        {
            //Rank near-complete bonus regions highly
            foreach(var superRegion in KnowledgeMap.SuperRegions)
            {
                var territoriesOwned = superRegion.SubRegions.Count(r => r.PlayerName.Equals(MyPlayerName));
                var territoriesLeft = superRegion.SubRegions.Count - territoriesOwned;
                var rank = territoriesLeft != 0 ?
                    Math.Pow((superRegion.ArmiesReward + territoriesOwned), 2) / Math.Pow(territoriesLeft, 2) : 0;

                if(territoriesLeft == 1)
                    rank = rank * 100;

                foreach(var subRegion in superRegion.SubRegions.Where(r => !internalRegions.Contains(r)))
                {
                    regionRank[subRegion.Name] = Math.Max(regionRank[subRegion.Name], (int)rank * subRegion.Armies);
                }
            }

            //Rank up incomplete superbonuses containing no enemy territories
            foreach(var superRegion in KnowledgeMap.SuperRegions.Where(s => !s.OwnedByPlayer().Equals(myName)
                                                                       && !s.SubRegions.Any(r => r.OwnedByPlayer(opponentName)))
                )
            {
                foreach(var subRegion in superRegion.SubRegions)
                {
                    regionRank[subRegion.Name] = 2 * regionRank[subRegion.Name];
                }
            }

            //Rank higher any non-internal territory in a complete bonus
            foreach(var region in MyRegions().Except(InternalRegions))
            {
                if(region.SuperRegion.OwnedByPlayer().Equals(myName))
                {
                    regionRank[region.Name] = regionRank[region.Name] * region.SuperRegion.ArmiesReward;
                }
            }
        }
    }
}
