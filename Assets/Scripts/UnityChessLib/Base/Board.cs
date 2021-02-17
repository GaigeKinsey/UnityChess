using System;
using UnityEngine;

namespace UnityChess
{
    /// <summary>An 8x8 column-major matrix representation of a chessboard.</summary>
    public class Board
    {
        public King WhiteKing { get; private set; }
        public King BlackKing { get; private set; }
        private readonly Piece[,] boardMatrix;

        public Piece this[Square position]
        {
            get
            {
                if (position.IsValid) return boardMatrix[position.File - 1, position.Rank - 1];
                throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
            }

            set
            {
                if (position.IsValid) boardMatrix[position.File - 1, position.Rank - 1] = value;
                else throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
            }
        }

        public Piece this[int file, int rank]
        {
            get => this[new Square(file, rank)];
            set => this[new Square(file, rank)] = value;
        }

        /// <summary>Creates a Board with initial chess game position.</summary>
        public Board(bool is960 = false)
        {
            boardMatrix = new Piece[8, 8];
            SetStartingPosition(is960);
        }

        /// <summary>Creates a deep copy of the passed Board.</summary>
        public Board(Board board)
        {
            // TODO optimize this method
            // Creates deep copy (makes copy of each piece and deep copy of their respective ValidMoves lists) of board (list of BasePiece's)
            // this may be a memory hog since each Board has a list of Piece's, and each piece has a list of Movement's
            // avg number turns/Board's per game should be around ~80. usual max number of pieces per board is 32
            boardMatrix = new Piece[8, 8];
            for (int file = 1; file <= 8; file++)
                for (int rank = 1; rank <= 8; rank++)
                {
                    Piece pieceToCopy = board[file, rank];
                    if (pieceToCopy == null) continue;

                    this[file, rank] = pieceToCopy.DeepCopy();
                }

            InitKings();
        }

        public void ClearBoard()
        {
            for (int file = 1; file <= 8; file++)
                for (int rank = 1; rank <= 8; rank++)
                    this[file, rank] = null;

            WhiteKing = null;
            BlackKing = null;
        }

        /// <summary>
        /// Creates a random array of characters representing the order of the back line, based on the rules of chess960 defined on the wiki
        /// </summary>
        /// <returns>A char array representing the order of the back line</returns>
        public char[] Generate960Arr()
        {
            char[] chess960Arr = new char[8];
            bool[] takenSquares = new bool[8];

            // Place light bishop
            int lightBishopSpot = 2 * UnityEngine.Random.Range(0, 4) + 1;
            chess960Arr[lightBishopSpot] = 'b';
            takenSquares[lightBishopSpot] = true;

            // Place dark bishop
            int darkBishopSpot = 2 * UnityEngine.Random.Range(0, 4);
            chess960Arr[darkBishopSpot] = 'b';
            takenSquares[darkBishopSpot] = true;

            // Place the queen
            int[] queenPlaces = new int[6];
            int queenIter = 0;
            for (int i = 0; i < takenSquares.Length; i++)
            {
                if (!takenSquares[i])
                {
                    queenPlaces[queenIter++] = i;
                }
            }

            int queenSpot = UnityEngine.Random.Range(0, queenPlaces.Length);
            chess960Arr[queenPlaces[queenSpot]] = 'q';
            takenSquares[queenPlaces[queenSpot]] = true;

            // Place the first knight
            int[] knight1Places = new int[5];
            int knight1Iter = 0;
            for (int i = 0; i < takenSquares.Length; i++)
            {
                if (!takenSquares[i])
                {
                    knight1Places[knight1Iter++] = i;
                }
            }

            int knight1Spot = UnityEngine.Random.Range(0, knight1Places.Length);
            chess960Arr[knight1Places[knight1Spot]] = 'k';
            takenSquares[knight1Places[knight1Spot]] = true;

            // Place the second knight
            int[] knight2Places = new int[4];
            int knight2Iter = 0;
            for (int i = 0; i < takenSquares.Length; i++)
            {
                if (!takenSquares[i])
                {
                    knight2Places[knight2Iter++] = i;
                }
            }

            int knight2Spot = UnityEngine.Random.Range(0, knight2Places.Length);
            chess960Arr[knight2Places[knight2Spot]] = 'k';
            takenSquares[knight2Places[knight2Spot]] = true;

            // Place the first rook
            for (int i = 0; i < takenSquares.Length; i++)
            {
                if (!takenSquares[i])
                {
                    chess960Arr[i] = 'r';
                    takenSquares[i] = true;
                    break;
                }
            }

            // Place the second rook
            for (int i = takenSquares.Length-1; i >= 0; i--)
            {
                if (!takenSquares[i])
                {
                    chess960Arr[i] = 'r';
                    takenSquares[i] = true;
                    break;
                }
            }

            // Place the king
            for (int i = 0; i < takenSquares.Length; i++)
            {
                if (!takenSquares[i])
                {
                    chess960Arr[i] = 'K';
                    takenSquares[i] = true;
                    break;
                }
            }

            return chess960Arr;
        }

        /// <summary>
        /// Resets the board and resets the position of all the pieces, will do a fischer random setup when passed 'true'
        /// </summary>
        /// <param name="is960"></param>
        public void SetStartingPosition(bool is960 = false)
        {
            ClearBoard();

            //Row 2/Rank 7 and Row 7/Rank 2, both rows of pawns
            for (int file = 1; file <= 8; file++)
            {
                foreach (int rank in new[] { 2, 7 })
                {
                    Square position = new Square(file, rank);
                    Side pawnColor = rank == 2 ? Side.White : Side.Black;
                    this[position] = new Pawn(position, pawnColor);
                }
            }

            if (is960)
            {
                char[] randomOrder = Generate960Arr();

                //Rows 1 & 8/Ranks 8 & 1, back rows for both players
                for (int file = 1; file <= 8; file++)
                {
                    foreach (int rank in new[] { 1, 8 })
                    {
                        Square position = new Square(file, rank);
                        Side pieceColor = rank == 1 ? Side.White : Side.Black;

						switch (randomOrder[file - 1])
						{
                            case 'r':
                                this[position] = new Rook(position, pieceColor);
                                break;
                            case 'b':
                                this[position] = new Bishop(position, pieceColor);
                                break;
                            case 'k':
                                this[position] = new Knight(position, pieceColor);
                                break;
                            case 'q':
                                this[position] = new Queen(position, pieceColor);
                                break;
                            case 'K':
                                this[position] = new King(position, pieceColor);
                                if (pieceColor is Side.White) WhiteKing = (King)this[position];
                                else BlackKing = (King)this[position];
                                break;
                        }
					}
                }
            }
            else
            {
                //Rows 1 & 8/Ranks 8 & 1, back rows for both players
                for (int file = 1; file <= 8; file++)
                {
                    foreach (int rank in new[] { 1, 8 })
                    {
                        Square position = new Square(file, rank);
                        Side pieceColor = rank == 1 ? Side.White : Side.Black;
                        switch (file)
                        {
                            case 1:
                            case 8:
                                this[position] = new Rook(position, pieceColor);
                                break;
                            case 2:
                            case 7:
                                this[position] = new Knight(position, pieceColor);
                                break;
                            case 3:
                            case 6:
                                this[position] = new Bishop(position, pieceColor);
                                break;
                            case 4:
                                this[position] = new Queen(position, pieceColor);
                                break;
                            case 5:
                                this[position] = new King(position, pieceColor);
                                break;
                        }
                    }
                }

                WhiteKing = (King)this[5, 1];
                BlackKing = (King)this[5, 8];
            }
        }

        public void MovePiece(Movement move)
        {
            if (!(this[move.Start] is Piece pieceToMove)) throw new ArgumentException($"No piece was found at the given position: {move.Start}");

            this[move.Start] = null;
            this[move.End] = pieceToMove;

            pieceToMove.HasMoved = true;
            pieceToMove.Position = move.End;

            (move as SpecialMove)?.HandleAssociatedPiece(this);
        }

        internal bool IsOccupied(Square position) => this[position] != null;

        internal bool IsOccupiedBySide(Square position, Side side) => this[position] is Piece piece && piece.Color == side;

        public void InitKings()
        {
            for (int file = 1; file <= 8; file++)
            {
                for (int rank = 1; rank <= 8; rank++)
                {
                    if (this[file, rank] is King king)
                    {
                        if (king.Color == Side.White) WhiteKing = king;
                        else BlackKing = king;
                    }
                }
            }
        }
    }
}