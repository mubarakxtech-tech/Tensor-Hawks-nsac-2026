using System;
using System.Collections.Generic;

namespace TensorHawks.Data
{
    // JSON Request Payload sent from Unity to Python FastAPI
    [Serializable]
    public class SimulationRequest
    {
        public string selected_rocket;        // "Saturn V", "SLS", "Falcon 9"
        public string target_destination;     // "Moon Orbit", "Mars Orbit", "Jupiter Orbit", "Saturn Orbit"
        public float total_fuel_mass_kg;      // Fuel Slider value
        public float payload_mass_kg;         // Payload Slider value
        public string power_system_id = "SP_StandardAlpha_02";
        public string comm_system_id = "COMM_DeepSpace_HGA_01";
        public string thruster_id = "THR_HallEffect_Ion_03";
        public List<string> instruments_ids = new List<string> { "INS_MAG_01", "INS_RAD_02" };
        public double budget_limit_usd = 2500000000;
    }

    // JSON Telemetry DTO
    [Serializable]
    public class TelemetryData
    {
        public float provided_delta_v;
        public float required_delta_v;
        public float provided_power_au;
        public float data_latency_seconds;
    }

    // JSON Response Payload returned from Python FastAPI
    [Serializable]
    public class SimulationResponse
    {
        public TelemetryData telemetry;
        public string mission_status; // "PASSING" or "FAILED"
        public List<string> mission_log;
        public int science_score;
    }
}