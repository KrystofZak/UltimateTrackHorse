# AK Road Editor

## Complete Documentation

**Version 1.02**  
**Mauro Valvano**

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Installation](#2-installation)
3. [Quick Start Guide](#3-quick-start-guide)
4. [MapEditor Component](#4-mapeditor-component)
   1. [Properties](#41-properties)
   2. [Edit Modes](#42-edit-modes)
   3. [Working with Control Points](#43-working-with-control-points)
5. [RoadGenerator Component](#5-roadgenerator-component)
   1. [Road Settings](#51-road-settings)
   2. [Terrain Settings](#52-terrain-settings)
   3. [Texture Settings](#53-texture-settings)
6. [RailingEditor Component](#6-railingeditor-component)
   1. [Railing Settings](#61-railing-settings)
   2. [Editing Railings](#62-editing-railings)
7. [TrafficLineGenerator Component](#7-trafficlinegenerator-component)
8. [Step-by-Step Tutorials](#8-step-by-step-tutorials)
9. [Tips and Best Practices](#11-tips-and-best-practices)
10. [Troubleshooting](#12-troubleshooting)
11. [Support and Contact](#13-support-and-contact)

---

## 1. Introduction

Road Map Creator is a powerful yet easy-to-use tool for creating roads, paths, and tracks in your Unity projects. Whether you're developing a racing game, city simulator, or any project that requires custom path creation, this tool provides an intuitive editor workflow and high-quality mesh generation.

**Key Features:**

- Intuitive point-and-click interface for creating paths
- Bezier curve system for smooth roads with precise control
- Automatic mesh generation for both the road surface and adjacent terrain
- Customizable road width, materials, and texture tiling
- Support for multiple roads per editor
- Support for both open paths and closed loops
- Complete editor integration with undo/redo support

---

## 2. Installation

1. Import the Road Map Creator package into your Unity project
2. Ensure you have a terrain or other collidable surface in your scene where you want to place the road
3. Import the MapEditor prefab into your scene
4. Ensure the MapEditor prefab is positioned at (0,0,0)

**Minimum Requirements:**

- Unity 2020.3 or higher
- Universal Render Pipeline (URP) or Built-In Render Pipeline

---

## 3. Quick Start Guide

Follow these steps to quickly create your first road:

1. Select the MapEditor prefab in your scene
2. In the Inspector, click "Create New Road" to add a road to your map
3. Change the Edit Mode to "AddPoints"
4. Click on your terrain in the Scene view to add points along your desired path
5. Add at least 3-4 points to see a nice curve
6. Switch to "EditPoints" mode to fine-tune your path
7. Select any point to adjust its position or control handles
8. Select the road in the hierarchy to customize its properties
9. Ensure the RoadGenerator component has appropriate materials assigned

You can create multiple roads using the "Create New Road" button in the MapEditor inspector.

Your first road is now complete! Continue reading for more detailed instructions on customization and advanced features.

---

## 4. MapEditor Component

The MapEditor component is the core of the road creation system. It manages the path points and provides the interface for editing.

### 4.1 Properties

| Property            | Description                                                                              |
| ------------------- | ---------------------------------------------------------------------------------------- |
| Edit Mode           | Controls the current editing behavior (Disabled, AddPoints, EditPoints)                  |
| Show Control Points | Toggles visibility of Bezier curve control handles                                       |
| Curve Resolution    | Controls the smoothness of the curve (higher values = smoother curves but more vertices) |
| Closed Path         | When enabled, connects the last point to the first point to form a loop                  |
| Multiple Roads      | The MapEditor can have multiple roads as children, each with its own settings            |

### 4.2 Edit Modes

- **Disabled**: No editing is possible in the Scene view
- **AddPoints**: Click on surfaces to add new points to your path
- **EditPoints**: Select and modify existing points and their control handles

### 4.3 Working with Control Points

Each point on your path is a Bezier anchor point with two control handles:

- The **In Handle** controls how the curve approaches this point
- The **Out Handle** controls how the curve leaves this point

To edit control points:

1. Ensure "Show Control Points" is enabled
2. Switch to "EditPoints" mode
3. Select a point on your path
4. Blue spheres will appear representing the control handles
5. Click and drag these handles to adjust the curve shape
6. Control handles can also be edited numerically in the Inspector when a point is selected

---

## 5. RoadGenerator Component

The RoadGenerator takes a path defined by a MapEditor and generates a mesh representation of a road along that path.

### 5.1 Road Settings

| Setting       | Description                                            |
| ------------- | ------------------------------------------------------ |
| Map Editor    | Reference to the MapEditor component defining the path |
| Road Width    | Width of the road in world units                       |
| Road Material | Material to be applied to the road surface             |
| Height Offset | Small Y-axis offset to prevent z-fighting with terrain |

### 5.2 Terrain Settings

| Setting          | Description                                                                |
| ---------------- | -------------------------------------------------------------------------- |
| Terrain Material | Material to apply to side terrain areas                                    |
| Terrain Size     | Width of terrain on each side of the road                                  |
| Height Offset    | Vertical offset of terrain relative to road (negative values = below road) |

### 5.3 Texture Settings

| Setting           | Description                                         |
| ----------------- | --------------------------------------------------- |
| UV Tiling Density | Controls texture repetition along the road's length |
| UV Tiling Width   | Controls texture tiling across the road's width     |
| Flip Normals      | Toggle if road material appears upside-down         |

---

## 6. RailingEditor Component

The RailingEditor component allows you to create and edit standalone railings using Bezier curves. Railings can be of type Wall or Plane, and support custom materials, offsets, and UV tiling.

### 6.1 Railing Settings

| Setting          | Description                                    |
| ---------------- | ---------------------------------------------- |
| Type             | Wall, Plane, or None                           |
| Material         | Material for the railing                       |
| Offset           | Lateral offset from the path                   |
| Wall Height      | Height of the wall railing (if type is Wall)   |
| Plane Height     | Height of the plane railing (if type is Plane) |
| UV Repeat Factor | Texture tiling along the railing               |
| Closed Path      | Connects last point to first to form a loop    |

### 6.2 Editing Railings

- **Edit Modes:**
  - Disabled: No editing
  - AddPoints: Click to add points
  - EditPoints: Select and move points or control handles
- **Snapping:** Enable vertex snapping and set snap distance
- **Control Points:** Show/hide and edit Bezier handles for each point
- **Actions:**
  - Recalculate Curves: Auto-adjust handles for smoothness
  - Regenerate Railings: Update mesh after changes
  - Clear All Points: Remove all points from the railing
  - Delete Selected Point: Remove the currently selected point

## 7. TrafficLineGenerator Component

The TrafficLineGenerator component creates and displays traffic lines along a path. Lines can be set to follow the road automatically or use a custom manual path.

| Property        | Description                                      |
| --------------- | ------------------------------------------------ |
| Path Mode       | Automatic (follows road) or Manual (custom path) |
| Line Properties | See below                                        |
| Path Width      | Width of the road/path for line placement        |
| Show Line       | Toggle line visibility                           |

**Traffic Line Properties:**

- Enabled: Toggle this line
- Reverse Direction: Flip arrow direction
- Line Width: Width of the line
- Arrow Spacing: Distance between arrows
- Arrow Size: Size of direction arrows

**Editing:**

- In Manual mode, you can set, update, or clear path points for the traffic line.

---

## 8. Step-by-Step Tutorials

### 8.1 Creating a Basic Road

1. Import the MapEditor prefab into your scene
2. Ensure the MapEditor prefab is positioned at (0,0,0)
3. Assign a material to the "Road Material" field in the RoadGenerator component
4. In the MapEditor, set Edit Mode to "AddPoints"
5. Click on your terrain to add points, creating a path
6. Add at least 3-4 points to form a nice curve
7. The road mesh will generate automatically along this path
8. Roads support automatic railings and traffic lines.

### 8.2 Adding and Editing manual Railings

1. Add a RailingEditor component to your scene
2. Set Edit Mode to AddPoints and click to add points
3. Switch to EditPoints to adjust points and handles
4. Choose Wall or Plane type and assign a material
5. Adjust offset, height, and UV repeat as needed
6. Use the action buttons to recalculate curves or regenerate the mesh

### 8.3 Adding and Editing manual Traffic Lines

1. Add a TrafficLineGenerator to your road or path
2. Set Path Mode to Manual
3. Adjust line properties (width, arrows, etc.)
4. In Manual mode, set or update path points as needed
5. Toggle Show Line to preview in the scene

---

## 11. Tips and Best Practices

- **Multiple Roads**: Use multiple roads for different road types (highways, dirt paths, etc.) sharing the same path
- **Performance**: Keep curve resolution at a reasonable level (10-15) for good performance
- **Road Planning**: Sketch your road layout before placing points for better results
- **Point Spacing**: Place points closer together for tight curves, farther apart for straighter sections
- **Control Handles**: For sharp turns, keep control handles closer to their anchor points
- **Materials**: Use materials with tiling textures designed for roads for best visual results
- **Terrain Integration**: Adjust the terrain height offset to blend roads seamlessly with your landscape
- **Undo Support**: Remember that you can use Ctrl+Z (Cmd+Z on Mac) to undo changes
- **Selection**: Click away from points to deselect the current point
- **Prefab Position**: Ensure the MapEditor prefab remains at position (0,0,0) for best results

---

## 12. Troubleshooting

**Road mesh is not generating:**

- Ensure the MapEditor prefab is at position (0,0,0)
- Verify that you have at least 2 points in your path
- Check that materials are assigned to the RoadGenerator
- Click the "Regenerate Mesh" button to force an update
- Make sure the road's MapEditor reference is set correctly

**Road appears upside-down:**

- Try enabling the "Flip Normals" option in the RoadGenerator

**Z-fighting between road and terrain:**

- Increase the "Height Offset" value slightly

**Curves are too angular:**

- Increase the "Curve Resolution" value in the MapEditor
- Add more points to create smoother transitions

**Editor is unresponsive:**

- Ensure you're in the correct Edit Mode for your desired action
- Check the Console for any error messages
- Restart Unity if issues persist

---

## 13. Support and Contact

For additional support:

- Email: valvano.m@live.it
- Website: www.maurovalvano.it
- Documentation Updates: Check the Unity Asset Store page for the latest documentation

We welcome your feedback and feature suggestions for future updates!

---

_This document was last updated on 25/04/2025._
