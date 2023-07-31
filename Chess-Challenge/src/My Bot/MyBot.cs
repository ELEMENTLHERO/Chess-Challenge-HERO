#define DEBUG
#undef DEBUG

using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;


public class MyBot : IChessBot
{
    bool doOnce = true;
    int downUp = 0;
    bool isWhite = false; //we don't know

    public Move Think(Board board, Timer timer)
    {
        if (doOnce)
        {
            Square myKingSquare = board.GetKingSquare(board.IsWhiteToMove);
            downUp = myKingSquare.Rank < 5 ? 1 : -1; //1 is down -1 is up
            doOnce = false;
            isWhite = board.IsWhiteToMove;
        }
        Move[] moves = board.GetLegalMoves();
        
        return EvalMoves(moves,board,2).Key;
    }

    public KeyValuePair<Move,float> EvalMoves(Move[] moves, Board board, int depth)
    {
        if (depth == 0) return new KeyValuePair<Move, float>(Move.NullMove,0);

        Move bestMove = new Move();
        float bestValue = float.MinValue;
        //Dictionary<Move,int> rankedList = new Dictionary<Move, int>();

        foreach (var move in moves)
        {
            
            float value = 0;
            value = board.GetPiece(move.StartSquare).IsKing ? value - 1.5f : value;
            value = board.GetPiece(move.StartSquare).IsPawn ? value+(move.TargetSquare.Rank - move.StartSquare.Rank) * downUp *0.25f: value;
            value = value + (move.TargetSquare.Rank - move.StartSquare.Rank) * downUp * 0.1f;
            value = move.IsCapture ? getPieceValue(board.GetPiece(move.TargetSquare).PieceType) + value : value;
            //board.TrySkipTurn();
            //value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? - getPieceValue(move.MovePieceType) + value : value; //risk of being captured
            value = board.SquareIsAttackedByOpponent(move.StartSquare)&&!board.SquareIsAttackedByOpponent(move.TargetSquare) ? getPieceValue(move.MovePieceType) + value : value; //move only if you can get to saftey
#if DEBUG
            
            if (move.TargetSquare.File == 1)
            {

            }
            //Console.WriteLine("move: " + move + " my value: " + getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, false)) + " - " + "enemy value: " + getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, true)));

#endif
            //board.UndoSkipTurn();
            value = ((float)getNumberOfSeenSquares(move.TargetSquare, move.MovePieceType, board) - (float)getNumberOfSeenSquares(move.StartSquare, move.MovePieceType, board))/4 + value; //prio lots of vision
            value = move.IsCastles ? value + 1 : value;
            value = move.IsEnPassant ? value + 2 : value;
            value = move.IsPromotion ? value + 10 : value;
            value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, false)) - getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, true)) + value: value;
            board.MakeMove(move);
            //float test = getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, false)) - getLowestValueInList(GetPiecesAttackingSquare(board, move.TargetSquare, true));
            //if (test > 0)
            //{
            //    Console.WriteLine(board.GetFenString());
            //    Console.WriteLine(test + "\n" + move);
            //}
            value = board.IsInCheck()&&board.SquareIsAttackedByOpponent(move.TargetSquare) ? value - 10 : value;
            value = board.IsInCheck()? value + 5: value;
            value = board.IsInCheckmate() ? value + 1000 : value;
#if DEBUG

            if (value > 900)
            {
                Console.WriteLine("move:"+move+" checkmate?");
            }
#endif

            //Draw eval
            float diff = getBoardValueDiff(board, isWhite);
            value = board.IsDraw() ? value - diff : value;
#if DEBUG
            if(getBoardValueDiff(board, isWhite) < 0)
            {
                //Console.WriteLine("Want draw with: " + diff + " with move: " + move);
            }
#endif
            

            //board.ForceSkipTurn();
            //value += EvalMoves(board.GetLegalMoves(), board, depth - 1).Value;
            //board.UndoSkipTurn();
            //value = board.IsDraw()
            board.UndoMove(move);
            //GetLegalMoves(true)
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;

            }
        }

#if DEBUG
        Console.WriteLine("val: " + bestValue + " move: " + bestMove);
        Console.WriteLine("diff: "+getBoardValueDiff(board, isWhite));
#endif
        //BitboardHelper.GetKnightAttacks
        return new KeyValuePair<Move, float>(bestMove, bestValue);
    }

    public struct AttackDefendingBitboards
    {

        public Dictionary<PieceType, ulong> _defendingSquares;
        public Dictionary<PieceType, ulong> _attackedSquares;

        public AttackDefendingBitboards()
        {
            _defendingSquares = new Dictionary<PieceType, ulong>();
            _attackedSquares = new Dictionary<PieceType, ulong>();

            foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
            {
                _defendingSquares[pieceType] = 0;
                _attackedSquares[pieceType] = 0;

            }


            
        }
    }

    /// <summary>
    /// seems to work
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public AttackDefendingBitboards getProtectedSquares(Board board)
    {
        AttackDefendingBitboards attackDefendingBitboards = new AttackDefendingBitboards();

        foreach (PieceList pieces in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieces)
            {
                if (!(isWhite ^ piece.IsWhite)) //logical AND
                {
                    //attackDefendingBitboards.defendingSquares |= getAttackingSquares(piece.Square, piece.PieceType, board);
                    attackDefendingBitboards._defendingSquares[piece.PieceType] |= getAttackingSquares(piece.Square, piece.PieceType, board);
                }
                else if (isWhite ^ piece.IsWhite) //logical XOR (no need for else if)
                {
                    //attackDefendingBitboards.attackedSquares |= getAttackingSquares(piece.Square, piece.PieceType, board);
                    attackDefendingBitboards._attackedSquares[piece.PieceType] |= getAttackingSquares(piece.Square, piece.PieceType, board);

                }
            }
        }
        return attackDefendingBitboards;
    }
    //public List<Piece> squareAttackedBy(Board board, Square square)
    //{
    //    Dictionary<Square, List<Piece>> squaresInPlay = new Dictionary<Square, List<Piece>>();
    //    AttackDefendingBitboards attackDefendingBitboards = getProtectedSquares(board);
    //    foreach (PieceList pieces in board.GetAllPieceLists())
    //    {
    //        foreach (Piece piece in pieces)
    //        {

    //            if (!(isWhite ^ piece.IsWhite)) //logical AND
    //            {
    //                if(BitboardHelper.SquareIsSet(attackDefendingBitboards.defendingSquares, square))
    //                {
    //                    // add square and all pieces attacking that square squaresInPlay.Add(square,new List<Piece>());

    //                }
    //            }
    //            else if (isWhite ^ piece.IsWhite) //logical XOR (no need for else if)
    //            {
    //                squaresInPlay.Add(BitboardHelper.SquareIsSet(attackDefendingBitboards.attackedSquares, square));

    //            }
    //        }
    //    }

    //}

    public HashSet<PieceType> GetPiecesAttackingSquare(Board board, Square square, bool my)
    {
        HashSet<PieceType> attackingPieces = new HashSet<PieceType>();
        AttackDefendingBitboards attackDefendingBitboards = getProtectedSquares(board);
        if (my)
        {
            foreach (var attackBitboard in attackDefendingBitboards._defendingSquares)
            {
                if (BitboardHelper.SquareIsSet(attackBitboard.Value, square))
                {
                    attackingPieces.Add(attackBitboard.Key);
                }

            }
        }
        else
        {
            foreach (var attackBitboard in attackDefendingBitboards._attackedSquares)
            {
                if (BitboardHelper.SquareIsSet(attackBitboard.Value, square))
                {
                    attackingPieces.Add(attackBitboard.Key);
                }

            }
        }

#if DEBUG
        //BitboardHelper.VisualizeBitboard(attackDefendingBitboards._attackedSquares[PieceType.Bishop]);
#endif

        return attackingPieces;
    }

    public float getLowestValueInList(HashSet<PieceType> pieces)
    {
        float currLowest = float.MaxValue;
        foreach (var piece in pieces)
        {
            float value = getPieceValue(piece);
            if (value < currLowest)
            {
                currLowest = value;
            }
        }
        if (currLowest == float.MaxValue)
        {
            return 0;
        }
        return currLowest;
    }

    public float getBoardValueDiff(Board board, bool isWhite)
    {
        float valueDiff = 0;
        foreach (var pieceList in board.GetAllPieceLists())
        {
            foreach(var piece in pieceList) 
            {
                if (!(isWhite ^ piece.IsWhite)) //logical AND
                {
                    valueDiff += getPieceValue(piece.PieceType);
                }
                else if (isWhite ^ piece.IsWhite) //logical XOR (no need for else if)
                {
                    valueDiff -= getPieceValue(piece.PieceType);
                }
            }
        }
        return valueDiff;
    }

    public float getPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.None:
                return 0;
            case PieceType.Pawn:
                return 1;
            case PieceType.Knight:
                return 3;
            case PieceType.Bishop:
                return 3;
            case PieceType.Rook:
                return 5;
            case PieceType.Queen:
                return 9;
            case PieceType.King:
                return 0;
            default: return 0;
        }
    }

    public int getNumberOfSeenSquares(Square square, PieceType piece, Board board)
    {
        return BitboardHelper.GetNumberOfSetBits(getAttackingSquares(square, piece, board));
    }

    public ulong getAttackingSquares(Square square, PieceType piece,Board board)
    {
        switch (piece)
        {
            case PieceType.None:
                return 0;
            case PieceType.Pawn:
                return BitboardHelper.GetPawnAttacks(square, board.GetPiece(square).IsWhite);
            case PieceType.Knight:
                return BitboardHelper.GetKnightAttacks(square);
            case PieceType.Bishop:
                return BitboardHelper.GetSliderAttacks(PieceType.Bishop, square, board);
            case PieceType.Rook:
                return BitboardHelper.GetSliderAttacks(PieceType.Rook, square, board);
            case PieceType.Queen:
                return BitboardHelper.GetSliderAttacks(PieceType.Queen, square,board);
            case PieceType.King:
                return BitboardHelper.GetKingAttacks(square);
            default: return 0;
        }
        
    }
}