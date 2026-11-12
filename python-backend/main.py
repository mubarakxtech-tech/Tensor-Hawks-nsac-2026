import math
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from models import SimulationInput, SimulationResponse, TelemetryOutput

app = FastAPI(title="Tensor Hawks Space Mission API", version="2.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Official Rocket Database Specs (derived from NASA & SpaceX resources)
ROCKET_SPECS = {
    "Saturn V": {
        "dry_mass_kg": 2948400.0 - 118800.0,  # Total mass minus LEO payload approximation / baseline dry frame
        "specific_impulse_isp": 3200.0,       # Optimized engine Isp parameter
        "base_cost_usd": 1200000000.0
    },
    "Space Launch System (SLS)": {
        "dry_mass_kg": 2603000.0 - 95000.0,
        "specific_impulse_isp": 3100.0,
        "base_cost_usd": 2000000000.0
    },
    "Falcon 9": {
        "dry_mass_kg": 549054.0 - 22800.0,
        "specific_impulse_isp": 2950.0,
        "base_cost_usd": 400000000.0
    }
}

# Official Delta-V Requirements & Environmental Parameters (m/s)
DESTINATION_SPECS = {
    "Lunar Gateway": {
        "required_dv": 4100.0,
        "latency_seconds": 2.5,
        "power_au": 1.0
    },
    "Mars Orbit": {
        "required_dv": 6200.0,
        "latency_seconds": 750.0,
        "power_au": 0.5
    },
    "Deep Space Asteroid": {
        "required_dv": 7500.0,
        "latency_seconds": 1800.0,
        "power_au": 0.2
    }
}

@app.get("/")
def read_root():
    return {"status": "Tensor Hawks Physics Engine Online", "active_modules": ["simulation", "telemetry"]}

@app.post("/run-simulation", response_model=SimulationResponse)
def run_simulation(data: SimulationInput):
    # Validate Rocket Selection
    if data.selected_rocket not in ROCKET_SPECS:
        raise HTTPException(
            status_code=400, 
            detail=f"Invalid rocket choice. Select from: {list(ROCKET_SPECS.keys())}"
        )
    
    # Validate Destination Selection
    if data.target_destination not in DESTINATION_SPECS:
        raise HTTPException(
            status_code=400, 
            detail=f"Invalid destination. Select from: {list(DESTINATION_SPECS.keys())}"
        )

    rocket = ROCKET_SPECS[data.selected_rocket]
    destination = DESTINATION_SPECS[data.target_destination]

    g0 = 9.81
    dry_mass_kg = rocket["dry_mass_kg"]
    total_wet_mass = dry_mass_kg + data.total_fuel_mass_kg
    specific_impulse_isp = rocket["isp"] if "isp" in rocket else rocket["specific_impulse_isp"]

    # Tsiolkovsky rocket equation calculation for Provided Delta-V
    provided_dv = specific_impulse_isp * g0 * math.log(total_wet_mass / dry_mass_kg)
    required_dv = destination["required_dv"]

    provided_power = destination["power_au"] + (len(data.instruments_ids) * 15.0)
    latency = destination["latency_seconds"]

    mission_logs = [
        f"Launch vehicle configuration loaded: {data.selected_rocket}.",
        f"Target destination locked: {data.target_destination} (Required Δv: {required_dv} m/s).",
        f"Power systems [{data.power_system_id}] and comms [{data.comm_system_id}] initialized.",
        f"Calculated Provided Delta-V: {provided_dv:.1f} m/s."
    ]

    # Verification checks
    if provided_dv >= required_dv and data.budget_limit_usd >= rocket["base_cost_usd"]:
        status = "PASSING"
        mission_logs.append("Mission trajectory verified. All physical and budgetary parameters met successfully!")
    else:
        status = "FAILED"
        reasons = []
        if provided_dv < required_dv:
            reasons.append("Insufficient Delta-V budget")
        if data.budget_limit_usd < rocket["base_cost_usd"]:
            reasons.append("Exceeded financial capital limits")
        mission_logs.append(f"Critical mission failure: {', '.join(reasons)}.")

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