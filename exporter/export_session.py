import fastf1

fastf1.Cache.enable_cache("C:/Users/keena/Projects/F1-MR-Vizualizer/cache_dir")

session = fastf1.get_session(2023, "Belgium", "Q")
session.load()

print(session.event["EventName"])
#print(session.name)
#print(session.date)
#print(session.drivers)
#print(session.results[["Abbreviation", "FullName", "TeamName"]].head())

driver_code = session.results.iloc[0]["Abbreviation"]
print("Testing driver: ", driver_code)

laps = session.laps.pick_drivers(driver_code)
fastest_lap = laps.pick_fastest()

