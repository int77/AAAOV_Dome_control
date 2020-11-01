

extern "C" {
  #include <stdlib.h>
}

#include "Arduino.h"
#include "Dome.h"

long parkAzimuth = 130;
long homeAzimuth = 143;
long stepsPerRotation = 135575;


volatile Position position(FULLROTATION);

struct config_t {
	long parkAzimuth;
	long homeAzimuth;
	long stepsPerRotation;
	long lastPosition;
} config;

unsigned long pulseCount[2];
unsigned long t[2];
volatile long temp, move;
volatile uint8_t state_enc;

//
// Constructor
// ..//
Dome::Dome(void)
{
	EEPROM.get(0, config);

	position = config.lastPosition;
	position.range = config.stepsPerRotation;

}

//
// Function for interpreting a command string of the format ": COMMAND <ARGUMENT> #"
//
void Dome::interpretCommand(Messenger *message)
{
  message->readChar(); // Reads ":"
  char command = message->readChar(); // Read the command
  
  switch(command){
	case 'P':
		Park();
		break;
	case 'O':
		OpenCloseShutter(message->readInt());
		break;
	case 'S':
		Slew(message->readLong());
		break;
	case 'H':
		AbortSlew();
		break;
	case 'T':
		SyncToAzimuth(message->readInt());
		break;
	case 'F':
		FindHome();
		break;
	case 'A':
		SetHomeAzimuth(message->readInt());
		break;
	case 'Z':
		SetParkAzimuth(message->readInt());
		break;
	case 'C':
		Calibrate();
		break;
	case 'W':
		SaveConfig();
		break;
	case 'R':
		ReadConfig();
		break;
	case 'G':
		GetStatus();
		break;
  }
}

void Dome::GetStatus()
{
	Serial.println("CONNECTED");
	PrintAzimuth();
}

void Dome::Go_East() 
{
	digitalWrite(GO_RIGHT, HIGH);
	digitalWrite(GO_LEFT, LOW);
}

void Dome::Go_West()
{
	digitalWrite(GO_RIGHT, LOW);
	digitalWrite(GO_LEFT, HIGH);
}

void Dome::step(long val)
{
	noInterrupts();
	move = abs_macro(val);
	interrupts();
  
  if (val > 0) {
	  Go_West();
  }
  else {
	  Go_East();
  }
  
  while(move > 0)
  {
	//rotating = true;

	if(Serial.available() > 0) break;

  }
 
  AbortSlew();
}

void Dome::AbortSlew()
{
	digitalWrite(GO_LEFT, HIGH);
	digitalWrite(GO_RIGHT, HIGH);
	config.lastPosition = position.stepperPosition;
	config.stepsPerRotation = position.range;
	EEPROM.put(0, config);
}

void Dome::Slew(long azimuth)
{
	Serial.println("SLEWING");
	step(position.Quickest(azimuth));
	PrintAzimuth();
	if ((position.Degrees() >= config.homeAzimuth - 2) && (position.Degrees() <= config.homeAzimuth + 2)) Serial.println("ATHOME");
}

void Dome::FindHome()
{
	Serial.println("SLEWING");
	noInterrupts();
	move = 2*config.stepsPerRotation;
	interrupts();

	Go_West();

	while(move > 0)
	{
		if (Serial.available() > 0) break;
		
		if (digitalRead(homeSensor) == LOW) {
			delay(50);
			if (digitalRead(homeSensor) == LOW) {
				break;
			}
		}

	}
	AbortSlew();
	if (move>0) {
		position.Sync(config.homeAzimuth);
		delay(5000);
		Slew(config.homeAzimuth);
		Serial.println("SYNCED");
		Serial.println("ATHOME");
		PrintAzimuth();
	}
	else {
		Serial.println("HOME not found");
	}

}

void Dome::Calibrate()
{
	Serial.println("SLEWING");
	noInterrupts();
	move = 4194304;
	interrupts();
	int i = 0;

	Go_West();

	while (move > 0)
	{
		if (Serial.available() > 0) break;
		if (digitalRead(homeSensor) == LOW) {
			delay(50);
			if (digitalRead(homeSensor) == LOW) {
				pulseCount[i] = move;
				t[i] = millis();
				Serial.print("At HOME position:");
				Serial.println(pulseCount[i]);
				delay(1500);
				i++;
			}
			
		}
		if (i > 1) break;
	}
	position.range = pulseCount[0] - pulseCount[1];
	AbortSlew();
	if (i > 1) {
		Serial.print("Number of pulses per Dome rotation: ");
		Serial.println(config.stepsPerRotation);
		Serial.print("Dome rotation period in seconds: ");
		Serial.println((t[1]-t[0])/1000);
	}
}

void Dome::PrintAzimuth()
{
  Serial.print("P ");
  Serial.println(position.Degrees());

}

long Dome::GetAzimuth()
{
	return position.Degrees();
}

void Dome::PrintStepperPosition()
{
	Serial.print("S ");
	Serial.println(position.stepperPosition);
}

void Dome::Park()
{
	Serial.println("SLEWING");
	step(position.Quickest(config.parkAzimuth));
	delay(5000);
	step(position.Quickest(config.parkAzimuth));
	PrintAzimuth();
	if ((position.Degrees() >= config.parkAzimuth-2)&&(position.Degrees() <= config.parkAzimuth + 2)) Serial.println("PARKED");
	

}

void Dome::SetHomeAzimuth (long azimuth)
{
	config.homeAzimuth = azimuth;
	Serial.println("Home azimuth changed");
	EEPROM.put(0, config);
}

void Dome::SetParkAzimuth(long azimuth)
{
	config.parkAzimuth = azimuth;
	Serial.println("Park azimuth changed");
	EEPROM.put(0, config);
}

void Dome::OpenCloseShutter(int open)
{
	switch (open)
	{
	case 1:
		Serial.println("SHUTTER OPENING");
		break;
	case 0:
		Serial.println("SHUTTER CLOSING");
		break;
	}

	delay(3000);

	switch(open)
	{
	case 1:
		Serial.println("SHUTTER OPEN");
		digitalWrite(LED_BUILTIN, HIGH);
		break;
	case 0:
		Serial.println("SHUTTER CLOSED");
		digitalWrite(LED_BUILTIN, LOW);
		break;
	}
}

void Dome::SyncToAzimuth(long azimuth)
{
  position.Sync(azimuth);
  PrintAzimuth();
  Serial.println("SYNCED");
}

void Dome::ReadConfig()
{
	EEPROM.get(0, config);
	Serial.print("home azimuth: ");	Serial.println(config.homeAzimuth);
	Serial.print("park azimuth: ");	Serial.println(config.parkAzimuth);
	Serial.print("steps per rotation: ");	Serial.println(config.stepsPerRotation);
	Serial.print("last position: ");	Serial.println(config.lastPosition);
}

void Dome::SaveConfig()
{
	//config.homeAzimuth = homeAzimuth;
	//config.parkAzimuth = parkAzimuth;
	//config.lastPosition = 0;
	//config.stepsPerRotation = stepsPerRotation;

	EEPROM.put(0, config);
}

void update_position() {

	//For old Arduino Nano 
	//uint8_t p1val = PIND & B00000100;
	//uint8_t p2val = PIND & B00001000;

	//For Arduino Nano Every
	uint8_t p1val = VPORTA.IN & B00000001;
	uint8_t p2val = VPORTF.IN & B00100000;

	uint8_t state = state_enc & B00000011;
	
	if (p1val) state |= B00000100;
	if (p2val) state |= B00001000;

	state_enc = (state >> 2);
	switch (state) {
	case 1: case 7: case 8: case 14:
		position++;
		move--;
		return;
	case 2: case 4: case 11: case 13:
		position--;
		move--;
		return;
/*	case 3: case 12:
		position++;
		position++;
		move -= 2;
		return;
	case 6: case 9:
		position++;
		position++;
		move -= 2;
		return;*/
	}
}
