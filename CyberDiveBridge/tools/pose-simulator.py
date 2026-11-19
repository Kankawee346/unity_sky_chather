"""Camera-free simulator: type p + Enter for parry, n + Enter to lower arm."""
import json
import os
import threading
import time
import uuid
import paho.mqtt.client as mqtt

host = os.environ.get("CYBER_MQTT_HOST", "127.0.0.1")
password = os.environ["CYBER_MQTT_PASSWORD"]
session = uuid.uuid4().hex
active = False
running = True
client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="pose-sim-" + session[:8])
client.username_pw_set("catchlab", password)
client.connect(host, 1883, 30)
client.loop_start()

def keyboard():
    global active, running
    while running:
        try:
            key = input("p=raise, n=neutral, q=quit > ").strip().lower()
        except EOFError:
            running = False
            return
        if key == "q":
            running = False
        elif key in ("p", "n"):
            active = key == "p"

threading.Thread(target=keyboard, daemon=True).start()
try:
    seq = 0
    while running:
        seq += 1
        raised = active
        client.publish("catchlab/pose", json.dumps({"v": 1, "sessionId": session,
            "seq": seq, "gesture": "PARRY" if raised else "NONE", "active": raised,
            "confidence": 0.99}), qos=0, retain=False)
        time.sleep(0.1)
finally:
    client.disconnect()
    client.loop_stop()
