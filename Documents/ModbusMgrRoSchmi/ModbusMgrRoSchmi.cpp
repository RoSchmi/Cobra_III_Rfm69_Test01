// Copyright RoSchmi 2017
// ModbusRtu is a protocol for crc secured transmission between a Modbus Master and a Modbus Client
// This library "ModbusMgr_01.cpp" works together with Reinhard Ostermeier's Modbus Library ModbusRtu.cpp
// for the Arduino like GHI FEZ Medusa Mini Mainboard
// -https://www.ghielectronics.com/community/codeshare/entry/865
// -https://www.ghielectronics.com/community/codeshare/entry/880
// The Arduino like mainboard serves as a Modbus Client.
// As a Modbus Master you can use for tests the WindowsForms App >ModbusWindowsForm<
// The Modbus Client holds a set of so called "Coils". A coil stands for a one bit infomation (on or off).
// In this special example exactly 8 coils can be addressed. They are represented by the 8 bits of one byte (datatype unsigned char)
// Besides the coils the Modbus Client holds a set of so called "HoldingRegisters".
// Each "HoldingRegister" is a 16 bite wide data word, which can be read and set by the Modubus Client as well
// as by the Modbus Master. 
// When the client receives a command from the Modbus master,
// the event routines  >OnMessageReceived<, >CoilChanged< and >HoldingRegistersChanged< are triggered
// In these routines some actions can be performed to react on the state of a certain coil.
// In this example the LED of the button module is switched on or off.
// There are some additional methods implemented, which can be called from the Arduino Loop{}:
// "ReadSingleCoil"
// "WriteSingleCoil"
// "ReadAllCoils"
// "SetHoldingRegisters"
// "ReadHoldingRegister"
// "ReadMaxHoldingRegisterCount"
// The holdingRegisters can be use for example to hold actual
// sensor readings of the client, which so can periodically be fetched by the master.
// additionally the HoldingRegisters can be used to realize complex command and acknowledge mechanisms

// To integrate the ModbusMgr_01 library into an  Arduino Application after the declaration 
// the ModbusMgr_01::Init method is called in Arduino Setup{}, the ModbusMgr_01::Loop is called
// from the Arduino Loop{}.

#define MODBUS

#include <Wire.h>
#include <SPI.h>

#include <avr/pgmspace.h>

#include "ModbusMgrRoSchmi.h"

#define ZONE_COUNT 10
#define MODBUS_BASE_ADDRESS 0x10

// Constructor
ModbusMgrRoSchmi::ModbusMgrRoSchmi()
{
}

ModbusMgrRoSchmi::~ModbusMgrRoSchmi()
{
}

void ModbusMgrRoSchmi::Init(unsigned char clientAddress, int baudRate)
{
     // instantiate a button module, the button LED is 
	 // turned on or off in the CoilChanged routine
     b = new button(3);

#ifdef MODBUS
	
	_modbus = new ModbusRtuRoSchmi(2, clientAddress, baudRate);
	
	_modbus->Coils = &_coils;
    _modbus->CoilCount = MAX_COIL_COUNT;
    _modbus->HoldingRegisters = _holdingRegisters;
	_modbus->HoldingRegisterCount = MAX_HOLDINGREG_COUNT;
	_modbus->Callback = this;
#endif
	_lastModbusMessage = millis();
	_masterMode = 0;
}
  
void ModbusMgrRoSchmi::Loop()
{
#ifdef MODBUS
	if (_masterMode == 0)
	{
		_modbus->Poll();
	}
#endif

	delay(10);
}

// Is called when a Message from the Modbus Master is received
void ModbusMgrRoSchmi::OnMessageReceived(unsigned char address, unsigned char functionCode)
{
     if (address >= MODBUS_BASE_ADDRESS && address < MODBUS_BASE_ADDRESS + ZONE_COUNT)
	{
		_lastModbusMessage = millis();
	}
}

// Is called when a >Write Coil< command was sent by the master
void ModbusMgrRoSchmi::CoilChanged(unsigned short address, unsigned short count)
{      
	   // Example;
	   // if coil No. 0 is set -> switch button led on
	   b->set_led(ReadSingleCoil(0) ? true : false);
}

// Is called when a >Write Register< command was sent by the master
void ModbusMgrRoSchmi::HoldingRegistersChanged(unsigned short address, unsigned short count)
{
}

// Can be called from the Main Loop to read a Single coil (bit) of the 8 coils (a byte)
// each bit (address 0x00 to 0x07) can be used to represent the state of a digitial input
// or can be set by the Modbus Master to control the state of an actuator
bool ModbusMgrRoSchmi::ReadSingleCoil(uint8_t address)
{
	uint8_t workAddress = address & 0x07;
    uint8_t mask = 0x01 << workAddress;
	return _coils & mask;

}
// Can be called from the Main Loop to write a single coil (bit) of the Coils byte (8 coils)
// (returns true if a valid coil was addressed, false if a not valid coil was addresse)
// These data can be fetched by the Modbus Master using the master's >Read Coil< command
// Alternatively the coils can be set by the Modbus Master to control the state of an actuator

bool ModbusMgrRoSchmi::WriteSingleCoil(uint8_t address, bool on_off_state)
{
	if ((address >= 0) && (address <= 7))  // only bit position 0 to 7 allowed
	{
	    if (on_off_state == true)
		{
			_coils = _coils | (0x01 << address);
			return true;
		}
		else
		{
			_coils = _coils & (~(0x01 << address));
			return true;
		}
	}
	else
	{
		return false;
	}
}

// Can be called from the Main Loop to read all 8 coils
// The states of the coils are represented as the 8 bits of the returned byte
unsigned char ModbusMgrRoSchmi::ReadAllCoils()
{
	return _coils;
}

// Can be called from the Main Loop to set or reset all 8 coils
// The states of the coils are represented as the 8 bits of a byte
void ModbusMgrRoSchmi::SetAllCoils(bool on_off_state)
{
		_coils = on_off_state ? 0xFF : 0x00;
}

// Can be called by the Main Loop to deposit Register data (16 bit) in registers
// These data can be fetched by the Modbus Master with the next >Read Registers<  command
void ModbusMgrRoSchmi::SetHoldingRegisters(unsigned short HoldingRegArray[], int RegCount)
{
	int count = (RegCount > MAX_HOLDINGREG_COUNT) ? MAX_HOLDINGREG_COUNT : RegCount;
	
    for (int i = 0; i < count; i++)
    {
	_holdingRegisters[i] = HoldingRegArray[i];
    }
     //_modbus->HoldingRegisters = _holdingRegisters;
	 //_modbus->HoldingRegisterCount = RegCount;
}

// Can be called by the Main Loop to read a register from the Holding Register Array
unsigned short ModbusMgrRoSchmi::ReadHoldingRegister(unsigned short offset)
{
	return _holdingRegisters[offset];
}

// Can be called by the Main Loop to get the maximal count of useable Holding Registers
unsigned short ModbusMgrRoSchmi::ReadMaxHoldingRegisterCount()
{
    return MAX_HOLDINGREG_COUNT;
}

