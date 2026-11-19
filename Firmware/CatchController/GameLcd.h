    #pragma once
#include <Arduino.h>
#include "LcdConfig.h"
#if CATCHLAB_LCD_ENABLED
#include <Wire.h>
#include <hd44780.h>
#include <hd44780ioClass/hd44780_I2Cexp.h>
hd44780_I2Cexp gameLcd; // Auto-detect backpack address and pin mapping.
bool lcdReady = false;
char lcdShown[2][17] = {};
char lcdWanted[2][17] = {};
unsigned long lcdStatusAt = 0;
unsigned long lcdPaintAt = 0;
void setLcdLine(int row, const char* value)
{
    memset(lcdWanted[row], ' ', 16);
    const size_t length = strnlen(value, 16);
    memcpy(lcdWanted[row], value, length);
    lcdWanted[row][16] = '\0';
}
void beginGameLcd()
{
    Wire.setPins(LcdSdaPin, LcdSclPin);
    Wire.setTimeOut(5);
    lcdReady = gameLcd.begin(16, 2) == 0;
    if (lcdReady) { setLcdLine(0, "CatchLab ready"); setLcdLine(1, "Waiting for game"); }
    else Serial.println("LCD not detected; sensor input remains available.");
}
void receiveGameLcd(char* topic, byte* payload, unsigned int length)
{
    if (strcmp(topic, "catchlab/game/status") != 0 || length >= 64) return;
    char message[64]; memcpy(message, payload, length); message[length]='\0';
    int score, seconds, level;
    char mode[4], state[6];
    if (sscanf(message,"%d,%d,%d,%3[^,],%5s",&score,&seconds,&level,mode,state)!=5) return;
    if (score<0 || score>999999 || seconds<0 || seconds>30 || level<1 || level>3) return;
    if (strcmp(mode,"FSR") && strcmp(mode,"JOY")) return;
    if (strcmp(state,"READY") && strcmp(state,"PLAY") && strcmp(state,"PAUSE") && strcmp(state,"END")) return;
    char line[17];
    snprintf(line,sizeof(line),"S:%06d T:%02d",score,seconds); setLcdLine(0,line);
    snprintf(line,sizeof(line),"L%d %s %-5s",level,mode,state); setLcdLine(1,line);
    lcdStatusAt=millis();
}
void tickGameLcd(bool connected)
{
    if (!lcdReady) return;
    unsigned long now=millis();
    if (now-lcdPaintAt<10) return;
    lcdPaintAt=now;
    if (!connected) setLcdLine(1,"MQTT offline");
    else if (now-lcdStatusAt>6000) setLcdLine(1,"Waiting for game");
    // Change only one differing character per tick. Avoid clear() and a full 32-character
    // rewrite in the low-latency sensor loop; steady scores usually change one digit.
    for(int row=0;row<2;row++) for(int col=0;col<16;col++)
    {
        if(lcdShown[row][col]==lcdWanted[row][col]) continue;
        gameLcd.setCursor(col,row); gameLcd.write(lcdWanted[row][col]);
        lcdShown[row][col]=lcdWanted[row][col]; return;
    }
}
#else
inline void beginGameLcd() {}
inline void receiveGameLcd(char*, byte*, unsigned int) {}
inline void tickGameLcd(bool) {}
#endif
