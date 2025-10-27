#pragma once

// Modbus master/slave implementation of the modbus RTU protocol
// this implementation does not implement all function codes
// Author: Reinhard Ostermeier, 2014
// For more information about the modbus protocol see: http://www.modbus.org/specs.php
 

#include "Defines.h"

#include <Gadgeteering.h>
#include <Modules/DisplayN18_osre.h>

using namespace gadgeteering;
using namespace gadgeteering::modules;

// accoring to the modbus spec this should be 256, but it's usually not needed
#define MODBUS_BUFFER_LENGTH 64

// implement IModbusRtuCallback to receive notifications for incoming messages on a modbus device.
// for a modbus master this interface has no effect.
class IModbusRtuCallback
{
public:
	// Is called when a valid modbus request was received, no matter what device was addressed
	// param address: Device address of the targeted device (0 for broadcast)
	// param functionCode: The function code of the message
	virtual void OnMessageReceived(unsigned char address, unsigned char functionCode);

	// Is called when a holding register was modified on this device
	// param address: Address of the 1st register
	// param count: number of written registers
	virtual void HoldingRegistersChanged(unsigned short address, unsigned short count);

	// Is called when a coil was modified on this device
	// param address: Address of the 1st coil
	// param count: number of coils
	virtual void CoilChanged(unsigned short address, unsigned short count);
};

// This is a combined implementation for modus devices and wasters
// it is not safe to use device and master functions at the same time
class ModbusRtuRoSchmi
{
private:
	bool _masterMode;
	unsigned char _deviceId;
	unsigned char _buffer[MODBUS_BUFFER_LENGTH];

	unsigned long _lastReceive;
	unsigned int _bufferPos;
	unsigned int _expectedLength;

	// master mode vars
	unsigned char _expectedResponseCode;
	long _responseTimeout;
	unsigned short _responseLength;

	void OnMessageReceived(unsigned int length);

	unsigned short CalcCrc(unsigned int length);

	bool CheckCrc(int length);
	
	void Send(unsigned int length);

	void SendErrorResponse(unsigned char ec);

	void DbgOutBuffer(unsigned int length, int y);

	unsigned short ExtractUshort(int pos);
	void InsertUshort(int pos, unsigned short value);

	void OnReadCoils(unsigned int length);
	void OnReadHoldingRegisters(unsigned int length);
	//void OnReadInputRegisters(unsigned int length);
	void OnWriteSingleCoil(unsigned int length, bool broadcast);
	void OnWriteSingleRegister(unsigned int length, bool broadcast);
	void OnWriteMultipleRegisters(unsigned int length, bool broadcast);

	// master mode methods
	void PrepareSend(unsigned char deviceAddress, unsigned short functionCode);
	bool CheckResponse(char& error);

public:
	// creates a new instance of ModbusRtu
	// param socket_number: number of the Gadgeteer socket (currently unused, see remarks)
	// param deviceId: id of this device (1..247). This id is used for device mode only
	// remarks:
	// The current implementation initials the primary Serial interface for modbus with the following parameters
	// 9600 Baud, 8 data bits, 1 stop bit, even parity
	// It is not possible to use the Serial connection for debugging!
	// Remove all Serial.print.. calls from you program.
	ModbusRtuRoSchmi(unsigned char socket_number, unsigned char deviceId, int baudRate);

	// destructor
	~ModbusRtuRoSchmi();

	// poll for incomming requests
	// call this method in an regular intevall for a modbus device
	// For a modbus master this method is called internally when a response is polled.
	void Poll();

	// pointer to a class that implements the IModbusRtuCallback interface.
	IModbusRtuCallback* Callback;

	// pointer to a byte array which holds the coils of the device
	// this has no effect for a modbus master
	unsigned char* Coils;
	
	// number of coils (bits).
	// this has no effect for a modbus master
	unsigned short CoilCount;

	// pointer to a ushort array which holds the holding registers of the device
	// this has no effect for a modbus master
	unsigned short* HoldingRegisters;

	// number of holding registers.
	// this has no effect for a modbus master
	unsigned short HoldingRegisterCount;


	// master mode methods

	// write a single coil to a device
	// param deviceAddress: address of the target device
	// param address: address of the coil
	// param value: new value for the coil. Use 0xff00 for high and 0x0000 for false.
	// param timeout: timeout in ms to wait for the response
	// remarks:
	// call PollWriteSingleCoil in an regular interval until it returns true before starting the next action
	void WriteSingleCoil(unsigned char deviceAddress, unsigned short address, unsigned short value, int timeout = 2000);

	// Poll for the WriteSingleCoil response
	// param error (out): error code of the response: -1=timeout, 0=ok, >0 modbus device error
	// return value: true if the response was received or timeout, else false
	bool PollWriteSingleCoil(char& error);

	// Read coils from an device
	// param deviceAddress: address of the target device
	// param startAddress: address of the 1st coil
	// param coilCount: number of coils to read
	// param timeout: timeout in ms to wait for the response
	// remarks:
	// call PoolReadCoils in an regular interval until it returns true before starting the next action
	void ReadCoils(unsigned char deviceAddress, unsigned short startAddress, unsigned short coilCount, int timeout = 2000);

	// Poll for the WriteSingleCoil response
	// param error (out): error code of the response: -1=timeout, 0=ok, >0 modbus device error
	// param coils (in): pointer to an array of bytes to store the coils. The buffer must be big enough to hold all coils (bits).
	// return value: true if the response was received or timeout, else false
	bool PoolReadCoils(char& error, unsigned char* coils);
};

