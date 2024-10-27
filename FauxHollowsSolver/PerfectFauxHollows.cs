﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FauxHollowsSolver;

public sealed class PerfectFauxHollows {
    public const int TileRowLen = 6;
    public const int TileColLen = 6;
    public const int TotalTiles = TileRowLen * TileColLen;

    public const int MaxAttempts = 11;

    public const int ConfirmedSword = 50;
    public const int ConfirmedBox = 60;
    public const int ConfirmedChest = 70;

    public static int[] Solve(Tile[] state) {
        var recommendations = new int[TotalTiles];

        if (state.Contains(Tile.Unknown))
            return recommendations;

        var blockedCount = state.Count(tile => tile == Tile.Blocked);
        var hiddenCount = state.Count(tile => tile == Tile.Hidden);
        var attemptsRemaining = MaxAttempts - (TotalTiles - blockedCount - hiddenCount);

        if (attemptsRemaining == 0)
            return recommendations;

        var foundSwords = false;
        var foundBoxChest = false;
        var foundCommander = false;

        int[] knownSwords = null;
        int[] knownBoxChest = null;
        int[] knownCommander = null;

        var possibleSwords = new List<int[]>();
        var possibleBoxChests = new List<int[]>();
        var possibleCommanders = new List<int[]>();

        for (var i = 0; i < TotalTiles; i++) {
            var maskedTile = state[i];
            var tile = maskedTile.Unmask();

            var x = i % TileRowLen;
            var y = i / TileRowLen;

            // Known values, where a tile is not blocked or empty
            if (!foundSwords && (foundSwords = Tile.Swords.HasFlag(tile))) {
                possibleSwords.Clear();

                var upperLeftIndex = maskedTile switch {
                    Tile.SwordsUpperLeft => i,
                    Tile.SwordsUpperRight => i - 1,
                    Tile.SwordsMiddleLeft => i - TileRowLen,
                    Tile.SwordsMiddleRight => i - 1 - TileRowLen,
                    Tile.SwordsLowerLeft => i - (TileRowLen * 2),
                    Tile.SwordsLowerRight => i - 1 - (TileRowLen * 2),

                    Tile.SwordsUpperLeft | Tile.RotatedLeft => i - TileRowLen,
                    Tile.SwordsUpperRight | Tile.RotatedLeft => i,
                    Tile.SwordsMiddleLeft | Tile.RotatedLeft => i - 1 - TileRowLen,
                    Tile.SwordsMiddleRight | Tile.RotatedLeft => i - 1,
                    Tile.SwordsLowerLeft | Tile.RotatedLeft => i - 2 - TileRowLen,
                    Tile.SwordsLowerRight | Tile.RotatedLeft => i - 2,

                    Tile.SwordsUpperLeft | Tile.RotatedRight => i - 2,
                    Tile.SwordsUpperRight | Tile.RotatedRight => i - 2 - TileRowLen,
                    Tile.SwordsMiddleLeft | Tile.RotatedRight => i - 1,
                    Tile.SwordsMiddleRight | Tile.RotatedRight => i - 1 - TileRowLen,
                    Tile.SwordsLowerLeft | Tile.RotatedRight => i,
                    Tile.SwordsLowerRight | Tile.RotatedRight => i - TileRowLen,
                    _ => throw new Exception("Woops"),
                };

                if (maskedTile.IsRotatedLeft() || maskedTile.IsRotatedRight())
                    knownSwords = GetRectIndices(upperLeftIndex, 3, 2);
                else
                    knownSwords = GetRectIndices(upperLeftIndex, 2, 3);

                var hiddenIndices = knownSwords.Where(t => state[t] == Tile.Hidden).ToArray();
                if (hiddenIndices.Length <= attemptsRemaining)
                    foreach (var index in hiddenIndices)
                        recommendations[index] = ConfirmedSword;
            }
            else if (!foundBoxChest && (foundBoxChest = Tile.BoxChest.HasFlag(tile))) {
                possibleBoxChests.Clear();

                var upperLeftIndex = tile switch {
                    Tile.BoxUpperLeft => i,
                    Tile.BoxUpperRight => i - 1,
                    Tile.BoxLowerLeft => i - TileRowLen,
                    Tile.BoxLowerRight => i - 1 - TileRowLen,

                    Tile.BoxUpperLeft | Tile.RotatedLeft => i - TileRowLen,
                    Tile.BoxUpperRight | Tile.RotatedLeft => i,
                    Tile.BoxLowerLeft | Tile.RotatedLeft => i - 1 - TileRowLen,
                    Tile.BoxLowerRight | Tile.RotatedLeft => i - 1,

                    Tile.BoxUpperLeft | Tile.RotatedRight => i - 1,
                    Tile.BoxUpperRight | Tile.RotatedRight => i - 1 - TileRowLen,
                    Tile.BoxLowerLeft | Tile.RotatedRight => i,
                    Tile.BoxLowerRight | Tile.RotatedRight => i - TileRowLen,

                    Tile.ChestUpperLeft => i,
                    Tile.ChestUpperRight => i - 1,
                    Tile.ChestLowerLeft => i - TileRowLen,
                    Tile.ChestLowerRight => i - 1 - TileRowLen,

                    Tile.ChestUpperLeft | Tile.RotatedLeft => i - TileRowLen,
                    Tile.ChestUpperRight | Tile.RotatedLeft => i,
                    Tile.ChestLowerLeft | Tile.RotatedLeft => i - 1 - TileRowLen,
                    Tile.ChestLowerRight | Tile.RotatedLeft => i - 1,

                    Tile.ChestUpperLeft | Tile.RotatedRight => i - 1,
                    Tile.ChestUpperRight | Tile.RotatedRight => i - 1 - TileRowLen,
                    Tile.ChestLowerLeft | Tile.RotatedRight => i,
                    Tile.ChestLowerRight | Tile.RotatedRight => i - TileRowLen,
                    _ => throw new Exception("Woops"),
                };

                var recommendedValue = Tile.Box.HasFlag(tile) ? ConfirmedBox : ConfirmedChest;

                knownBoxChest = GetRectIndices(upperLeftIndex, 2, 2);
                var hiddenIndices = knownBoxChest.Where(t => state[t] == Tile.Hidden).ToArray();
                if (hiddenIndices.Length <= attemptsRemaining)
                    foreach (var index in hiddenIndices)
                        recommendations[index] = recommendedValue;
            }
            else if (!foundCommander && (foundCommander = Tile.Commander.HasFlag(tile))) {
                possibleCommanders.Clear();

                knownCommander = [i];

                recommendations[i] = 0;
            }
            else {
                // Gather potential locations each reward could be
                if (!foundSwords && attemptsRemaining >= 6) {
                    if (x < TileRowLen - 2 && y < TileColLen - 1) {
                        var rect = GetRectIndices(i, 3, 2);
                        if (rect.All(idx => state[idx] == Tile.Hidden))
                            possibleSwords.Add(rect);
                    }
                    if (x < TileRowLen - 1 && y < TileColLen - 2) {
                        var rect = GetRectIndices(i, 2, 3);
                        if (rect.All(ri => state[ri] == Tile.Hidden))
                            possibleSwords.Add(rect);
                    }
                }

                if (!foundBoxChest && attemptsRemaining >= 4) {
                    if (x < TileRowLen - 1 && y < TileColLen - 1) {
                        var rect = GetRectIndices(i, 2, 2);
                        if (rect.All(ri => state[ri] == Tile.Hidden))
                            possibleBoxChests.Add(rect);
                    }
                }

                if (!foundCommander && attemptsRemaining >= 1) {
                    possibleCommanders.Add([i]);
                }
            }
        }

        // Sum up the count of possibles where a known is not already present
        foreach (var rect in possibleSwords.Concat(possibleBoxChests).Concat(possibleCommanders)) {
            if (foundSwords && knownSwords.Intersect(rect).Any() ||
                foundBoxChest && knownBoxChest.Intersect(rect).Any() ||
                foundCommander && knownCommander.Intersect(rect).Any())
                continue;

            foreach (var i in rect)
                // Values over TileCount are reserved for known but hidden tiles
                if (recommendations[i] < TotalTiles)
                    recommendations[i]++;
        }

        return recommendations;
    }

    private static int[] GetRectIndices(int upperLeftCornerIndex, int xMax, int yMax) {
        var i = 0;
        var indices = new int[xMax * yMax];
        for (var y = 0; y < yMax; y++) {
            for (var x = 0; x < xMax; x++) {
                indices[i] = upperLeftCornerIndex + x + (y * TileRowLen);
                i++;
            }
        }
        return indices;
    }
}