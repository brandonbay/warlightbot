using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using main;
using move;

namespace bot
{

    public class BotParser
    {

        readonly Bot bot;

        BotState currentState;

        public BotParser(Bot bot)
        {
            this.bot = bot;
            this.currentState = new BotState();
        }

        public void Run()
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null)
                    break;
                line = line.Trim();

                if (line.Length == 0)
                    continue;

                var parts = line.Split(' ');
                var output = new StringBuilder();

                switch(parts[0])
                {
                    case "pick_starting_regions" :
                        // Pick which regions you want to start with
                        currentState.SetPickableStartingRegions(parts);
                        var preferredStartingRegions = bot.GetPreferredStartingRegions(currentState, long.Parse(parts[1]));
                        foreach(var region in preferredStartingRegions)
                            output.Append(region.Id + " ");
                        Console.WriteLine(output);
                        break;
                    case "go" :
                        if(parts.Length != 3)
                            break;
                        // We need to do a move
                        currentState.UpdateRegions();
                        currentState.UpdateAggression();
                        switch(Move.GetMoveType(parts[1]))
                        {
                            case Move.gMoveTypes.Place :
                                // Place armies
                                var placeArmiesMoves = bot.GetPlaceArmiesMoves(currentState, long.Parse(parts[2]));
                                foreach(var move in placeArmiesMoves)
                                    output.Append(move.String + ",");
                                break;
                            case Move.gMoveTypes.AttackTransfer :
                                // attack/transfer
                                var attackTransferMoves = bot.GetAttackTransferMoves(currentState, long.Parse(parts[2]));
                                foreach(var move in attackTransferMoves)
                                    output.Append(move.String + ",");
                                break;
                        }
                        if(output.Length > 0)
                        {
                            Console.WriteLine(output);
                            break;
                        }
                        Console.WriteLine("No moves");
                        break;
                    case "settings":
                        if(parts.Length != 3)
                            break;
                        // Update settings
                        currentState.UpdateSettings(parts[1], parts[2]);
                        break;
                    case "setup_map" :
                        // Initial full map is given
                        currentState.SetupMap(parts);
                        break;
                    case "update_map" :
                        // All visible regions are given
                        currentState.UpdateMap(parts);
                        break;
                    case "opponent_moves" :
                        // All visible opponent moves are given
                        currentState.ReadOpponentMoves(parts);
                        break;
                    default :
                        Console.Error.WriteLine("Unable to parse line \"" + line + "\"");
                        break;
                }
            }
        }

    }

}