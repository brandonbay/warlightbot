using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace move
{

    public class Move
    {
        public enum gMoveTypes
        {
            Place,
            AttackTransfer,
            InvalidMoveType,
        }

        private String playerName; // Name of the player that did this move
        private String illegalMove = ""; // Gets the value of the error message if move is illegal, else remains empty

        public String PlayerName
        {
            set { playerName = value; }
            get { return playerName; }
        }

        public String IllegalMove
        {
            set { illegalMove = value; }
            get { return illegalMove; }
        }

        public virtual gMoveTypes MoveType
        {
            get { return gMoveTypes.InvalidMoveType; }
        }

        public static gMoveTypes GetMoveType (string moveInput)
        {
            switch(moveInput)
            {
                case "place_armies":
                    return gMoveTypes.Place;
                case "attack/transfer" :
                    return gMoveTypes.AttackTransfer;
                default :
                    return gMoveTypes.InvalidMoveType;
            }
        }
    }
}