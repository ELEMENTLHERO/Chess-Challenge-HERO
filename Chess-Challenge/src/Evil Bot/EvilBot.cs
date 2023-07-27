using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();

            // Pick a random move to play if nothing better is found
            Random rng = new();
            Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
            int highestValueCapture = 0;

            foreach (Move move in allMoves)
            {
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    moveToPlay = move;
                    break;
                }

                // Find highest value capture
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                if (capturedPieceValue > highestValueCapture)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }

            return moveToPlay;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
    }
}

public class MyEvilBot : IChessBot
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

        return EvalMoves(moves, board, 2).Key;
    }

    public KeyValuePair<Move, int> EvalMoves(Move[] moves, Board board, int depth)
    {
        if (depth == 0) return new KeyValuePair<Move, int>(Move.NullMove, 0);

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
            value = board.SquareIsAttackedByOpponent(move.TargetSquare) ? -getPieceValue(move.MovePieceType) + value : value;
            value = board.SquareIsAttackedByOpponent(move.StartSquare) && !board.SquareIsAttackedByOpponent(move.TargetSquare) ? getPieceValue(move.MovePieceType) + value : value;
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