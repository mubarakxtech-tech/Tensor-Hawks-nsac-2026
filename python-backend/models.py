from pydantic import BaseModel
from typing import List

class SimulationInput(BaseModel):
    selected_rocket: str
    target_destination: str
    power_system_id: str
    comm_system_id: str
    thruster_id: str
    instruments_ids: List[str]
    total_fuel_mass_kg: float
    budget_limit_usd: float

class TelemetryOutput(BaseModel):
    provided_delta_v: float
    required_delta_v: float
    provided_power_au: float
    data_latency_seconds: float

class SimulationResponse(BaseModel):
    telemetry: TelemetryOutput
    mission_status: str
    mission_log: List[str]