import math
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from models import SimulationInput, SimulationResponse, TelemetryOutput

app = FastAPI(title="Tensor Hawks Space Mission API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
def read_root():
    return {"status": "Tensor Hawks Physics Engine Online"}

@app.post("/run-simulation", response_model=SimulationResponse)
def run_simulation(data: SimulationInput):
    g0 = 9.81
    dry_mass_kg = 12000.0
    total_wet_mass = dry_mass_kg + data.total_fuel_mass_kg
    specific_impulse_isp = 3200.0

    provided_dv = specific_impulse_isp * g0 * math.log(total_wet_mass / dry_mass_kg)

    target_requirements = {
        "Mars Orbit": 6200.0,
        "Lunar Gateway": 4100.0,
        "Deep Space Asteroid": 7500.0
    }
    required_dv = target_requirements.get(data.target_destination, 6000.0)

    provided_power = 450.0
    latency = 750.0 if "Mars" in data.target_destination else 3.0

    mission_logs = [
        "Launch vehicle separation confirmed.",
        f"Target destination set to {data.target_destination}.",
        f"Calculated Provided Delta-V: {provided_dv:.1f} m/s (Required: {required_dv} m/s)."
    ]

    if provided_dv >= required_dv and data.budget_limit_usd >= 1500000000:
        status = "PASSING"
        mission_logs.append("Mission trajectory verified. Success parameters met!")
    else:
        status = "FAILED"
        mission_logs.append("Critical failure: Insufficient Delta-V budget or financial capital.")

    return SimulationResponse(
        telemetry=TelemetryOutput(
            provided_delta_v=round(provided_dv, 2),
            required_delta_v=required_dv,
            provided_power_au=provided_power,
            data_latency_seconds=latency
        ),
        mission_status=status,
        mission_log=mission_logs
    )