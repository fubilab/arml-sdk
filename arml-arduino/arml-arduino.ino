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

// How many pixels are assigned to the logo window
#define LOGO_COUNT 9

// NeoPixel brightness, 0 (min) to 255 (max)
#define BRIGHTNESS 200  // max = 255

// Declare our NeoPixel strip object:
Adafruit_NeoPixel strip(LED_COUNT, LED_PIN, NEO_GRBW + NEO_KHZ800);

// Declare BNO IMU object 
Adafruit_BNO055 bno = Adafruit_BNO055(55);

// loading <-> ready transition
bool armlLoading = true;
bool armlReady = false;

// BNO state
bool bnoActive = false;

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
int brightness = BRIGHTNESS;
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

// Split mode: the strip is two solid halves (first half colour A, second half colour B). Used by Unity to show
// the hand tracking quality. While active, it is redrawn when the loading -> ready fade ends, so the fade
// cannot overwrite it.
#define SPLIT_PREFIX "ARML_SPLIT_"
#define SPLIT_PREFIX_LENGTH 11
bool splitMode = false;
int splitColorA[] = { 0, 0, 0, 0, 255 };
int splitColorB[] = { 0, 0, 0, 0, 255 };

void setup(void) {
  Serial.begin(115200);

  //configure pin 4 as an input and enable the internal pull-up resistor
  pinMode(4, INPUT_PULLUP);
  pinMode(13, OUTPUT);

  //neopixel init
  strip.begin();  // INITIALIZE NeoPixel strip object (REQUIRED)

  showDefault();
}

void loop(void) {
  currentMillis = millis();

  readButton();
  readBno();
  receiveCmdLoop();
  updateLoadingReady();
  updateColorProgress();

  // Read everything that arrived, but stop at the end of a command so the next one is not appended to it
  while (Serial.available() && !stringComplete) {
    char inChar = (char)Serial.read();
    if (inChar == '\n') {
      stringComplete = true;
    } else {
      inputString += inChar;
    }
  }

  delay(5);
}

void activateBNO() {
  /* Initialise the BNO sensor */
  if (bno.begin()) {
    bnoActive = true;
    delay(100);
    bno.setExtCrystalUse(true);
  } else {
    /* There was a problem detecting the BNO055 ... check your connections */
    Serial.print("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
  }
}

// color loop stuff
int fadeVal=100, fadeMax=100;
int fadeStep = 4;
uint32_t firstPixelHue = 0;
int numRainbowLoops = 20;
int rainbowLoopCount = 0;

void showDefault() {
  splitMode = false;
  strip.setBrightness(brightness);

  float GB = 128 * fadeVal / fadeMax;
  float W = 255 * fadeVal / fadeMax;
  //show default patterns
  strip.fill(strip.Color(0, strip.gamma8(GB), strip.gamma8(GB), strip.gamma8(W)));
  uint32_t goblinStartHue = 36000L;
  for(int i=strip.numPixels() - LOGO_COUNT, j = 0; i<strip.numPixels(); i++, j++) {
    uint32_t pixelHue = goblinStartHue - j * 4000L;
    strip.setPixelColor(i, strip.gamma32(strip.ColorHSV(pixelHue, 255 * 0.5,
    GB)));
  }
  strip.show();
  solidColorState = HIGH; // stop animations
}

void updateLoadingReady() {
  if (armlReady && armlLoading) {
    // transition: loading -> ready
    fadeVal -= fadeStep;
    if(fadeVal <= 0) { 
      armlLoading = false;
    }
  }
  if (!armlReady && !armlLoading) {
    //transition: ready -> loading
    fadeVal -= fadeStep;
    if(fadeVal <= 0) { 
      armlLoading = true;
    }
  }
  if (armlLoading && !armlReady && fadeVal < fadeMax) {
    fadeVal += fadeStep; 
  }
  if (armlReady && !armlLoading && fadeVal < fadeMax) {
    fadeVal += fadeStep;
  }
  if (armlLoading) {
    showLoadingLoop();
  } else if(fadeVal < fadeMax) {
    showIdle();
  }
  if (fadeVal > fadeMax) {
    fadeVal = fadeMax;
  }
}

// What is shown when loading is done: the split colours when Unity set them, the default lights otherwise
void showIdle() {
  if (splitMode) {
    showSplit();
  } else {
    showDefault();
  }
}

// Two solid halves: first half colour A, second half colour B. A hand tracking quality of nothing is both black.
void showSplit() {
  int half = LED_COUNT / 2;
  strip.fill(strip.Color(splitColorA[0], splitColorA[1], splitColorA[2], splitColorA[3]), 0, half);
  strip.fill(strip.Color(splitColorB[0], splitColorB[1], splitColorB[2], splitColorB[3]), half, LED_COUNT - half);
  strip.show();
  solidColorState = HIGH;  // stop animations
}

// Parses "R<r>G<g>B<b>W<w>A<a>E" into color[5] and applies the alpha. Returns false when it is malformed.
bool parseColorCmd(String cmd, int color[]) {
  int r = cmd.indexOf('R');
  int g = cmd.indexOf('G');
  int b = cmd.indexOf('B');
  int w = cmd.indexOf('W');
  int a = cmd.indexOf('A');
  int e = cmd.indexOf('E');
  if (r != 0 || g < 0 || b < 0 || w < 0 || a < 0 || e < 0)
    return false;
  if (!(r < g && g < b && b < w && w < a && a < e))
    return false;

  color[0] = cmd.substring(r + 1, g).toInt();
  color[1] = cmd.substring(g + 1, b).toInt();
  color[2] = cmd.substring(b + 1, w).toInt();
  color[3] = cmd.substring(w + 1, a).toInt();
  color[4] = cmd.substring(a + 1, e).toInt();
  applyAlphaValue(color);
  return true;
}

// ARML_SPLIT_<colorA>_<colorB>, each colour in the solid colour format, e.g.
// ARML_SPLIT_R255G160B0W0A255E_R0G255B0W0A255E
void processSplitCmd(String cmd) {
  String body = cmd.substring(SPLIT_PREFIX_LENGTH);
  int separator = body.indexOf('_');
  if (separator < 0)
    return;

  int colorA[5];
  int colorB[5];
  if (!parseColorCmd(body.substring(0, separator), colorA) || !parseColorCmd(body.substring(separator + 1), colorB))
    return;

  for (int i = 0; i < 5; i++) {
    splitColorA[i] = colorA[i];
    splitColorB[i] = colorB[i];
  }
  splitMode = true;
  showSplit();
}


void showLoadingLoop() {
  //int loopPixels = strip.numPixels() - LOGO_COUNT;
  int loopPixels = strip.numPixels();
  for(int i=0; i<loopPixels; i++) { // For each pixel in strip...
    // Offset pixel hue by an amount to make one full revolution of the
    // color wheel (range of 65536) along the length of the strip
    uint32_t pixelHue = firstPixelHue + (i * 65536L / loopPixels);

    // strip.ColorHSV() can take 1 or 3 arguments: a hue (0 to 65535) or
    // optionally add saturation and value (brightness) (each 0 to 255).
    // Here we're using just the three-argument variant, though the
    // second value (saturation) is a constant 255.
    strip.setPixelColor(i, strip.gamma32(strip.ColorHSV(pixelHue, 255,
      255 * fadeVal / fadeMax)));
  }
  strip.show();
  firstPixelHue += 256;
}

void readBno() {
  if (!bnoActive) {
    return;
  }
  /* Get a new sensor event */
  imu::Quaternion quat = bno.getQuat();

  Serial.print(quat.x(), 4);
  Serial.print(",");
  Serial.print(quat.y(), 4);
  Serial.print(",");
  Serial.print(quat.z(), 4);
  Serial.print(",");
  Serial.print(quat.w(), 4);
  Serial.println("");
}

void receiveCmdLoop() {
  if (millis() - previousReceiveCmdMillis < receiveCmdInterval)
    return;

  if (stringComplete) {
    Serial.print("CMD received: ");
    Serial.println(inputString);
    inputString.trim();
    if (inputString.indexOf(SPLIT_PREFIX) == 0) {  // checked first: split colours (hand tracking quality)
      processSplitCmd(inputString);
    } else if (inputString.indexOf("P_") >= 0) {  //If command contains "P_", set progress colour
      //processSetProgressColorCmd(inputString);
    } else if (inputString.indexOf("Anim") >= 0) {
      processAnimationColorCmd(inputString);
    } else if (inputString.indexOf("ARML_READY") == 0) {
      armlReady = true;
    } else if (inputString.indexOf("ARML_LOADING") == 0) {
      armlReady = false;
    } else if (inputString.indexOf("ARML_DEFAULT") == 0) {
      showDefault();
    } else if (inputString.indexOf("ARML_B") == 0) {
      processBrightnessCmd(inputString);
    } else if (inputString.indexOf("ARML_ENABLE_BNO") == 0) {
      activateBNO();
    } else {
      processSetSolidColorCmd(inputString);
    }
    inputString = "";
    stringComplete = false;
  }

  previousReceiveCmdMillis += receiveCmdInterval;
}

void processBrightnessCmd(String cmd) {
  brightness = cmd.substring(cmd.indexOf("B") + 1).toInt();
  strip.setBrightness(brightness);
  strip.show();
}

void processSetSolidColorCmd(String cmd) {
  //Serial.println("Set Solid Color: " + cmd);
  solidColorState = HIGH;

  splitMode = false;

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

  applyAlphaValue(solidColor);
  strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
  strip.show();
}

// apply alpha value in RGBWA color[4] to RGB
void applyAlphaValue(int color[]) {
  float v = (float)color[4] / (float)255;
  color[0] = round(color[0] * v);
  color[1] = round(color[1] * v);
  color[2] = round(color[2] * v);
}

void processAnimationColorCmd(String cmd) {
  Serial.println("processAnimationColorCmd: " + cmd);

  //Get first color
  String firstColorCmd = cmd.substring(cmd.indexOf("S") + 2, cmd.indexOf("X") - 1);

  solidColor[0] = firstColorCmd.substring(firstColorCmd.indexOf("R") + 1, firstColorCmd.indexOf("G")).toInt();
  solidColor[1] = firstColorCmd.substring(firstColorCmd.indexOf("G") + 1, firstColorCmd.indexOf("B")).toInt();
  solidColor[2] = firstColorCmd.substring(firstColorCmd.indexOf("B") + 1, firstColorCmd.indexOf("W")).toInt();
  solidColor[3] = firstColorCmd.substring(firstColorCmd.indexOf("W") + 1, firstColorCmd.indexOf("A")).toInt();
  solidColor[4] = firstColorCmd.substring(firstColorCmd.indexOf("A") + 1).toInt();
  applyAlphaValue(solidColor);

  //Get second color
  String secondColorCmd = cmd.substring(cmd.indexOf("X") + 2, cmd.indexOf("H") - 1);

  progressColor[0] = secondColorCmd.substring(secondColorCmd.indexOf("R") + 1, secondColorCmd.indexOf("G")).toInt();
  progressColor[1] = secondColorCmd.substring(secondColorCmd.indexOf("G") + 1, secondColorCmd.indexOf("B")).toInt();
  progressColor[2] = secondColorCmd.substring(secondColorCmd.indexOf("B") + 1, secondColorCmd.indexOf("W")).toInt();
  progressColor[3] = secondColorCmd.substring(secondColorCmd.indexOf("W") + 1, secondColorCmd.indexOf("A")).toInt();
  progressColor[4] = secondColorCmd.substring(secondColorCmd.indexOf("A") + 1).toInt();
  applyAlphaValue(progressColor);

  //Get Animation pixel length
  animationPixelLength = cmd.substring(cmd.indexOf("L") + 1).toInt();

  //Get Animation start/end pixel
  startPixelIndex = cmd.substring(cmd.indexOf("PS") + 2).toInt();
  endPixelIndex = cmd.substring(cmd.indexOf("PE") + 2).toInt();

  currentPixelIndex = startPixelIndex;

  //Get Snake or or regular animation state
  snakeAnimation = LOW;
  if (cmd.indexOf("Anim2") >= 0) {
    snakeAnimation = HIGH;
    Serial.println("isSnake");
  }

  backwardsMode = LOW;
  if (cmd.indexOf("Ba") >= 0) {
    backwardsMode = HIGH;
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

  animationRate = animationRate / (endPixelIndex - startPixelIndex);  //Divide by length of animation in pixels so takes same amount regardless of length
  
  
  //Set Solid Color
  if (clearPixelsOutsideRange == LOW) {
    strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
  } else {
    strip.fill(strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]), startPixelIndex, endPixelIndex);
  }

  strip.show();

  //This line makes sure the animation starts from the beginning, otherwise it "catches up" to the current milli (interesting effect)
  previousLedPixelChangeMillis = millis();

  solidColorState = LOW;
  splitMode = false;
}

void updateColorProgress() {
  if (solidColorState == HIGH)
    return;

  if (millis() - previousLedPixelChangeMillis < animationRate)
    return;

  int drawAtIndex = currentPixelIndex % endPixelIndex;
  //WRAP
  if (backwardsMode == HIGH) {
    drawAtIndex = endPixelIndex - drawAtIndex - 1;
  }

//  Serial.println(String(currentPixelIndex) + ":" + String(drawAtIndex));

  //REGULAR ANIMATION
  if (snakeAnimation == LOW) {
    //REACHED END?
    if (currentPixelIndex > 0 && (currentPixelIndex % endPixelIndex == 0)) {
      // stop anim
      solidColorState = HIGH;
      return;
    }
  }
  //SNAKE ANIMATION
  else {
    int removeAtIndex;
    if (backwardsMode == LOW) {
      removeAtIndex = (currentPixelIndex - animationPixelLength) % endPixelIndex;
    } else {
      removeAtIndex = (drawAtIndex + animationPixelLength) % endPixelIndex;
    }
    //Remove trail (snake)
    strip.setPixelColor(removeAtIndex, strip.Color(solidColor[0], solidColor[1], solidColor[2], solidColor[3]));
  }

  strip.setPixelColor(drawAtIndex, strip.Color(progressColor[0], progressColor[1], progressColor[2], progressColor[3]));
  
  previousLedPixelChangeMillis += animationRate;
  
  currentPixelIndex++;

  strip.show();
}

bool buttonDown = false;
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
    Mouse.release(MOUSE_LEFT);
    if (buttonDown) {
      // do onButtonUp actions
//      if (solidColorState == HIGH) {
//        armlLoading = false;
//        armlReady = true;
//        showDefault();
//        processAnimationColorCmd("Anim2Ba_S_R227G0B255W128A255E_X_R26G255B0W128A25E_H2_L20_PS0_PE54");
//      } else {
//        showDefault();
//      }
    }
    buttonDown = false;
  } else {
    digitalWrite(13, HIGH);
    Mouse.press(MOUSE_LEFT);
    buttonDown = true;
    //Serial.println("Button being pressed");

  }

  previousButtonMillis += buttonInterval;
}
