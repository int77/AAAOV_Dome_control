#include "Dome.h"
#include "Messenger.h"
#include<avr/wdt.h> 

Dome dome = Dome();
Messenger message = Messenger();
bool listening = 1;
bool manual_turn_east = 0;
bool manual_turn_west = 0;

unsigned long current_millis, previous_millis_pa;
unsigned long azimuth_update_period = 100; //ms
long current_azimuth, previous_azimuth;

void setup(){
  Serial.begin(9600);
  Serial.flush();

  Serial.println("R ASCOM.Arduino.Dome");
  
  message.attach(messageCompleted);

  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(GO_LEFT, OUTPUT);
  pinMode(GO_RIGHT, OUTPUT);
  digitalWrite(GO_LEFT, HIGH);
  digitalWrite(GO_RIGHT, HIGH);

  pinMode(encoderA, INPUT_PULLUP);
  pinMode(encoderB, INPUT_PULLUP);
  pinMode(homeSensor, INPUT);
  pinMode(manual_west, INPUT_PULLUP);
  pinMode(manual_east, INPUT_PULLUP);

  //attachInterrupt(digitalPinToInterrupt(encoderA), update_positionA, RISING); // pin 2
  //attachInterrupt(digitalPinToInterrupt(encoderB), update_positionB, RISING); // pin 3

  attachInterrupt(digitalPinToInterrupt(encoderA), update_position, CHANGE); // pin 2
  attachInterrupt(digitalPinToInterrupt(encoderB), update_position, CHANGE); // pin 3

  //wdt_enable(WDTO_2S); //watchdog
}

void messageCompleted(){
  dome.interpretCommand(&message);
}

void loop(){

    manual_turn_east = !digitalRead(manual_east);
    manual_turn_west = !digitalRead(manual_west);

    if (manual_turn_east || manual_turn_west) listening = 0;

    current_millis = millis();
    if ((current_millis - previous_millis_pa) >= azimuth_update_period) 
    {
        previous_millis_pa = current_millis;

        current_azimuth = dome.GetAzimuth();
        if (previous_azimuth != current_azimuth) {
            previous_azimuth = current_azimuth;
            dome.PrintAzimuth();
        }
    } 
 
    if (listening) {
        while (Serial.available()) message.process(Serial.read());
    }
    else {

        if (manual_turn_east) {
            listening = 0;
            dome.Go_East();
        };
        if (manual_turn_west) {
            listening = 0;
            dome.Go_West();
        };

        if(!manual_turn_east & !manual_turn_west) {
            listening = 1;
            dome.AbortSlew();
        };
    }

  //wdt_reset();
}


