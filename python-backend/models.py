# from pydantic import BaseModel, Field
# from typing import List

# class SimulationInput(BaseModel):
#     selected_rocket: str = Field(..., description="Rocket option: 'Saturn V', 'Space Launch System (SLS)', or 'Falcon 9'")
#     target_destination: str = Field(..., description="Destination: 'Mars', 'Jupiter', or 'Saturn'")
#     # power_system_id: str
#     # comm_system_id: str
#     # thruster_id: str
#     instruments_ids: List[str]
#     total_fuel_mass_kg: float = Field(..., gt=0)
#     # budget_limit_usd: float = Field(..., gt=0)

# class TelemetryOutput(BaseModel):
#     provided_delta_v: float
#     required_delta_v: float
#     provided_power_au: float
#     data_latency_seconds: float

# class SimulationResponse(BaseModel):
#     telemetry: TelemetryOutput
#     mission_status: str
#     mission_log: List[str]




from pydantic import BaseModel, Field
from typing import List

class SimulationInput(BaseModel):
    selected_rocket: str = Field(..., description="Rocket option: 'Saturn V', 'Space Launch System SLS', or 'SpaceX Falcon 9'")
    target_destination: str = Field(..., description="Destination: 'Mars', 'Jupiter', or 'Saturn'")
    # instruments_ids: List[str]
    total_fuel_mass_kg: float = Field(..., gt=0)

class TelemetryOutput(BaseModel):
    provided_delta_v: float
    required_delta_v: float
    # provided_power_au: float
    data_latency_seconds: float

class SimulationResponse(BaseModel):
    telemetry: TelemetryOutput
    mission_status: str
    mission_log: List[str]