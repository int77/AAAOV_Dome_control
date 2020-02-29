#ifndef Dome_h
#define Dome_h

#define encoderA 2
#define encoderB 3
#define GO_LEFT 4
#define GO_RIGHT 5
#define homeSensor 6

#define FULLROTATION 1000

#include <inttypes.h>
#include <avr/io.h>
#include <EEPROM.h>

#include "Messenger.h"
#include "Position.h"

class Dome
{
  public:
    Dome(void);
    void interpretCommand(Messenger *message);
    void Park();
    void OpenCloseShutter(int open);
    void Slew(long val);
    void AbortSlew();
    void SyncToAzimuth(long azimuth);
	void FindHome();
    void SetHomeAzimuth(long azimuth);
	void SetParkPosition(long azimuth);
	void Calibrate();
    void Calibrate1();
	void SaveConfig();
	void ReadConfig();
  private:
    void step(long val);
    long GetAzimuth();
    long GetPosition(long azimuth);
    void PrintAzimuth();
};

extern void update_position1();
extern void update_position2();

#endif
