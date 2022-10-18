Versioning
 

Version 3.20

 

Run only on .Net 6.0.

The main menu has been restructured

The status bar now displays the number of threads running when the best move is being searched.

Restructure the code to separate:

The core chess routines
the search engine
the PGN parsing
the FICS interface (chess server)
the user interface
Fixed the crash which occurs when a game has more than 256 moves (512 ply).

The chess 50 move rule now correctly checks for 50 moves (was 50 ply).

Iterative deepening has been disabled. The way the multi-threading is implemented makes the iterative deepening longer than when you’re not using it. I will try to find a solution to that.

The translation table now works properly and improves the performance of the search.

Code clean-up has been done.
