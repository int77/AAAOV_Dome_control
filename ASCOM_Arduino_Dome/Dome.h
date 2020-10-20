#ifndef Dome_h
#define Dome_h

#include <inttypes.h>
#include <avr/io.h>
#include <EEPROM.h>

#include "Messenger.h"
#include "Position.h"

#define encoderA 2
#define encoderB 3
#define GO_LEFT 4
#define GO_RIGHT 5
#define homeSensor 6
#define manual_west 8
#define manual_east 7
#define FULLROTATION 1000

class Dome
{
  public:
    Dome(void);
    void interpretCommand(Messenger *message);
    void Park();
    void OpenCloseShutter(int open);
    void Slew(long val);
    void Go_West();
    void Go_East();
    void AbortSlew();
    void SyncToAzimuth(long azimuth);
    void FindHome();
    void SetHomeAzimuth(long azimuth);
    void SetParkAzimuth(long azimuth);
    void Calibrate();
    void SaveConfig();
    void ReadConfig();
    void PrintAzimuth();
    long GetAzimuth();
    
  private:
    void step(long val);
    //long GetPosition(long azimuth);
    void PrintStepperPosition();
};

//extern void update_positionA();
//extern void update_positionB();

extern void update_position();

#endif
