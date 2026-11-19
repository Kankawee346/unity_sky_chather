#include <WiFi.h>
#include <PubSubClient.h>
#include "Settings.h"
#include "GameLcd.h"

constexpr int LeftFsrPin = 4;
constexpr int RightFsrPin = 5;
constexpr int JoystickYPin = 6;
constexpr int FsrPressRaw = 500;
constexpr int FsrReleaseRaw = 350;
constexpr int JoystickYCenterRaw = 1970;
constexpr int JoystickYDeadZone = 250;
// Set false if moving the stick left makes the Y reading fall instead of rise.
constexpr bool JoystickYHighMeansLeft = true;
constexpr unsigned long SampleIntervalMs = 5;
constexpr unsigned long HeartbeatIntervalMs = 50;
constexpr unsigned long AnalogPrintIntervalMs = 50;
constexpr unsigned long ReconnectIntervalMs = 2000;

WiFiClient network;
PubSubClient mqtt(network);
bool leftPressed = false;
bool rightPressed = false;
bool showAnalog = false;
unsigned long lastSample = 0;
unsigned long lastAnalogPrint = 0;
unsigned long lastReconnect = 0;
unsigned long lastPublish = 0;
int lastPublishedState = -1;

void setup()
{
    Serial.begin(9600);
    Serial.println("Send r in Serial Monitor to toggle analog readings.");
    analogReadResolution(12);
    WiFi.mode(WIFI_STA);
    WiFi.setSleep(false);
    WiFi.setAutoReconnect(true);
    WiFi.begin(WifiName, WifiPassword);
    mqtt.setServer(BrokerHost, 1883);
    mqtt.setSocketTimeout(1);
    mqtt.setCallback(receiveGameLcd);
    beginGameLcd();
}

void loop()
{
    while (Serial.available() > 0)
    {
        if (Serial.read() == 'r')
        {
            showAnalog = !showAnalog;
            Serial.println(showAnalog ? "fsr_left_raw,fsr_right_raw,joystick_y_raw" : "Analog readings off");
        }
    }

    const unsigned long now = millis();
    tickGameLcd(mqtt.connected());
    if (now - lastSample < SampleIntervalMs)
        return;

    lastSample = now;
    const int leftRaw = analogRead(LeftFsrPin);
    const int rightRaw = analogRead(RightFsrPin);
    const int joystickYRaw = analogRead(JoystickYPin);
    leftPressed = leftRaw >= (leftPressed ? FsrReleaseRaw : FsrPressRaw);
    rightPressed = rightRaw >= (rightPressed ? FsrReleaseRaw : FsrPressRaw);

    if (showAnalog && now - lastAnalogPrint >= AnalogPrintIntervalMs)
    {
        lastAnalogPrint = now;
        Serial.printf("%d,%d,%d\n", leftRaw, rightRaw, joystickYRaw);
    }

    if (WiFi.status() != WL_CONNECTED)
        return;

    if (!mqtt.connected())
    {
        if (now - lastReconnect >= ReconnectIntervalMs)
        {
            lastReconnect = now;
            if (mqtt.connect("catchlab-esp32s3", BrokerUser, BrokerPassword))
            {
                network.setNoDelay(true);
                lastPublishedState = -1;
                Serial.println("MQTT connected");
                mqtt.subscribe("catchlab/game/status");
            }
            else
                Serial.printf("MQTT connection failed (%d)\n", mqtt.state());
        }
        return;
    }

    mqtt.loop();
    int direction = 0;
    if (joystickYRaw > JoystickYCenterRaw + JoystickYDeadZone)
        direction = JoystickYHighMeansLeft ? -1 : 1;
    else if (joystickYRaw < JoystickYCenterRaw - JoystickYDeadZone)
        direction = JoystickYHighMeansLeft ? 1 : -1;

    const int state = int(leftPressed) * 6 + int(rightPressed) * 3 + direction + 1;
    if (state == lastPublishedState && now - lastPublish < HeartbeatIntervalMs)
        return;

    char sample[16];
    snprintf(sample, sizeof(sample), "%d,%d,%d", int(leftPressed), int(rightPressed), direction);
    if (mqtt.publish("catchlab/controller", sample))
    {
        lastPublishedState = state;
        lastPublish = now;
    }
}
