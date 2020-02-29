extern "C" {
  #include <stdlib.h>
}

#include "Arduino.h"
#include "Position.h"

Position::Position(long range)
{
  this->stepperPosition = 0;
  this->range = range;
}

long Position::Quickest(long azimuth)
{
  long bounded = this->DegreesToPos(azimuth) - this->stepperPosition;
  long cross_clockwise = (this->stepperPosition + this->range) - this->DegreesToPos(azimuth);
  long cross_counterclockwise = (this->DegreesToPos(azimuth) + this->range) - this->stepperPosition;

  if(abs(bounded) < abs(cross_clockwise) && abs(bounded) < abs(cross_counterclockwise))
    return bounded;
  else if(abs(cross_clockwise) < abs(bounded) && abs(cross_clockwise) < abs(cross_counterclockwise))
    return cross_clockwise * -1;
  else if(abs(cross_counterclockwise) < abs(bounded) && abs(cross_counterclockwise) < abs(cross_clockwise))
    return cross_counterclockwise;
  
  return bounded;
}

void Position::Sync(long azimuth)
{
  this->stepperPosition = this->DegreesToPos(azimuth);
}

long Position::Stepper()
{
  return this->stepperPosition;
}

long Position::Degrees()
{
  return this->PosToDegrees(this->stepperPosition);
}

long Position::PosToDegrees(long pos)
{
  return map(pos, 0, this->range, 0, 360);
}

long Position::DegreesToPos(int degrees)
{
  return map(degrees, 0, 360, 0, this->range) + 1;
}

Position Position::operator++(int)
{
  if((this->stepperPosition + 1) > this->range)
  {
    this->stepperPosition += 1 - this->range;
  }
  else
  {
    this->stepperPosition += 1;
  }
  
  return *this;
}

Position Position::operator--(int)
{
  if(this->stepperPosition == 0 )
  {
    this->stepperPosition = this->range;
  }
  else
  {
    this->stepperPosition -= 1;
  }
  
  return *this;
}


Position Position::operator=(long pos)
{
  this->stepperPosition = pos;
  return *this;
}
