#ifndef Position_h
#define Position_h

#include <inttypes.h>
#include <avr/io.h>

class Position {
  public:
    Position(long range);
    Position operator++(int);
    Position operator--(int);
    Position operator=(long pos);
    long Stepper();
    long Degrees();
    long stepperPosition;
    long Quickest(long azimuth);
    void Sync(long azimuth);
	long range;
  private:
    long PosToDegrees(long pos);
    long DegreesToPos(int degrees);
	//long range;
};

#endif
