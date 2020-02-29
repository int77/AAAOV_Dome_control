#include "Dome.h"
#include "Messenger.h"
#include<avr/wdt.h> 


Dome dome = Dome();
Messenger message = Messenger();

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

  attachInterrupt(0, update_position1, CHANGE);
  attachInterrupt(1, update_position2, CHANGE);

  //wdt_enable(WDTO_2S); //watchdog
}

void messageCompleted(){
  dome.interpretCommand(&message);
}

void loop(){
  while(Serial.available()) message.process(Serial.read());

  //wdt_reset();
}


