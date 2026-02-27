# UltimateTrackHorse

## Tracks

### Main road -> straight
<img width="268" height="270" alt="image" src="https://github.com/user-attachments/assets/250c70cc-3df8-49d3-afe3-99d34f4ca522" />


### Left/Right turns
<img width="269" height="270" alt="image" src="https://github.com/user-attachments/assets/f59bb0b5-895a-44fe-835e-6a7f9468364b" />

### O road 
<img width="272" height="281" alt="image" src="https://github.com/user-attachments/assets/d9bc7849-2f92-4f6a-a028-b76d87bcf999" />

### Serpentines

<img width="272" height="271" alt="image" src="https://github.com/user-attachments/assets/fbf60d3e-0430-4ec6-b27a-ca74ded4aa1f" />

### Bezier curvers

- (Procedurally generate? - mayheps)
- some variations

<img width="269" height="271" alt="image" src="https://github.com/user-attachments/assets/aaaa9616-7e55-4bac-93a1-a1a6227e4b21" />

### Wide-ass road

<img width="274" height="271" alt="image" src="https://github.com/user-attachments/assets/c7311bd9-0730-4c68-aea7-988b2ddfaf1c" />

## Obsticles

### Barrier/Wall

- different sizes and shapes
- eg.: Boulder, Fallen Tree, Car Wreck, etc. 

### Blurry obsticles

- Blurs visions
- eg.: HayBale, Paint Cans, Smoke Wall

### Surface changes

#### Ice 
- Car loses traction for a brief moment

#### Glass 
- Permanently damages one of the tires making the car shift to one direction for the rest of the race
> If we have time

#### Honey 
- Slows down the car

#### Oil smudge 
- spins car on a spot (random rotation matrix) 

## Map Generation & Game Logic (Technical Specs)

This project utilizes a custom procedural generation system to create dynamic, playable tracks. Responsibility for this module includes the generation algorithm, tile connectivity, and core game rules.

### 1. Procedural Generation (Wave Function Collapse)
The map is generated on a grid based on user-defined dimensions.
* **The Algorithm:** We use the **Wave Function Collapse (WFC)** algorithm. Every grid cell starts in a state of superposition (all possible tiles) and is collapsed based on local constraints to ensure a logical layout.
* **Boundary Constraints:** The **Start** and **Finish** points are fixed at the edges of the grid before generation begins. The algorithm ensures a guaranteed continuous path between these two points.
* **Connectivity (Sockets):** Tiles are connected using a socket-based system. A connection is only valid if the sockets on adjacent sides match (e.g., `Road_Mid` must connect to `Road_Mid`).

### 2. Technical Requirements for Assets
To ensure the WFC algorithm functions correctly, all environment prefabs must adhere to these standards:
* **Pivot Point:** Center of the tile ($0,0,0$).
* **Dimensions:** Tiles must be perfectly square and uniform in size (e.g., $10 \times 10$ units).
* **Road Alignment:** The road/track must always enter and exit within the **middle third** of any given side to ensure seamless alignment regardless of tile type.
* **Obstacle Anchors:** Every road prefab must contain empty GameObjects acting as transforms labeled `ObstacleSlot`. These are used by the logic module to spawn interactive elements.

### 3. Obstacle & Hazard Logic
Obstacles are placed dynamically after the track layout is finalized by completing the track:
1.  **Placement:** The system identifies valid `ObstacleSlot` locations on the generated tiles.
2.  **Selection:** Based on user input or random weighting, hazards (Walls, Oil Smudges, Ice, etc.) are instantiated.
3.  **Collision Layers:** Each obstacle is assigned to a specific layer to interact correctly with the physics-driven car.

### 4. Game Logic & Surface Systems
The logic module also handles the bridge between the environment and the car physics:
* **Surface Interface:** We use a unified interface for surface changes. When a car triggers a hazard, the hazard sends data (friction modifiers, rotation matrices for oil, etc.) directly to the Car Controller.
* **Win/Loss Conditions:** Detection of the player reaching the finish line, lap timing, and checkpoint validation.
