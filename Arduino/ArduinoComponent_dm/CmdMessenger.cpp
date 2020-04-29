/*
  CmdMessenger - library that provides command based messaging

  Permission is hereby granted, free of charge, to any person obtaining
  a copy of this software and associated documentation files (the
  "Software"), to deal in the Software without restriction, including
  without limitation the rights to use, copy, modify, merge, publish,
  distribute, sublicense, and/or sell copies of the Software, and to
  permit persons to whom the Software is furnished to do so, subject to
  the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

  Initial Messenger Library - Thomas Ouellet Fredericks.
  CmdMessenger Version 1    - Neil Dudman.
  CmdMessenger Version 2    - Dreamcat4.
  CmdMessenger Version 3    - Thijs Elenbaas.
  3.6  - Fixes
  - Better compatibility between platforms
  - Unit tests
  3.5  - Fixes, speed improvements for Teensy
  3.4  - Internal update
  3.3  - Fixed warnings
  - Some code optimization
  3.2  - Small fixes and sending long argument support
  3.1  - Added examples
  3.0  - Bugfixes on 2.2
  - Wait for acknowlegde
  - Sending of common type arguments (float, int, char)
  - Multi-argument commands
  - Escaping of special characters
  - Sending of binary data of any type (uses escaping)
  */

extern "C" {
#include <stdlib.h>
#include <stdarg.h>
}
#include <stdio.h>
#include "CmdMessenger.h"

#define _CMDMESSENGER_VERSION 3_6 // software version of this library

int msgCounter;

// **** Initialization **** 

/**
 * CmdMessenger constructor
 */
CmdMessenger::CmdMessenger(Stream &ccomms, const char fld_separator, const char cmd_separator, const char esc_character)
{
	init(ccomms, fld_separator, cmd_separator, esc_character);
}

/**
 * Enables printing newline after a sent command
 */
void CmdMessenger::init(Stream &ccomms, const char fld_separator, const char cmd_separator, const char esc_character)
{
	default_callback = NULL;
	comms = &ccomms;
	print_newlines = false;
	field_separator = fld_separator;
	command_separator = cmd_separator;
	escape_character = esc_character;
	//bufferLength = MESSENGERBUFFERSIZE;
	maxMessageLength = MESSENGERBUFFERSIZE - 1;
	reset();

	default_callback = NULL;
	for (int i = 0; i < MAXCALLBACKS; i++)
		callbackList[i] = NULL;

	pauseProcessing = false;
  msgCounter = 0;
}

/**
 * Resets the command buffer and message state
 */
void CmdMessenger::reset()
{
	bufferIndex = 0;
	current = NULL;
	last = NULL;
	dumped = true;
	stxReceived = false;
	etxReceived = false;
}

/**
 * Enables printing newline after a sent command
 */
void CmdMessenger::printLfCr(bool addNewLine)
{
	print_newlines = addNewLine;
}

/**
 * Attaches an default function for commands that are not explicitly attached
 */
void CmdMessenger::attach(messengerCallbackFunction newFunction)
{
	default_callback = newFunction;
}

/**
 * Attaches a function to a command ID
 */
void CmdMessenger::attach(const unsigned int cmd, messengerCallbackFunction newFunction)
{
	if (cmd >= 0 && cmd < MAXCALLBACKS) {
		callbackList[cmd] = newFunction;
	}
}

// **** Command processing ****

/**
 * Feeds serial data in CmdMessenger
 */
void CmdMessenger::feedinSerialData()
{
  //char msgAux[255];
  
	while (!pauseProcessing && comms->available())
	{
		// The Stream class has a readBytes() function that reads many bytes at once. On Teensy 2.0 and 3.0, readBytes() is optimized. 
		// Benchmarks about the incredible difference it makes: http://www.pjrc.com/teensy/benchmark_usb_serial_receive.html

		size_t bytesAvailable = min(comms->available(), MAXSTREAMBUFFERSIZE);
		comms->readBytes(streamBuffer, bytesAvailable);

    /*sprintf(msgAux, "%d: %c", msgCounter, *streamBuffer);
    Serial2.println(msgAux);
    msgCounter++;*/

		// Process the bytes in the stream buffer, and handles dispatches callbacks, if commands are received
		for (size_t byteNo = 0; byteNo < bytesAvailable; byteNo++)
		{
			processLine(streamBuffer[byteNo]);

			// If waiting for acknowledge command
			if (messageState == kEndOfMessage)
			{
				handleMessage();
			}
		}
	}
}

/**
 * Processes bytes and determines message state
 */
void CmdMessenger::processLine(char serialChar)
{
	messageState = kProccesingMessage;
	if (serialChar == STX && !stxReceived) {
		bufferIndex = 0;
		stxReceived = true;
		etxReceived = false;
		commandBuffer[bufferIndex++] = serialChar;
	} else {
		if (stxReceived) {
			commandBuffer[bufferIndex++] = serialChar;
			if (etxReceived) {
				commandBuffer[bufferIndex] = 0;
				currentLrc = serialChar;
				messageState = kEndOfMessage;
				current = commandBuffer;
				bufferLastIndex = bufferIndex;
				reset();
			} else {
				if (serialChar == ETX) {
					// Got ETX, next character is the checksum
					etxReceived = true;
				}

				if (bufferIndex >= maxMessageLength) {
					// The message received is too long, so we clear the buffer and start again.
					reset();
				}
			}
		} else {
			// The current byte will not be processed because the STX byte has not been received.
			// This could happen if the message is truncated.
		}
	}

	//return messageState;
}

/**
 * Validates message format and dispatches attached callbacks based on command
 */
void CmdMessenger::handleMessage()
{
	unsigned char expectedLrc = 0;
	char auxCmd[CMD_LENGTH + 1];
	uint8_t i, dataLen;

	for (i = 0; i < bufferLastIndex - 1; i++) {
		expectedLrc ^= *(commandBuffer + i);
	}

	// We don't know the CMD yet, so we put 999 as a placeholder
	currentCmd = 999; 

	if (currentLrc != expectedLrc) {
		char errMsg[50];
		snprintf(errMsg, sizeof(errMsg), "Expected %02X but got instead %02X", expectedLrc, currentLrc);
		sendMsg(ERR_INVALID_LRC, errMsg);
		return;
	}

	if (bufferLastIndex < MIN_MSG_LENGTH - 1) {
		sendMsg(ERR_INVALID_DATA_LEN);
		return;
	}

	// Extract the DATA field
	dataLen = bufferLastIndex - 3;
	memcpy(currentData, commandBuffer + 1, dataLen);
	*(currentData + dataLen) = 0;

	// Extract the CMD field
	memcpy(auxCmd, currentData, 3);
	*(auxCmd + 3) = 0;
	currentCmd = atoi(auxCmd);

	// Get the body of the DATA if there is any
	if (dataLen > 4) {
		memcpy(currentArgs, currentData + 4, dataLen - 4);
		*(currentArgs + dataLen - 4) = 0;
	}

	// If callback attached, we call it
	switch (currentCmd) 
	{
		case PIN_CONFIGURATION:
		case SINGLE_PIN_STATUS:
		case CONFIGURED_INPUT_PIN_STATUS:
		case ALL_INPUT_PIN_STATUS:
		case SET_PIN_VALUE:
		case CCT_INIT:
		case CCT_INHIBIT_ON:
		case CCT_INHIBIT_OFF:
		case CCT_GET_SERIAL_NUMBER:
		case CCT_GET_COIN_VALUES:
		case CCT_GET_CREDIT:
		case CCT_RESET_DEVICE:
			if (callbackList[currentCmd] != NULL) {
				(*callbackList[currentCmd])();
			}
			break;
		default:
			sendMsg(ERR_UNKNOWN_CMD);
			break;
	}
}

/**
 * Waits for reply from sender or timeout before continuing
 */
bool CmdMessenger::blockedTillReply(unsigned int timeout, byte ackCmdId)
{
	unsigned long time = millis();
	unsigned long start = time;
	bool receivedAck = false;
	while ((time - start) < timeout && !receivedAck) {
		time = millis();
		receivedAck = checkForAck(ackCmdId);
	}
	return receivedAck;
}

/**
 *   Loops as long data is available to determine if acknowledge has come in
 */
bool CmdMessenger::checkForAck(byte ackCommand)
{
	while (comms->available()) {
		//Processes a byte and determines if an acknowlegde has come in
		//int messageState = processLine(comms->read());
		processLine(comms->read());
		if (messageState == kEndOfMessage) {
			int id = readInt16Arg();
			if (ackCommand == id && ArgOk) {
				return true;
			}
			else {
				return false;
			}
		}
		return false;
	}
	return false;
}

/**
 * Gets next argument. Returns true if an argument is available
 */
bool CmdMessenger::next()
{
	char * temppointer = NULL;
	// Currently, cmd messenger only supports 1 char for the field seperator
	switch (messageState) {
	case kProccesingMessage:
		return false;
	case kEndOfMessage:
		temppointer = currentArgs;
		messageState = kProcessingArguments;
	default:
		if (dumped)
			current = split_r(temppointer, field_separator, &last);
		if (current != NULL) {
			dumped = true;
			return true;
		}
	}
	return false;
}

/**
 * Returns if an argument is available. Alias for next()
 */
bool CmdMessenger::available()
{
	//return next();
	return strlen(currentArgs) > 0;
}

/**
 * Returns if the latest argument is well formed.
 */
bool CmdMessenger::isArgOk()
{
	return ArgOk;
}

/**
 * Returns the commandID of the current command
 */
uint8_t CmdMessenger::commandID()
{
	return lastCommandId;
}

// ****  Command sending ****

/**
 * Send start of command. This makes it easy to send multiple arguments per command
 */
void CmdMessenger::sendCmdStart(byte cmdId)
{
	if (!startCommand) {
		startCommand = true;
		pauseProcessing = true;
		comms->print(cmdId);
	}
}

/**
 * Send an escaped command argument
 */
void CmdMessenger::sendCmdEscArg(char* arg)
{
	if (startCommand) {
		comms->print(field_separator);
		printEsc(arg);
	}
}

/**
 * Send formatted argument.
 *  Note that floating points are not supported and resulting string is limited to 128 chars
 */
void CmdMessenger::sendCmdfArg(char *fmt, ...)
{
	const int maxMessageSize = 128;
	if (startCommand) {
		char msg[maxMessageSize];
		va_list args;
		va_start(args, fmt);
		vsnprintf(msg, maxMessageSize, fmt, args);
		va_end(args);

		comms->print(field_separator);
		comms->print(msg);
	}
}

/**
 * Send double argument in scientific format.
 *  This will overcome the boundary of normal float sending which is limited to abs(f) <= MAXLONG
 */
void CmdMessenger::sendCmdSciArg(double arg, unsigned int n)
{
	if (startCommand)
	{
		comms->print(field_separator);
		printSci(arg, n);
	}
}

/**
 * Send end of command
 */
bool CmdMessenger::sendCmdEnd(bool reqAc, byte ackCmdId, unsigned int timeout)
{
	bool ackReply = false;
	if (startCommand) {
		comms->print(command_separator);
		if (print_newlines)
			comms->println(); // should append BOTH \r\n
		if (reqAc) {
			ackReply = blockedTillReply(timeout, ackCmdId);
		}
	}
	pauseProcessing = false;
	startCommand = false;
	return ackReply;
}

/**
 * Send a command without arguments, with acknowledge
 */
bool CmdMessenger::sendCmd(byte cmdId, bool reqAc, byte ackCmdId)
{
	if (!startCommand) {
		sendCmdStart(cmdId);
		return sendCmdEnd(reqAc, ackCmdId, DEFAULT_TIMEOUT);
	}
	return false;
}

/**
 * Send a command without arguments, without acknowledge
 */
bool CmdMessenger::sendCmd(byte cmdId)
{
	if (!startCommand) {
		sendCmdStart(cmdId);
		return sendCmdEnd(false, 1, DEFAULT_TIMEOUT);
	}
	return false;
}

// **** Command receiving ****

/**
 * Find next argument in command
 */
int CmdMessenger::findNext(char *str, char delim)
{
	int pos = 0;
	bool escaped = false;
	bool EOL = false;
	ArglastChar = '\0';
	while (true) {
		escaped = isEscaped(str, escape_character, &ArglastChar);
		EOL = (*str == '\0' && !escaped);
		if (EOL) {
			return pos;
		}
		if (*str == field_separator && !escaped) {
			return pos;
		}
		else {
			str++;
			pos++;
		}
	}
	return pos;
}

/**
 * Read the next argument as int
 */
int16_t CmdMessenger::readInt16Arg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return atoi(current);
	}
	ArgOk = false;
	return 0;
}

/**
 * Read the next argument as int
 */
int32_t CmdMessenger::readInt32Arg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return atol(current);
	}
	ArgOk = false;
	return 0L;
}

/**
 * Read the next argument as bool
 */
bool CmdMessenger::readBoolArg()
{
	return (readInt16Arg() != 0) ? true : false;
}

/**
 * Read the next argument as char
 */
char CmdMessenger::readCharArg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return current[0];
	}
	ArgOk = false;
	return 0;
}

/**
 * Read the next argument as float
 */
float CmdMessenger::readFloatArg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		//return atof(current);
		return strtod(current, NULL);
	}
	ArgOk = false;
	return 0;
}

/**
 * Read the next argument as double
 */
double CmdMessenger::readDoubleArg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return strtod(current, NULL);
	}
	ArgOk = false;
	return 0;
}

/**
 * Read next argument as string.
 * Note that the String is valid until the current command is replaced
 */
char* CmdMessenger::readStringArg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return current;
	}
	ArgOk = false;
	return '\0';
}

/**
 * Read the next argument as uint8_t
 */
uint8_t CmdMessenger::readUInt8Arg()
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		return (uint8_t)atoi(current);
	}
	ArgOk = false;
	return 0;
}

/**
 * Returns all the arguments
 */
char* CmdMessenger::getArgs()
{
	return currentArgs;
}

/**
 * Return next argument as a new string
 * Note that this is useful if the string needs to be persisted
 */
void CmdMessenger::copyStringArg(char *string, uint8_t size)
{
	if (next()) {
		dumped = true;
		ArgOk = true;
		strlcpy(string, current, size);
	}
	else {
		ArgOk = false;
		if (size) string[0] = '\0';
	}
}

/**
 * Compare the next argument with a string
 */
uint8_t CmdMessenger::compareStringArg(char *string)
{
	if (next()) {
		if (strcmp(string, current) == 0) {
			dumped = true;
			ArgOk = true;
			return 1;
		}
		else {
			ArgOk = false;
			return 0;
		}
	}
	return 0;
}

// **** Escaping tools ****

/**
 * Unescapes a string
 * Note that this is done inline
 */
void CmdMessenger::unescape(char *fromChar)
{
	// Move unescaped characters right
	char *toChar = fromChar;
	while (*fromChar != '\0') {
		if (*fromChar == escape_character) {
			fromChar++;
		}
		*toChar++ = *fromChar++;
	}
	// Pad string with \0 if string was shortened
	for (; toChar < fromChar; toChar++) {
		*toChar = '\0';
	}
}

/**
 * Split string in different tokens, based on delimiter
 * Note that this is basically strtok_r, but with support for an escape character
 */
char* CmdMessenger::split_r(char *str, const char delim, char **nextp)
{
	char *ret;
	// if input null, this is not the first call, use the nextp pointer instead
	if (str == NULL) {
		str = *nextp;
	}
	// Strip leading delimiters
	while (findNext(str, delim) == 0 && *str) {
		str++;
	}
	// If this is a \0 char, return null
	if (*str == '\0') {
		return NULL;
	}
	// Set start of return pointer to this position
	ret = str;
	// Find next delimiter
	str += findNext(str, delim);
	// and exchange this for a a \0 char. This will terminate the char
	if (*str) {
		*str++ = '\0';
	}
	// Set the next pointer to this char
	*nextp = str;
	// return current pointer
	return ret;
}

/**
 * Indicates if the current character is escaped
 */
bool CmdMessenger::isEscaped(char *currChar, const char escapeChar, char *lastChar)
{
	bool escaped;
	escaped = (*lastChar == escapeChar);
	*lastChar = *currChar;

	// special case: the escape char has been escaped:
	if (*lastChar == escape_character && escaped) {
		*lastChar = '\0';
	}
	return escaped;
}

/**
 * Escape and print a string
 */
void CmdMessenger::printEsc(char *str)
{
	while (*str != '\0') {
		printEsc(*str++);
	}
}

/**
 * Escape and print a character
 */
void CmdMessenger::printEsc(char str)
{
	if (str == field_separator || str == command_separator || str == escape_character || str == '\0') {
		comms->print(escape_character);
	}
	comms->print(str);
}

/**
 * Print float and double in scientific format
 */
void CmdMessenger::printSci(double f, unsigned int digits)
{
	// handle sign
	if (f < 0.0)
	{
		Serial.print('-');
		f = -f;
	}

	// handle infinite values
	if (isinf(f))
	{
		Serial.print("INF");
		return;
	}
	// handle Not a Number
	if (isnan(f))
	{
		Serial.print("NaN");
		return;
	}

	// max digits
	if (digits > 6) digits = 6;
	long multiplier = pow(10, digits);     // fix int => long

	int exponent;
	if (abs(f) < 10.0) {
		exponent = 0;
	}
	else {
		exponent = int(log10(f));
	}
	float g = f / pow(10, exponent);
	if ((g < 1.0) && (g != 0.0))
	{
		g *= 10;
		exponent--;
	}

	long whole = long(g);                     // single digit
	long part = long((g - whole)*multiplier + 0.5);  // # digits
	// Check for rounding above .99:
	if (part == 100) {
		whole++;
		part = 0;
	}
	char format[16];
	sprintf(format, "%%ld.%%0%dldE%%+d", digits);
	char output[16];
	sprintf(output, format, whole, part, exponent);
	comms->print(output);
}

void CmdMessenger::sendMsg(const char *responseCode, char *message)
{
	unsigned char	LRC;
	char			fullMessage[MESSENGERBUFFERSIZE];
	char			fullData[MAX_DATA_SIZE];
	uint8_t			len;
  char msg2[255];

  /*sprintf(msg2, "CMD = %d", currentCmd);
  Serial2.println(msg2);

  sprintf(msg2, "RC = %s", responseCode);
  Serial2.println(msg2);

  if (message != NULL) {
    sprintf(msg2, "MSG = %s", message);
    Serial2.println(msg2);
  }*/
    
	sprintf(fullData, "%d|%s", currentCmd, responseCode);
	if (message != NULL && *message != 0) {
		sprintf(fullData, "%s|%s", fullData, message);
	}

	// calculate LRC
	LRC = STX;
	for (len = 0; *(fullData + len) != 0; len++)
		LRC ^= *(fullData + len);
	LRC ^= ETX;

	sprintf(fullMessage, "%c%s%c%c", STX, fullData, ETX, LRC);
	comms->print(fullMessage);
	comms->flush();
	//pauseProcessing = false;
}
