using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using main;
using move;

namespace bot
{

    /**
     * This is a simple bot that does random (but correct) moves.
     * This class implements the Bot interface and overrides its Move methods.
     * You can implements these methods yourself very easily now,
     * since you can retrieve all information about the match from variable “state”.
     * When the bot decided on the move to make, it returns a List of Moves. 
     * The bot is started by creating a Parser to which you Add
     * a new instance of your bot, and then the parser is started.
     */

    public class BotStarter : Bot
    {

        private const int cNumberOfStartingRegions = 6;
        public static readonly List<Map.Territories> FavouriteTerritories = new List<Map.Territories>
        {
            Map.Territories.Indonesia,
            Map.Territories.NewGuinea,
            Map.Territories.WesternAustralia,
            Map.Territories.EasternAustralia,
            Map.Territories.Ukraine,
            Map.Territories.Iceland,
            Map.Territories.WesternEurope,
            Map.Territories.SouthernEurope,
            Map.Territories.GreatBritain,
            Map.Territories.Scandinavia,
            Map.Territories.NorthernEurope,
            Map.Territories.Brazil,
            Map.Territories.Venezuela,
            Map.Territories.Peru,
            Map.Territories.Argentina,
            Map.Territories.Siam,
            Map.Territories.NorthAfrica,
            Map.Territories.Egypt,
            Map.Territories.EastAfrica,
            Map.Territories.Congo,
            Map.Territories.SouthAfrica,
            Map.Territories.Madagascar,
            Map.Territories.Alaska,
            Map.Territories.Greenland,
            Map.Territories.CentralAmerica,
            Map.Territories.NorthwestTerritory,
            Map.Territories.Alberta,
            Map.Territories.Quebec,
            Map.Territories.WesternUnitedStates,
            Map.Territories.EasternUnitedStates,
            Map.Territories.Ontario,
            Map.Territories.Kamchatka,
            Map.Territories.MiddleEast,
            Map.Territories.Ural,
            Map.Territories.Kazakhstan,
            Map.Territories.Yakutsk,
            Map.Territories.Irkutsk,
            Map.Territories.China,
            Map.Territories.Mongolia,
            Map.Territories.Siberia,
            Map.Territories.Japan,
            Map.Territories.India,
        };

        /**
         * A method used at the start of the game to decide which player start with what Regions. 6 Regions are required to be returned.
         * This example randomly picks 6 regions from the pickable starting Regions given by the engine.
         * @return : a list of m (m=6) Regions starting with the most preferred Region and ending with the least preferred Region to start with 
         */
        public List<Region> GetPreferredStartingRegions(BotState state, long timeOut)
        {
            var numberOfSelectedTerritories = 0;
            var preferredStartingRegions = new List<Region>();
            foreach(var territory in FavouriteTerritories)
            {
                if(numberOfSelectedTerritories <= cNumberOfStartingRegions &&
                    state.PickableStartingRegions.Contains(state.FullMap.GetRegion((int)territory)))
                {
                    var region = state.FullMap.GetRegion((int)territory);
                    preferredStartingRegions.Add(region);
                    numberOfSelectedTerritories++;
                }
            }

            return preferredStartingRegions;
        }

        /**
         * This method is called for at first part of each round. This example puts two armies on random regions
         * until he has no more armies left to place.
         * @return The list of PlaceArmiesMoves for one round
         */
        public List<PlaceArmiesMove> GetPlaceArmiesMoves(BotState state, long timeOut)
        {
            var placeArmiesMoves = new List<PlaceArmiesMove>();
            var armiesLeft = state.StartingArmies;

            var topExpansionRegion = state.ExpansionRegions.Values.Where(r => r.Armies == 4).OrderByDescending(r => state.RegionRank[r.Name]).FirstOrDefault();
            if(topExpansionRegion != null)
            {
                placeArmiesMoves.Add(new PlaceArmiesMove(state.MyPlayerName, topExpansionRegion, 1));
                topExpansionRegion.Armies += 1;
                armiesLeft -= 1;
            }

            foreach(var region in state.MyRegions().OrderByDescending(r => state.RegionRank[r.Name]))
            {
                if(armiesLeft == 0)
                    break;
                var armiesToAdd = 0;
                if(state.BorderTerritories().Contains(region))
                {
                    if(state.ThreatenedRegions.ContainsKey(region))
                    {
                        armiesToAdd = Math.Max(state.ThreatenedRegions[region] - region.Armies + 1, 1);
                    }
                    var amountToOverwhelm = (int)(region.Neighbours.Max(r => r.Armies) / 0.7) - region.Armies - armiesToAdd + 1;
                    armiesToAdd += Math.Max(amountToOverwhelm, 0);
                    armiesToAdd = Math.Min(armiesToAdd, armiesLeft);
                    placeArmiesMoves.Add(new PlaceArmiesMove(state.MyPlayerName, region, armiesToAdd));
                    region.Armies += armiesToAdd;
                    armiesLeft -= armiesToAdd;
                    state.UpdateThreat();
                }
                else if(state.ExpansionRegions.ContainsValue(region))
                {
                    if(region.Armies < 5)
                    {
                        armiesToAdd = Math.Min(5 - region.Armies, armiesLeft);
                        if (region.Neighbours.Any(r => r.OwnedByPlayer(Region.gNeutralRegion) && r.Armies == 1))
                            armiesToAdd = Math.Min(4 - region.Armies, armiesLeft);
                        placeArmiesMoves.Add(new PlaceArmiesMove(state.MyPlayerName, region, armiesToAdd));
                        region.Armies += armiesToAdd;
                        armiesLeft -= armiesToAdd;
                    }
                }
            }

            if(armiesLeft > 0)
            {
                var topRegion = state.MyRegions().OrderByDescending(r => state.RegionRank[r.Name]).First();
                placeArmiesMoves.Add(new PlaceArmiesMove(state.MyPlayerName, topRegion, armiesLeft));
            }

            return placeArmiesMoves;
        }

        /**
         * This method is called for at the second part of each round. This example attacks if a region has
         * more than 6 armies on it, and transfers if it has less than 6 and a neighbouring owned region.
         * @return The list of PlaceArmiesMoves for one round
         */
        public List<AttackTransferMove> GetAttackTransferMoves(BotState state, long timeOut)
        {
            state.UpdateRegions();
            var attackTransferMoves = new List<AttackTransferMove>();
            var myName = state.MyPlayerName;

            if(state.AggressiveMode)
            {

                foreach(var region in state.BorderTerritories())
                {
                    var targets = region.Neighbours.Where(r => r.OwnedByPlayer(state.GetOpponentPlayerName))
                        .Where(r => r.Armies <= ((region.Armies)))
                        .OrderBy(r => r.Armies);

                    if(targets.Count() > 1 && region.Armies > 1)
                    {
                        var threat = targets.First().Armies + state.EnemyReinforcements() - 2;
                        threat = Math.Max(threat, 6);
                        if(targets.First().Armies > region.Armies - 1)
                            continue;
                        attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, targets.First(),
                            Math.Min((int)(threat / 0.9), region.Armies - 1)));
                        var additionalTargets = targets.Skip(1);
                        while(additionalTargets.Count() > 1)
                        {
                            threat = additionalTargets.First().Armies + state.EnemyReinforcements() - 2;
                            if((int)(threat) > additionalTargets.First().Armies)
                                attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region,
                                    additionalTargets.First(), Math.Min((int)(threat / 0.9), region.Armies - 1)));
                            additionalTargets = additionalTargets.Skip(1);
                        }
                    }
                    else
                    {
                        if(region.Armies > 1 && targets.Any() &&
                           (targets.First().Armies + (state.EnemyReinforcements() / 2) < region.Armies - 1))
                            attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, targets.First(),
                                region.Armies - 1));
                    }
                }
            }
            else
            {
                attackTransferMoves.AddRange(from region in state.BorderTerritories() 
                                             let target = region.Neighbours.Where(r => r.OwnedByPlayer(state.GetOpponentPlayerName)).OrderBy(n => n.Armies).First()
                                             where region.Armies > 1 && target.Armies + (state.EnemyReinforcements() / 2) < region.Armies 
                                             select new AttackTransferMove(state.MyPlayerName, region, target, region.Armies - 1));
            }

            foreach(var region in state.InternalRegions)
            {
                Region target;
                if(region.Neighbours.All(rn => rn.OwnedByPlayer(myName)))
                {
                    target = region.Neighbours.Except(state.InternalRegions)
                        .OrderBy(r => BotStarter.FavouriteTerritories.IndexOf(r.Name)).FirstOrDefault();
                }
                else
                {
                    target = region.Neighbours.Where(rn => !rn.OwnedByPlayer(myName))
                        .OrderBy(r => BotStarter.FavouriteTerritories.IndexOf(r.Name)).FirstOrDefault();
                }
                if(region.Armies > 1 && target != null)
                    attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, target, region.Armies - 1));
            }

            foreach(var region in state.ExpansionRegions.Values.Where(r => r.Armies > 3))
            {
                var availableArmies = region.Armies - 1;
                foreach(var target in region.Neighbours.Where(r => r.OwnedByPlayer(Region.gNeutralRegion))
                    .OrderBy(rg => rg.Armies).ThenBy(nr => BotStarter.FavouriteTerritories.IndexOf(nr.Name)))
                {
                    if(attackTransferMoves.Any(a => a.ToRegion.Equals(target)) 
                        || attackTransferMoves.Where(a => a.FromRegion.Equals(region)).Sum(a => a.Armies) >= region.Armies)
                    {
                        continue;
                    }
                    else if(target.Armies == 2 && availableArmies >= 4)
                    {
                        attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, target, 4));
                        availableArmies -= 4;
                    }
                    else if(target.Armies == 1 && availableArmies >= 3)
                    {
                        attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, target, 3));
                        availableArmies -= 3;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach(var region in state.MyRegions())
            {
                if(attackTransferMoves.All(a => a.FromRegion != region) &&
                    !state.BorderTerritories().Contains(region) && 
                    region.Armies > 1)
                {
                    var target = region.Neighbours.Where(r => r.OwnedByPlayer(myName)).OrderByDescending(a => a.Armies).FirstOrDefault();
                    if(target != null && attackTransferMoves.All(a => a.FromRegion != target && a.ToRegion != region))
                        attackTransferMoves.Add(new AttackTransferMove(state.MyPlayerName, region, target, region.Armies - 1));
                }
            }

            attackTransferMoves.Reverse();

            return attackTransferMoves;
        }

        public static void Main(String[] args)
        {
            var parser = new BotParser(new BotStarter());
            parser.Run();
        }

    }
}