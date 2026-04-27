import json
from pathlib import Path

import fastf1
import numpy as np
import pandas as pd

OUTPUT_PATH = Path("output/spa_2023_q.json")
OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)

fastf1.Cache.enable_cache("../cache_dir")

YEAR = 2023
GP = "Belgium"
SESSION = "Q"
SAMPLE_DT = 0.1   # 10 Hz

session = fastf1.get_session(YEAR, GP, SESSION)
session.load()

def td_to_seconds(series):
    return series.dt.total_seconds()

def resample_driver_lap(lap, sample_dt=0.1):
    tel = lap.get_telemetry().copy()

    print(tel.columns) #temporary debug

    required_cols = ["Time", "X", "Y", "Z", "Speed"]
    missing = [c for c in required_cols if c not in tel.columns]
    if missing:
        raise ValueError(f"Missing telemetry columns: {missing}")

    tel["t"] = tel["Time"].dt.total_seconds()
    tel = tel[["t", "X", "Y", "Z", "Speed"]].dropna()

    tel["t"] = tel["t"] - tel["t"].min()

    duration = tel["t"].max()
    if pd.isna(duration) or duration <= 0:
        return []

    target_times = np.arange(0, duration, sample_dt)

    samples = []
    for t, x, y, z, speed in zip(
        target_times,
        np.interp(target_times, tel["t"], tel["X"]),
        np.interp(target_times, tel["t"], tel["Y"]),
        np.interp(target_times, tel["t"], tel["Z"]),
        np.interp(target_times, tel["t"], tel["Speed"]),
    ):
        samples.append({
            "t": round(float(t), 3),
            "x": round(float(x), 3),
            "y": round(float(y), 3),
            "z": round(float(z), 3),
            "speed": round(float(speed), 3)
        })

    return samples

drivers_out = []
track_polyline = None
max_duration = 0.0

for _, row in session.results.iterrows():
    code = row["Abbreviation"]
    full_name = row["FullName"]
    team_name = row["TeamName"]

    laps = session.laps.pick_drivers(code)
    fastest = laps.pick_fastest()

    if fastest is None or fastest.empty:
        continue
    
    try:
        samples = resample_driver_lap(fastest, SAMPLE_DT)
    except Exception as e:
        print(f"Skipping {code}: {e}")
        continue

    if not samples:
        continue

    if track_polyline is None:
        track_polyline = [
            {"x": s["x"], "y": s["y"], "z": s["z"]}
            for s in samples
        ]

    max_duration = max(max_duration, samples[-1]["t"])
                       
    drivers_out.append({
        "driverCode": code,
        "fullName"  : full_name,
        "teamName"  : team_name,
        "colorHex"  : "#FFFFFF", # fill later TODO
        "samples"   : samples
    })

export_obj = {
    "sessionName": f"{YEAR} {session.event['EventName']} {session.name}",
    "trackName": "Spa-Francorchamps",
    "sampleRateHz": int(round(1.0 / SAMPLE_DT)),
    "durationSeconds": round(max_duration, 3),
    "trackPolyline": track_polyline or [],
    "drivers": drivers_out
}

with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
    json.dump(export_obj, f, indent=2)

print(f"Exported {OUTPUT_PATH}")
print(f"Drivers exported: {len(drivers_out)}")
print(f"Duration: {max_duration:.2f}s")




#import fastf1

#fastf1.Cache.enable_cache("C:/Users/keena/Projects/F1-MR-Visualizer/cache_dir")

#session = fastf1.get_session(2023, "Belgium", "Q")
#session.load()

#print(session.event["EventName"])
#print(session.name)
#print(session.date)
#print(session.drivers)
#print(session.results[["Abbreviation", "FullName", "TeamName"]].head())

#driver_code = session.results.iloc[0]["Abbreviation"]
#print("Testing driver: ", driver_code)

#laps = session.laps.pick_drivers(driver_code)
#fastest_lap = laps.pick_fastest()

#print("Fastest lap:")
#print(fastest_lap[["Driver", "LapTime", "Team"]])

#pos = fastest_lap.get_pos_data()
#car = fastest_lap.get_car_data()

#print("Position columns:", pos.columns.tolist())
#print(pos.head())

#print("Car data columns:", car.columns.tolist())
#print(car.head())

