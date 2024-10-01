#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>
#include <utility/imumaths.h>
#include "Mouse.h"
#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
#include <avr/power.h>  // Required for 16 MHz Adafruit Trinket
#endif

// Which pin on the Arduino is connected to the NeoPixels?
// On a Trinket or Gemma we suggest changing this to 1:
#define LED_PIN 6

// How many NeoPixels are attached to the Arduino?
#define LED_COUNT 63

// NeoPixel brightness, 0 (min) to 255 (max)
#define BRIGHTNESS 200  // max = 255

// Declare our NeoPixel strip object:
Adafruit_NeoPixel strip(LED_COUNT, LED_PIN, NEO_GRBW + NEO_KHZ800);

// Declare BNO IMU object 
Adafruit_BNO055 bno = Adafruit_BNO055(55);

//Unity command stuff
String inputString = "";
bool stringComplete = false;

//Millis
unsigned long currentMillis = 0;
unsigned long previousButtonMillis = 0;          //stores last time button was checked
unsigned long previousLedPixelChangeMillis = 0;  //stores last time a strip pixel was updated
unsigned long previousReceiveCmdMillis = 0;      //stores last time it listened for commands

//Intervals
int ledChangeInterval = 100;
int buttonInterval = 10;
int receiveCmdInterval = 10;

//Colors
int progressColor[] = { 0, 255, 0, 0, 5 };
int solidColor[] = { 255, 0, 0, 0, 5 };
float animationRate;
int animationPixelLength;

//States
int currentPixelIndex = -1;
byte solidColorState = HIGH;
byte snakeAnimation = LOW;
int startPixelIndex = -1;
int endPixelIndex = LED_COUNT;
byte backwardsMode = LOW;
byte clearPixelsOutsideRange = LOW;

void setup(void) {
  Serial.begin(115200);

  /* Initialise the sensor */
  if (!bno.begin())
  {
    /* There was a problem detecting the BNO055 ... check your connections */
    Serial.print("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
    while (1);
  }

  delay(100);
   
  bno.setExtCrystalUse(true);
  
  //configure pin 4 as an input and enable the internal pull-up resistor
  pinMode(4, INPUT_PULLUP);
  pinMode(13, OUTPUT);

  //neopixel init
  strip.begin();  // INITIALIZE NeoPixel strip object (REQUIRED)
  strip.setBrightness(BRIGHTNESS);

  //show default patterns
  strip.fill(strip.Color(0, strip.gamma8(128), strip.gamma8(128), strip.gamma8(255)));
  uint32_t goblinStartHue = 36000L;
  for(int i=strip.numPixels() - 9, j = 0; i<strip.numPixels(); i++, j++) {
    uint32_t pixelHue = goblinStartHue - j * 4000L;
    strip.setPixelColor(i, strip.gamma32(strip.ColorHSV(pixelHue, 255 * 0.5,
    128)));
  }
  strip.show();
}

void loop(void) {
  currentMillis = millis();

  readButton();
  receiveCmdLoop();

  //rainbowLoop();
  updateColorProgress();

  if (Serial.available()) {
    char inChar = (char)Serial.read();
    if (inChar == '\n') {
      stringComplete = true;
    } else {
      inputString += inChar;
    }
  }

  delay(5);
}

void receiveCmdLoop() {
  if (millis() - previousReceiveCmdMillis < receiveCmdInterval)
    return;

  if (stringComplete) {
    Serial.print("CMD received: ");
    Serial.println(inputString);
    inputString.trim();
    if (inputString.indexOf("P_") >= 0) {  //If command contains "P_", set progress colour
      //processSetProgressColorCmd(inputString);
    } else if (inputString.indexOf("Anim") >= 0) {
      processAnimationColorCmd(inputString);
    } else {
      processSetSolidColorCmd(inputString);
    }
    inputString = "";
    stringComplete = false;
  }

  previousReceiveCmdMillis += receiveCmdInterval;
}

void processSetSolidColorCmd(String cmd) {
  Serial.println("Set Solid Color mode");
  solidColorState = HIGH;

  //Safe checks to ignore non-valid commands
  if (cmd.substring(0, 1) != "R" || cmd.substring(cmd.length(), cmd.length() - 1) != "E" || cmd.length() > 19)
    return;

  //Remove anything after E to prevent wrong colors
  String safeCmd = cmd.substring(0, cmd.indexOf("E"));

  solidColor[0] = safeCmd.substring(cmd.indexOf("R") + 1, cmd.indexOf("G")).toInt();
  solidColor[1] = safeCmd.substring(cmd.indexOf("G") + 1, cmd.indexOf("B")).toInt();
  solidColor[2] = safeCmd.substring(cmd.indexOf("B") + 1, cmd.indexOf("W")).toInt();
  solidColor[3] = safeCmd.substring(cmd.indexOf("W") + 1, cmd.indexOf("A")).toInt();
  solidColor[4] = safeCmd.substring(cmd.indexOf("A") + 1).toInt();

  strip.setBrightness(solidColor[4]);

  strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));

  strip.show();
}

void processAnimationColorCmd(String cmd) {
  Serial.println("Set Animation Color mode");

  //Get first color
  String firstColorCmd = cmd.substring(cmd.indexOf("S") + 2, cmd.indexOf("X") - 1);

  solidColor[0] = firstColorCmd.substring(firstColorCmd.indexOf("R") + 1, firstColorCmd.indexOf("G")).toInt();
  solidColor[1] = firstColorCmd.substring(firstColorCmd.indexOf("G") + 1, firstColorCmd.indexOf("B")).toInt();
  solidColor[2] = firstColorCmd.substring(firstColorCmd.indexOf("B") + 1, firstColorCmd.indexOf("W")).toInt();
  solidColor[3] = firstColorCmd.substring(firstColorCmd.indexOf("W") + 1, firstColorCmd.indexOf("A")).toInt();
  solidColor[4] = firstColorCmd.substring(firstColorCmd.indexOf("A") + 1).toInt();

  //Get second color
  String secondColorCmd = cmd.substring(cmd.indexOf("X") + 2, cmd.indexOf("H") - 1);

  progressColor[0] = secondColorCmd.substring(secondColorCmd.indexOf("R") + 1, secondColorCmd.indexOf("G")).toInt();
  progressColor[1] = secondColorCmd.substring(secondColorCmd.indexOf("G") + 1, secondColorCmd.indexOf("B")).toInt();
  progressColor[2] = secondColorCmd.substring(secondColorCmd.indexOf("B") + 1, secondColorCmd.indexOf("W")).toInt();
  progressColor[3] = secondColorCmd.substring(secondColorCmd.indexOf("W") + 1, secondColorCmd.indexOf("A")).toInt();
  progressColor[4] = secondColorCmd.substring(secondColorCmd.indexOf("A") + 1).toInt();

  //Get Animation pixel length
  animationPixelLength = cmd.substring(cmd.indexOf("L") + 1).toInt();

  //Get Animation start/end pixel
  startPixelIndex = cmd.substring(cmd.indexOf("PS") + 2).toInt();
  endPixelIndex = cmd.substring(cmd.indexOf("PE") + 2).toInt();

  currentPixelIndex = startPixelIndex;

  //Get Snake or or regular animation state
  if (cmd.indexOf("Anim2") >= 0) {
    snakeAnimation = HIGH;
    Serial.println("isSnake");
  } else {
    snakeAnimation = LOW;
    Serial.println("noSnake");
  }

  if (cmd.indexOf("Ba") >= 0) {
    backwardsMode = HIGH;
    currentPixelIndex = endPixelIndex;
    Serial.println("isBack");
  }

  //Clear Pixels Outside Range
  if (cmd.indexOf("_T") >= 0) {
    clearPixelsOutsideRange = HIGH;

    for (int i = 0; i < LED_COUNT; i++) {
      if (i < startPixelIndex || i > endPixelIndex) {
        strip.setPixelColor(i, strip.Color(0, 0, 0, 0));
      }
    }
  }

  //Get Animation rate (comes as seconds from Unity)
  animationRate = cmd.substring(cmd.indexOf("H") + 1).toFloat() * 1000;

  if (snakeAnimation == LOW) {
    animationRate = animationRate * animationPixelLength;  //Multiply by animation length oterwhise it will progress faster than intended if length is more than 1 (unless its snake)
  }
  animationRate = animationRate / (endPixelIndex - startPixelIndex);  //Divide by length of animation in pixels so takes same amount regardless of length

  //Set Solid Color
  if (clearPixelsOutsideRange == LOW) {
    strip.setBrightness(solidColor[4]);  //This line decides whether the strip fills up with background colour when animation starts, or if it becomes a trail (the first cycle)
    strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
  } else {
    strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]), startPixelIndex, endPixelIndex);
  }

  strip.show();

  //This line makes sure the animation starts from the beginning, otherwise it "catches up" to the current milli (interesting effect)
  previousLedPixelChangeMillis = millis();

  solidColorState = LOW;
}

void updateColorProgress() {
  if (solidColorState == HIGH)
    return;

  if (millis() - previousLedPixelChangeMillis < animationRate)
    return;

  //REACHED END
  if (backwardsMode == LOW) {
    if (currentPixelIndex > endPixelIndex + animationPixelLength + 1) {
      currentPixelIndex = startPixelIndex;
    }
  } else {
    if (currentPixelIndex < startPixelIndex - animationPixelLength - 1) {
      currentPixelIndex = endPixelIndex;
    }
  }

  //REGULAR ANIMATION
  if (snakeAnimation == LOW) {
    for (int i = 0; i < animationPixelLength; i++) {
      strip.setPixelColor(currentPixelIndex, strip.Color(progressColor[0], progressColor[1], progressColor[2], progressColor[3]));

      if (backwardsMode == LOW) {
        currentPixelIndex++;
        if (currentPixelIndex > endPixelIndex) {
          currentPixelIndex = startPixelIndex;
          if (clearPixelsOutsideRange == LOW) {
            strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
          } else {
            strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]), startPixelIndex, 10);
          }
        } else {
          currentPixelIndex--;
          if (currentPixelIndex < startPixelIndex - 1) {
            currentPixelIndex = endPixelIndex;
            if (clearPixelsOutsideRange == LOW) {
              strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
            } else {
              strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]), startPixelIndex, 10);
            }
          }
        }
      }
    }
  }

  //SNAKE ANIMATION
  if (snakeAnimation == HIGH) {
    strip.setPixelColor(currentPixelIndex, strip.Color(progressColor[0], progressColor[1], progressColor[2], progressColor[3]));

    if (backwardsMode == LOW) {
      currentPixelIndex++;
      //Remove trail (snake)
      for (int i = 0; i < animationPixelLength; i++) {
        strip.setPixelColor(currentPixelIndex - animationPixelLength - i, strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
      }
    } else {
      currentPixelIndex--;
      //Remove trail (snake)
      for (int i = 0; i < animationPixelLength; i++) {
        strip.setPixelColor(currentPixelIndex + animationPixelLength + 1 + i, strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
      }
    }
  }

  previousLedPixelChangeMillis += animationRate;

  strip.show();
}

void readButton() {
  if (millis() - previousButtonMillis < buttonInterval)
    return;

  //read the pushbutton value into a variable
  int sensorVal = digitalRead(4);

  // Keep in mind the pull-up means the pushbutton's logic is inverted. It goes
  // HIGH when it's open, and LOW when it's pressed. Turn on pin 13 when the
  // button's pressed, and off when it's not:
  if (sensorVal == HIGH) {
    digitalWrite(13, LOW);
  } else {
    digitalWrite(13, HIGH);
    Serial.println("Button being pressed");
  }

  previousButtonMillis += buttonInterval;
}

