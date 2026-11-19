// Use ESP32-S3 ADC1 pins for both FSR dividers and joystick VRy.
// Verify that the joystick output voltage is safe for the ESP32 ADC before wiring.
#if !defined(CONFIG_IDF_TARGET_ESP32S3)
#error "Select an ESP32-S3 board in Arduino IDE."
#endif
constexpr int LeftFsrPin = 4;
constexpr int RightFsrPin = 5;
constexpr int JoystickYPin = 6;
constexpr unsigned long SampleIntervalMs = 70;

void setup()
{
    Serial.begin(115200);
    analogReadResolution(12);
    Serial.println("fsr_left_raw,fsr_right_raw,joystick_y_raw");
}

void loop()
{
    static unsigned long lastSampleMs = 0;
    const unsigned long now = millis();
    if (now - lastSampleMs < SampleIntervalMs)
        return;

    lastSampleMs = now;
    Serial.print(analogRead(LeftFsrPin));
    Serial.print(',');
    Serial.print(analogRead(RightFsrPin));
    Serial.print(',');
    Serial.println(analogRead(JoystickYPin));
}
