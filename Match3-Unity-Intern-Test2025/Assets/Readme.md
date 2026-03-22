# Match-3 Refactor: Hotbar-Based Gameplay

##How to run
1. Open project in Unity
2. Load game Scene
3. Run the game
4. Choose a mode from Home Screen

## 1. Project Overview
This project refactors an existing match-3 style game into a new gameplay system where players select items from the board and place them into a limited bottom area (hotbar).

The core mechanic shifts from tile swapping to selection and accumulation, introducing a constraint-based matching system.

## 2. Original Game Analysis

The base project follows a traditional match-3 structure:
* The board is represented as a grid of items (fish types)
* Player interaction is based on swapping adjacent tiles
* Matching occurs when 3+ identical items align
* Matches are resolved directly on the board


## 3. New Gameplay Mechanics

### Core Rules
1. Player taps an item on the board → item moves to bottom cells
2. Items placed in bottom cells cannot return to the board (Normal Mode)
3. If exactly 3 identical items exist in the bottom cells → they are cleared
4. Player wins when the board is completely cleared
5. Player loses if all 5 bottom cells are filled without forming a match

## 4. System Constraints
* Total count of each item type is divisible by 3
* Bottom area contains exactly 5 cells
* Board always includes all fish types

## 5. Game Modes

### 5.1 Normal Mode
* Standard rules apply
* No interaction with items once placed in bottom cells

### 5.2 Time Attack Mode
* Player has 60 seconds to clear the board
* Bottom cells do NOT cause a loss condition
* Player can tap items in bottom cells to return them to original board positions

## 6. UI Features

### Home Screen
* Play Button
* Autoplay (Win) Button
* Auto Lose Button
* Time Attack Mode Button

### End States
* Win Screen (simple UI)
* Lose Screen (simple UI)

## 7. Design Approach

### 7.1 Core Refactor Strategy
Instead of extending swap mechanics, the interaction system was redesigned:
* Disabled tile swapping
* Introduced a **selection-based pipeline**
Flow:
Board Item Click → Move to Bottom Slot → Check Match → Clear if needed

### 7.2 Key Systems

#### Bottom Cell System (Hotbar)
* Fixed capacity: 5 slots
* Stores selected items sequentially
* Responsible for triggering match checks

#### Match Detection
* Triggered after each insertion
* Counts occurrences of item types
* Clears only when count == 3

#### Board Management
* Removes items immediately upon selection
* Tracks remaining items for win condition

## 8. Autoplay Systems
### Autoplay (Win)
* Selects valid items leading toward board completion
* Prioritizes forming matches efficiently

### Auto Lose
* Intentionally fills bottom cells without completing matches

## 9. Animations
### Item Movement
* Tween from board position → bottom cell

### Match Clearing
* Scale animation (1 → 0)
* Item destroyed after animation completes




