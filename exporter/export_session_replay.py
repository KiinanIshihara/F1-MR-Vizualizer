import json
from pathlib import Path

import fastf1
import fastf1.plotting
import numpy as np
import pandas as pd

OUTPUT_PATH = Path("C:/Users/keena/Projects/F1-MR-Visualizer/Unity/F1-MR-Visualizer/Assets/Resources/spa_2023_q_replay.json")
OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)

fastf1.Cache.enable_cache("../cache_dir")
fastf1.plotting.setup_mpl(misc_mpl_mods=False)

YEAR = 2023
GP = "Belgium"
SESSION = "Q"
SAMPLE_DT = 0.2  # 5 Hz is enough for full-session replay

session = fastf1.get_session(YEAR, GP, SESSION)
session.load()

print("pos_data keys:", session.pos_data.keys())
print("car_data keys:", session.car_data.keys())

driver_number = str(session.results.iloc[0]["DriverNumber"])
print("Testing driver number:", driver_number)

pos = session.pos_data[driver_number]
car = session.car_data[driver_number]

print("POS COLUMNS:")
print(pos.columns)
print(pos.head())

print("CAR COLUMNS:")
print(car.columns)
print(car.head())

def td_to_seconds(value):
    if pd.isna(value):
        return None
    return value.total_seconds()


def safe_round(value, digits=3):
    if value is None or pd.isna(value):
        return None
    return round(float(value), digits)


def get_team_color_safe(team_name):
    try:
        return fastf1.plotting.get_team_color(team_name, session=session)
    except Exception:
        return "#FFFFFF"
    

def resample_driver_session(driver_number, sample_dt=0.2):
    driver_key = str(driver_number)

    if driver_key not in session.pos_data:
        print(f"No pos_data for driver number {driver_key}")
        return []

    pos = session.pos_data[driver_key].copy()

    if pos.empty:
        return []

    if "SessionTime" in pos.columns:
        pos["t"] = pos["SessionTime"].dt.total_seconds()
    elif "Time" in pos.columns:
        pos["t"] = pos["Time"].dt.total_seconds()
    else:
        print(f"No usable time column for driver number {driver_key}")
        return []

    pos = pos[["t", "X", "Y", "Z"]].dropna()

    if pos.empty:
        return []

    speed_available = False

    if driver_key in session.car_data:
        car = session.car_data[driver_key].copy()

        if not car.empty and "Speed" in car.columns:
            if "SessionTime" in car.columns:
                car["t"] = car["SessionTime"].dt.total_seconds()
            elif "Time" in car.columns:
                car["t"] = car["Time"].dt.total_seconds()
            else:
                car = pd.DataFrame()

            if not car.empty:
                car = car[["t", "Speed"]].dropna()
                speed_available = not car.empty
        else:
            car = pd.DataFrame()
    else:
        car = pd.DataFrame()

    if speed_available:
        merged = pd.merge_asof(
            pos.sort_values("t"),
            car.sort_values("t"),
            on="t",
            direction="nearest"
        )
    else:
        merged = pos.copy()
        merged["Speed"] = 0.0

    merged = merged.dropna(subset=["t", "X", "Y", "Z"])

    start_t = merged["t"].min()
    end_t = merged["t"].max()

    if pd.isna(start_t) or pd.isna(end_t) or end_t <= start_t:
        return []

    target_times = np.arange(start_t, end_t, sample_dt)

    def interp(col):
        return np.interp(target_times, merged["t"], merged[col])

    xs = interp("X")
    ys = interp("Y")
    zs = interp("Z")

    if "Speed" in merged.columns and merged["Speed"].notna().any():
        valid_speed = merged.dropna(subset=["Speed"])
        speeds = np.interp(target_times, valid_speed["t"], valid_speed["Speed"])
    else:
        speeds = np.zeros_like(target_times)

    samples = []

    for t, x, y, z, speed in zip(target_times, xs, ys, zs, speeds):
        samples.append({
            "t": safe_round(t),
            "x": safe_round(x),
            "y": safe_round(y),
            "z": safe_round(z),
            "speed": safe_round(speed)
        })

    return samples


def build_track_polyline():
    for _, row in session.results.iterrows():
        code = row["Abbreviation"]
        laps = session.laps.pick_drivers(code)

        try:
            fastest = laps.pick_fastest()
        except Exception:
            continue

        if fastest is None or fastest.empty:
            continue

        try:
            samples = resample_single_lap_for_track(fastest)
        except Exception:
            continue

        if samples:
            return [{"x": s["x"], "y": s["y"], "z": s["z"]} for s in samples]

    return []

def resample_single_lap_for_track(lap, sample_dt=0.1):
    pos = lap.get_pos_data().copy()

    if pos.empty or "Time" not in pos.columns:
        return []

    pos["t"] = pos["Time"].dt.total_seconds()
    pos = pos[["t", "X", "Y", "Z"]].dropna()

    if pos.empty:
        return []

    # Normalize only for track-shape generation, not actual replay.
    pos["t"] = pos["t"] - pos["t"].min()

    duration = pos["t"].max()
    if pd.isna(duration) or duration <= 0:
        return []

    target_times = np.arange(0, duration, sample_dt)

    xs = np.interp(target_times, pos["t"], pos["X"])
    ys = np.interp(target_times, pos["t"], pos["Y"])
    zs = np.interp(target_times, pos["t"], pos["Z"])

    samples = []
    for t, x, y, z in zip(target_times, xs, ys, zs):
        samples.append({
            "t": safe_round(t),
            "x": safe_round(x),
            "y": safe_round(y),
            "z": safe_round(z)
        })

    return samples


drivers_out = []
max_duration = 0.0

for _, row in session.results.iterrows():
    code = row["Abbreviation"]
    full_name = row["FullName"]
    team_name = row["TeamName"]

    driver_number = ""
    if "DriverNumber" in row and not pd.isna(row["DriverNumber"]):
        driver_number = str(row["DriverNumber"])

    color_hex = get_team_color_safe(team_name)

    try:
        fastest_lap = session.laps.pick_drivers(code).pick_fastest()
        lap_time = fastest_lap["LapTime"]
        fastest_lap_seconds = lap_time.total_seconds() if not pd.isna(lap_time) else 0.0
    except Exception:
        fastest_lap_seconds = 0.0

    print(f"Exporting {code}...")

    samples = resample_driver_session(driver_number, SAMPLE_DT)

    if not samples:
        print(f"Skipping {code}: no samples")
        continue

    max_duration = max(max_duration, samples[-1]["t"])

    drivers_out.append({
        "driverCode": code,
        "fullName": full_name,
        "teamName": team_name,
        "colorHex": color_hex,
        "driverNumber": driver_number,
        "fastestLapSeconds": round(float(fastest_lap_seconds), 3),
        "samples": samples
    })

track_polyline = build_track_polyline()

export_obj = {
    "sessionName": f"{YEAR} {session.event['EventName']} {session.name} Replay",
    "trackName": "Spa-Francorchamps",
    "sampleRateHz": int(round(1.0 / SAMPLE_DT)),
    "durationSeconds": round(float(max_duration), 3),
    "trackPolyline": track_polyline,
    "drivers": drivers_out
}

with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
    json.dump(export_obj, f, indent=2)

print(f"Exported {OUTPUT_PATH}")
print(f"Drivers exported: {len(drivers_out)}")
print(f"Duration: {max_duration:.1f}s")
