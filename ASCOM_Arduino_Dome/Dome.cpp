

//ADDED FOR COMPATIBILITY WITH WIRING
extern "C" {
  #include <stdlib.h>
}

#include "Arduino.h"
#include "Dome.h"

//long parkAzimuth = 170;
//long homeAzimuth = 90;
//long stepsPerRotation = FULLROTATION;

struct config_t {
	long parkAzimuth;
	long homeAzimuth;
	long stepsPerRotation;
	long lastPosition;
} config;

unsigned long pulseCount[2];
unsigned long t[2];
volatile long temp, move;
static boolean rotating = false;
boolean A_set = false;
boolean B_set = false;

Position position(FULLROTATION);

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
		SetParkPosition(message->readInt());
		break;
	case 'C':
		Calibrate();
		break;
	case 'D':
		Calibrate1();
		break;
	case 'W':
		SaveConfig();
		break;
	case 'R':
		ReadConfig();
		break;

  }
}

void Dome::step(long val)
{
  move = abs(val);
    
  if (val > 0) {
	  digitalWrite(GO_RIGHT, LOW);
	  digitalWrite(GO_LEFT, HIGH);
  }
  else {
	  digitalWrite(GO_RIGHT, HIGH);
	  digitalWrite(GO_LEFT, LOW);
  }
  
  while(move > 0)
  {
	rotating = true;

	if(Serial.available() > 0) break;
	//if (move != temp) {
	//	Serial.println(move);
	//	temp = move;
	//}
	//Serial.print(move);
	//PrintAzimuth();
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
  step(position.Quickest(azimuth));
  PrintAzimuth();
  if (position.Degrees()==config.homeAzimuth) Serial.println("ATHOME");
}

void Dome::FindHome()
{
	//step(position.Quickest(azimuth));
	move = 1.5*config.stepsPerRotation;

	digitalWrite(GO_LEFT, HIGH);
	digitalWrite(GO_RIGHT, LOW);

	while(move > 0)
	{
		rotating = true;

		if (Serial.available() > 0) break;
		if (move != temp) {
			//Serial.println(move);
			// PrintAzimuth();
			temp = move;
		}


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
		PrintAzimuth();
		Serial.println("SYNCED");
		Serial.println("ATHOME");
	}
	else {
		Serial.println("HOME not found");
	}

}

void Dome::Calibrate()
{
	move = 256000;
	int i = 0;

	digitalWrite(GO_LEFT, HIGH);
	digitalWrite(GO_RIGHT, LOW);

	while (move > 0)
	{
		rotating = true;

		if (Serial.available() > 0) break;
		if (digitalRead(homeSensor) == LOW) {
			delay(50);
			if (digitalRead(homeSensor) == LOW) {
				pulseCount[i] = move;
				t[i] = millis();
				Serial.print("At HOME position:");
				Serial.println(pulseCount[i]);
				delay(1000);
				i++;
			}
			
		}
		if (i > 1) break;

		//Serial.print(move);
		//PrintAzimuth();
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


void Dome::Calibrate1()
{
	move = 256000;
	int i = 0;

	unsigned long lastDebounceTime = 0;
	unsigned long debounceDelay = 0;
	unsigned long P_count = 0;
	boolean lastState = LOW;
	boolean State;
	static boolean reading;

	digitalWrite(GO_LEFT, HIGH);
	digitalWrite(GO_RIGHT, LOW);

	while (move > 0)
	{
		rotating = false;

		reading = digitalRead(encoderA);

		if (reading != lastState) lastDebounceTime = millis();

		if ((millis() - lastDebounceTime) > debounceDelay) {
			//Serial.println(reading);

			if (reading != State) {
				State = reading;
				
				if (State != lastState) {
					P_count++;
					lastState = State;
					//Serial.println(P_count);
				}
			}

		}

		if (Serial.available() > 0) break;

		if (digitalRead(homeSensor) == LOW) {
			delay(50);
			if (digitalRead(homeSensor) == LOW) {
				pulseCount[i] = P_count;
				t[i] = millis();
				Serial.print("At HOME position:");
				Serial.println(pulseCount[i]);
				delay(1000);
				i++;
			}

		}
		if (i > 1) break;
	}
	//position.range = pulseCount[0] - pulseCount[1];
	AbortSlew();
	if (i > 1) {
		Serial.print("Number of pulses per Dome rotation: ");
		Serial.println(pulseCount[1] - pulseCount[0]);
		Serial.println(config.stepsPerRotation);
		Serial.print("Dome rotation period in seconds: ");
		Serial.println((t[1] - t[0]) / 1000);
	}
}

void Dome::PrintAzimuth()
{
  Serial.print("P ");
  Serial.println(position.Degrees());

}

void Dome::Park()
{
  Slew(config.parkAzimuth);
  Serial.println("PARKED");
}

void Dome::SetHomeAzimuth (long azimuth)
{
	config.homeAzimuth = azimuth;
	//PrintAzimuth();
	Serial.println("Home azimuth changed");
	EEPROM.put(0, config);
}

void Dome::SetParkPosition(long azimuth)
{
	config.parkAzimuth = azimuth;
	//PrintAzimuth();
	Serial.println("Park azimuth changed");
	EEPROM.put(0, config);
}

void Dome::OpenCloseShutter(int open)
{
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
  delay(500);
  Serial.println("SYNCED");
}

void Dome::ReadConfig()
{
	EEPROM.get(0, config);
	
	Serial.print("home azimuth: ");	Serial.println(config.homeAzimuth);
	Serial.print("park azimuth: ");	Serial.println(config.parkAzimuth);
	Serial.print("steps per rotation: ");	Serial.println(config.stepsPerRotation);
	Serial.print("last position: ");	Serial.println(config.lastPosition);
	PrintAzimuth();

}

void Dome::SaveConfig()
{
	//config.homeAzimuth = homeAzimuth;
	//config.parkAzimuth = parkAzimuth;
	//config.lastPosition = 0;
	//config.stepsPerRotation = stepsPerRotation;

	EEPROM.put(0, config);
}


void update_position1() {
	if (rotating) delay(2);
		if (digitalRead(encoderA) != A_set) A_set = !A_set;
	if (A_set && !B_set) {
		position++;
		move--;
	}
	rotating = false;
}

void update_position2() {
	if (rotating) delay(2);
	if (digitalRead(encoderB) != B_set) B_set = !B_set;
	if (B_set && !A_set) {
		position--;
		move--;
	}
	rotating = false;
}

