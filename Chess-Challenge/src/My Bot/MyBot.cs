#define DEBUG

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
        float bestValue = -100;
        //Dictionary<Move,int> rankedList = new Dictionary<Move, int>();

        foreach (var move in moves)
        {
            
            float value = 0;
            value = board.GetPiece(move.StartSquare).IsKing ? value - 1.5f : value;
            value = board.GetPiece(move.StartSquare).IsPawn ? value+(move.TargetSquare.Rank - move.StartSquare.Rank) * downUp *0.25f: value;
            if (move.IsCapture)
            {
                value = getPieceValue(board.GetPiece(move.TargetSquare).PieceType) + value;
            }
            //board.TrySkipTurn();
            value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? - getPieceValue(move.MovePieceType) + value : value; //risk of being captured
            value = board.SquareIsAttackedByOpponent(move.StartSquare)&&!board.SquareIsAttackedByOpponent(move.TargetSquare) ? getPieceValue(move.MovePieceType) + value : value; //move only if you can get to saftey
            //board.UndoSkipTurn();
            value = ((float)getNumberOfSeenSquares(move.TargetSquare, move.MovePieceType, board) - (float)getNumberOfSeenSquares(move.StartSquare, move.MovePieceType, board))/2 + value; //prio lots of vision
            value = move.IsCastles ? value + 2 : value;
            value = move.IsEnPassant ? value + 5 : value;
            value = move.IsPromotion ? value + 10 : value;
            value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? value - 25 - getPieceValue(move.MovePieceType) : value;
            board.MakeMove(move);
            value = board.IsInCheck() ? value + 25 : value;
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
    //TODO make list of squares which are protected
    public bool attacking(Board board, Piece piece)
    {
        //ulong protectedSquares
        //getAttackingSquares

        return false;
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
                else if (isWhite ^ piece.IsWhite) //logical XOR
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
                return 1000;
            default: return 0;
        }

        // return Convert.ToInt32(Math.Pow(Convert.ToInt32(piece + 1), 2));
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
                return BitboardHelper.GetSliderAttacks(PieceType.Bishop, square, board); //todo remove friendly?
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