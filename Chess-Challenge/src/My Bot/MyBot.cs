using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    bool doOnce = true;
    int downUp = 0;

    

    public Move Think(Board board, Timer timer)
    {
        if (doOnce)
        {
            Square myKingSquare = board.GetKingSquare(board.IsWhiteToMove);
            downUp = myKingSquare.Rank < 5 ? 1 : -1; //1 is down -1 is up
            doOnce = false;
        }
        Move[] moves = board.GetLegalMoves();
        
        return EvalMoves(moves,board,2).Key;
    }

    public KeyValuePair<Move,int> EvalMoves(Move[] moves, Board board, int depth)
    {
        if (depth == 0) return new KeyValuePair<Move, int>(Move.NullMove,0);

        Move bestMove = new Move();
        int bestValue = -100;
        //Dictionary<Move,int> rankedList = new Dictionary<Move, int>();

        foreach (var move in moves)
        {
            
            int value = 0;
            value = board.GetPiece(move.StartSquare).IsKing ? value - 1 : value;
            value = (move.TargetSquare.Rank - move.StartSquare.Rank) * downUp;
            if (move.IsCapture)
            {
                value = getPieceValue(board.GetPiece(move.TargetSquare).PieceType) + value;
            }
            //board.TrySkipTurn();
            value = board.SquareIsAttackedByOpponent(move.StartSquare) ? getPieceValue(move.MovePieceType) + value : value;
            //board.UndoSkipTurn();
            value = move.IsCastles ? value + 2 : value;
            value = move.IsEnPassant ? value + 5 : value;
            value = move.IsPromotion ? value + 15 : value;
            value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? value - 25 - getPieceValue(move.MovePieceType) : value;
            board.MakeMove(move);
            value = board.IsInCheck() ? value + 25 : value;
            value = board.IsInCheckmate() ? value + 1000 : value;
            
            //value += EvalMoves(board.GetLegalMoves(), board, depth - 1).Value;
            //value = board.IsDraw()
            board.UndoMove(move);
            //GetLegalMoves(true)
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;

            }
        }
        Console.WriteLine("val: " + bestValue + " move: " + bestMove);

        return new KeyValuePair<Move, int>(bestMove, bestValue);
    }

    public int getPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.None:
                return 0;
            case PieceType.Pawn:
                return 2;
            case PieceType.Knight:
                return 6;
            case PieceType.Bishop:
                return 6;
            case PieceType.Rook:
                return 10;
            case PieceType.Queen:
                return 18;
            case PieceType.King:
                return 1000;
            default: return 0;
        }

        // return Convert.ToInt32(Math.Pow(Convert.ToInt32(piece + 1), 2));
    }
}