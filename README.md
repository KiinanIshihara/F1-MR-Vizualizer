# 🏎️ F1 MR Visualizer - v0.1.0

A mixed reality (MR) app for visualizing an interactible Formula 1 session telemetry in 3D space using FastF1 API and Unity3D.


## 🚀 Overview

This project explores how motorsport telemetry data can be transformed into an immersive spatial experience using XR technologies.

Using FastF1 as a data source, the system extracts session telemetry and renders it in a 3D environment built with Unity, enabling users to visualize race dynamics in real-world space via MR devices (e.g., Meta Quest 3).

## 🛠️ Status
Current version: `v0.1.0-alpha`\
Stage: Early prototype

## 🧠 Concept

Instead of viewing F1 data through traditional 2D dashboards, this project aims to:
- Place a full race track into physical space (in your living room, bedroom, etc.)
- Animate car positions synchronized with the playback of the session 
- Visualize elevation, track layout, and relative positioning
- Enable intuitive spatial understanding of race events

## 🏗️ Tech Stack

- **Python (FastF1)** — telemetry data extraction
- **Unity** — 3D rendering and interaction
- **Blender (planned)** — track and environment modeling
- **XR / MR Devices (planned)** — Meta Quest 3

## 📦 Project Structure
f1-mr-visualizer/\
&emsp;  ├─ data-tools/ # FastF1 scripts and data extraction\
&emsp;  ├─ data/ # Raw and processed telemetry data\
&emsp;  ├─ docs/ # Design notes and specifications\
&emsp;  ├─ unity-client/ # Unity visualization (to be added)\

## 🧪 Current Status

Early development — focusing on:
- Extracting session data using FastF1
- Designing data pipeline for visualization

## 🎯 Version 0 Goals

- Load a single F1 session (race or qualifying)
- Extract car position data over time
- Export data into a Unity-friendly format
- Visualize simple car movement along a track

## 🔮 Future Plans

- Real-time playback controls
- Multi-driver comparison
- Spatial UI for interaction
- Full MR deployment
- Colocation visualization

---

## 📌 Notes

This project is part of a personal exploration into XR, real-time systems, and data visualization.
