#include "Dome.h"
#include "Messenger.h"
//#include<avr/wdt.h> 
#include <SPI.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128 // OLED display width, in pixels
#define SCREEN_HEIGHT 64 // OLED display height, in pixels
#define OLED_RESET     -1 // Reset pin # (or -1 if sharing Arduino reset pin)
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

Dome dome = Dome();
Messenger message = Messenger();
bool listening = 1;
bool manual_turn_east = 0;
bool manual_turn_west = 0;

long previous_millis=0, oled_update_period_ms = 100;

void messageCompleted() {
    dome.interpretCommand(&message);
}

void setup(){
  Serial.begin(9600);
  Serial.flush();
  delay(1000);

  Serial.println("ASCOM.Arduino.Dome");
    
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

  attachInterrupt(digitalPinToInterrupt(encoderA), update_position, CHANGE); // pin 2
  attachInterrupt(digitalPinToInterrupt(encoderB), update_position, CHANGE); // pin 3
    
  // OLED initialization
  if (!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) { 
      Serial.println(F("SSD1306 OLED allocation failed"));
  }
  display.clearDisplay();
  display.setTextSize(2);
  display.setTextColor(WHITE,0);        
  display.setCursor(0, 1);             
  display.println(F("AAAOV DOME"));
  display.display();
  delay(2000);
  display.clearDisplay();
  display.setTextSize(3);
  
  //wdt_enable(WDTO_2S); //watchdog
}

void update_oled() {
    char buf[4];
    if ((millis()-previous_millis) > oled_update_period_ms) {
        previous_millis = millis();
        sprintf(buf, "%3d", dome.GetAzimuth());
        display.setCursor(35, 20);             // Start at top-left corner
        display.print(buf);
        display.print((char)247);
        display.display();
    }

}

void loop(){

    manual_turn_east = !digitalRead(manual_east);
    manual_turn_west = !digitalRead(manual_west);
    update_oled();

    if (manual_turn_east || manual_turn_west) {
        delayMicroseconds(1000);
        manual_turn_east = !digitalRead(manual_east);
        manual_turn_west = !digitalRead(manual_west);
        if (manual_turn_east || manual_turn_west) {
            if (listening) Serial.println("SLEWING");
            listening = 0;
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
            delayMicroseconds(1000);
            manual_turn_east = !digitalRead(manual_east);
            manual_turn_west = !digitalRead(manual_west);
            if (!manual_turn_east & !manual_turn_west) {
                listening = 1;
                dome.AbortSlew();
                Serial.println("STOP");
                dome.PrintAzimuth();
            }
        };
    }

  //wdt_reset();
}


