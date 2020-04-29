#include "CmdMessenger.h"
#include "ccTalk.h"
#include "headers.h"
#include "helpers.h"

// --------------- PIN CONFIGURATION ---------------
// pin 2 = pulso para abrir chapa (OUTPUT)
// pin 3 = pulso que indica si chapa esta abierta (INPUT)
const uint8_t NUM_LOCKER = 1;
uint8_t inputPines[NUM_LOCKER] = {3};
uint8_t outputPines[NUM_LOCKER] = {2};
int dm6 = 0;
int iCE = 0;

//uint8_t inputPines[NUM_DIGITAL_PINS] = { 4, 5, 6, 7 };
//uint8_t outputPines[NUM_DIGITAL_PINS] = { 9, 10, 11, 12 };
//uint8_t inputPines[NUM_DIGITAL_PINS] = { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
//uint8_t outputPines[NUM_DIGITAL_PINS] = { 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
//uint8_t inputPines[1] = { 4 };
//uint8_t outputPines[1] = { 9 };
//const uint8_t pinBaliza = 41;
// --------------- PIN CONFIGURATION ---------------

CmdMessenger cmdMessenger = CmdMessenger(Serial);
ccTalk SCA1(&Serial1);
//int credit;

void OnPinConfiguration()
{
    int pin;
    uint8_t indexInput, indexOutput;
    char *token, *rest, *pinConfig = cmdMessenger.getArgs();

    if (strlen(pinConfig) == 0)
    {
        cmdMessenger.sendMsg(ERR_ARGS_EMPTY);
        return;
    }

    rest = pinConfig;
    while ((token = strtok_r(rest, ",", &rest)))
    {
        if (strlen(token) >= 3)
        {
            sscanf(token, "%2d", &pin);
            if (token[2] == 'I')
            {
                pinMode(pin, INPUT);
                inputPines[indexInput++] = pin;
            }
            else
            {
                pinMode(pin, OUTPUT);
                outputPines[indexOutput++] = pin;
            }
        }
        else
        {
            cmdMessenger.sendMsg(ERR_PIN_CONFIG_LEN);
            return;
        }
    }

    cmdMessenger.sendMsg(ERR_SUCCESS);
}

void OnSinglePinStatus()
{
    uint8_t pin = cmdMessenger.readUInt8Arg();
    if (pin == 0)
    {
        cmdMessenger.sendMsg(ERR_ARGS_EMPTY);
        return;
    }

    if (pin >= NUM_DIGITAL_PINS)
    {
        cmdMessenger.sendMsg(ERR_PIN_OUT_OF_RANGE);
        return;
    }

    if (getPinMode(pin) != INPUT)
    {
        cmdMessenger.sendMsg(ERR_PIN_IS_NOT_INPUT);
        return;
    }

    bool value = digitalRead(pin);
    char response[5];

    sprintf(response, "%02d|%d", pin, value);
    cmdMessenger.sendMsg(ERR_SUCCESS, response);
}

void OnConfiguredInputPinStatus()
{
    uint8_t i, j;
    bool pinValue;
    char pinValues[NUM_LOCKER * 4 + 1];

    for (i = 0; i < NUM_LOCKER; i++, j += 4)
    {
        pinValue = digitalRead(inputPines[i]);
        sprintf(pinValues + j, "%02d%d%c", inputPines[i], pinValue, ',');
    }
    *(pinValues + j - 1) = 0; // Removes the last comma

    cmdMessenger.sendMsg(ERR_SUCCESS, pinValues);
}

void OnAllInputPinStatus()
{
    uint8_t i, j;
    bool pinValue;
    char pinValues[NUM_DIGITAL_PINS * 4 + 1];

    for (i = 1; i < NUM_DIGITAL_PINS; i++, j += 4)
    {
        if (getPinMode(i) == INPUT)
        {
            pinValue = digitalRead(i);
            sprintf(pinValues + j, "%02d%d%c", i, pinValue, ',');
        }
    }
    *(pinValues + j - 1) = 0; // Removes the last comma

    cmdMessenger.sendMsg(ERR_SUCCESS, pinValues);
}

void OnSetPinValue()
{
    uint8_t pin = cmdMessenger.readUInt8Arg();
    uint8_t value = cmdMessenger.readUInt8Arg();
    char response[5];

    if (pin == 0 || pin >= NUM_DIGITAL_PINS)
    {
        cmdMessenger.sendMsg(ERR_PIN_OUT_OF_RANGE);
        return;
    }

    uint8_t bit = digitalPinToBitMask(pin);
    uint8_t port = digitalPinToPort(pin);
    volatile uint8_t *reg = portModeRegister(port);

    if (!(*reg & bit))
    {
        cmdMessenger.sendMsg(ERR_PIN_IS_NOT_OUTPUT);
        return;
    }

    if (value == 0 || value > 4)
    {
        cmdMessenger.sendMsg(ERR_PIN_VALUE_OUT_OF_RANGE);
        return;
    }

    switch (value)
    {
    case 1:
        digitalWrite(pin, LOW);
        break;
    case 2:
        digitalWrite(pin, HIGH);
        break;
    case 3:
        digitalWrite(pin, LOW);
        delay(500);
        digitalWrite(pin, HIGH);
        break;
    case 4:
        digitalWrite(pin, HIGH);
        delay(500);
        digitalWrite(pin, LOW);
        break;
    }

    sprintf(response, "%02d|%d", pin, value);
    cmdMessenger.sendMsg(ERR_SUCCESS, response);
}

void OnCCTalkInit()
{
    //SerialUSB.println("Hola");
    //SerialUSB.flush();
    SCA1.comm_init();
    while (SCA1.RX_state != ccTalk::RXidle)
    {
        SCA1.ccTalkReceive();
    }

    SCA1.device_init();

    //verify inhibits are disabled, should print 255 255
    //Serial.println("Getting inhibit status");
    //Serial.println(SCA1.get_inhibit());

    //toggle inhibit to test functionality, should print 0 0
    //Serial.println("Enabling inhibit");
    //SCA1.inhibit_on();
    //Serial.println("Getting inhibit status");
    //Serial.println(SCA1.get_inhibit());

    //SCA1.inhibit_off();
    //Serial.println("Ready to accept coins");
    cmdMessenger.sendMsg(ERR_SUCCESS);
}

void OnCCTalkInhibitOn()
{
    SCA1.master_inhibit_on();
    cmdMessenger.sendMsg(ERR_SUCCESS);
}

void OnCCTalkInhibitOff()
{
    SCA1.master_inhibit_off();
    cmdMessenger.sendMsg(ERR_SUCCESS);
}

void OnCCTalkGetSerialNumber()
{
    char *serialNumber;

    serialNumber = SCA1.getSerialNumber();
    cmdMessenger.sendMsg(ERR_SUCCESS, serialNumber);
}

void OnCCTalkGetCoinValues()
{
    uint8_t i, j;
    unsigned int *coinValues;
    char coinValuesArray[MAX_COIN_CHANNELS * 4 + 1];

    coinValues = SCA1.getCoinValues();

    for (i = 0; i < MAX_COIN_CHANNELS; i++, j += 4)
    {
        sprintf(coinValuesArray + j, "%03d%c", *(coinValues + i), ',');
        
    }

    *(coinValuesArray + j - 1) = 0; // Removes the last comma
    cmdMessenger.sendMsg(ERR_SUCCESS, coinValuesArray);
}

void OnCCTalkGetCredit()
{
    uint8_t i, j;
    unsigned int *coinCredits, coinEventCounter;
    char creditArray[(MAX_CLP_CHANNELS + 1) * 4 + 1];

    coinCredits = SCA1.getCoinCredits();
    coinEventCounter = SCA1.getCoinEventCounter();

    //******************************************************
    if (iCE == coinEventCounter) return;
    iCE = coinEventCounter;
    //******************************************************
    
    sprintf(creditArray, "%03d%c", coinEventCounter, ';');

    for (i = 0, j = 4; i < MAX_CLP_CHANNELS; i++, j += 4)
    {
        sprintf(creditArray + j, "%03d%c", *(coinCredits + i), ',');
    }

    *(creditArray + j - 1) = 0; // Removes the last comma      
    cmdMessenger.sendMsg(ERR_SUCCESS, creditArray);
}

void OnCCTalkResetDevice()
{
    SCA1.device_reset();
    cmdMessenger.sendMsg(ERR_SUCCESS);
}

void attachCommandCallbacks()
{
    cmdMessenger.attach(PIN_CONFIGURATION, OnPinConfiguration);
    cmdMessenger.attach(SINGLE_PIN_STATUS, OnSinglePinStatus);
    cmdMessenger.attach(CONFIGURED_INPUT_PIN_STATUS, OnConfiguredInputPinStatus);
    cmdMessenger.attach(SET_PIN_VALUE, OnSetPinValue);

    cmdMessenger.attach(CCT_INIT, OnCCTalkInit);
    cmdMessenger.attach(CCT_INHIBIT_ON, OnCCTalkInhibitOn);
    cmdMessenger.attach(CCT_INHIBIT_OFF, OnCCTalkInhibitOff);
    cmdMessenger.attach(CCT_GET_SERIAL_NUMBER, OnCCTalkGetSerialNumber);
    cmdMessenger.attach(CCT_GET_COIN_VALUES, OnCCTalkGetCoinValues);
    cmdMessenger.attach(CCT_GET_CREDIT, OnCCTalkGetCredit);
    cmdMessenger.attach(CCT_RESET_DEVICE, OnCCTalkResetDevice);
}

void setup()
{
    Serial.begin(9600);
    Serial1.begin(9600);
    //Serial2.begin(9600);

    // Wait until connection is established
    while (!Serial);
    while (!Serial1);
    //while (!Serial2);

    attachCommandCallbacks();

    // --------------- SET PINES ---------------
    uint8_t i;

    for (i = 0; i < NUM_LOCKER; i++)
    {
        digitalWrite(outputPines[i], HIGH);
    }

    for (i = 0; i < NUM_LOCKER; i++)
    {
        pinMode(outputPines[i], OUTPUT);
    }

    //pinMode(pinBaliza, OUTPUT);

    for (i = 0; i < NUM_LOCKER; i++)
    {
        pinMode(inputPines[i], INPUT);
    }
    // --------------- SET PINES ---------------    
}


void loop()
{
  if(dm6 == 0)
  {
    Serial.print("entro");
    dm6++;
    OnCCTalkInhibitOff();
  }
    //cmdMessenger.feedinSerialData();
    SCA1.read_credit();
    OnCCTalkGetCredit();
    delay(1000);   
    //Serial.println(SCA1.read_credit());  
}
