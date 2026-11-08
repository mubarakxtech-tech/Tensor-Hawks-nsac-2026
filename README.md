# 🚀 Tensor Hawks: Space Mission Design Game
### 2026 NASA Space Apps Challenge Submission

## 🌌 Overview
Designing a space mission requires balancing competing demands—including payload mass, budget limits, power availability, and orbital mechanics constraints. **Tensor Hawks** is an interactive space mission design simulation that allows players to make real engineering trade-offs and simulate complete space missions from concept to operation.

---

## ⚙️ System Architecture
Our application utilizes a modern, decoupled hybrid architecture:
- **Frontend (Unity / C# / WebGL):** Manages the user interface, interactive assembly dashboards, telemetry panels, and real-time mission status updates.
- **Backend (Python / FastAPI):** Acts as the core physics engine, evaluating deterministic engineering equations (Tsiolkovsky rocket equation, inverse-square law solar power decay, and telemetry latency).

```text
┌──────────────────────────────────────┐                   ┌──────────────────────────────────────┐
│          UNITY (WebGL / C#)          │                   │            PYTHON SERVER             │
│                                      │                   │          (FastAPI Backend)           │
│  - User selects payload/rocket UI    │   HTTP POST /run  │                                      │
│  - Collects mass, budget, target     ├──────────────────>│  - Runs Tsiolkovsky Equation         │
│  - Displays telemetry dashboards     │   (JSON Body)     │  - Calculates solar power falloff    │
│  - Renders 2D/3D orbit visualizer    │                   │  - Evaluates mission success status  │
│                                      │   HTTP Response   │                                      │
│  - Updates UI with dynamic stats     │<──────────────────┤  - Returns calculation results JSON  │
└──────────────────────────────────────┘    (JSON Data)    └──────────────────────────────────────┘