#pragma once

#include "Defines.h"
#include <Modules\Button.h>
#include "ModbusRtuRoSchmi.h"

using namespace gadgeteering;
using namespace gadgeteering::modules;

#define MAX_HOLDINGREG_COUNT 80
#define MAX_COIL_COUNT 8

class ModbusMgrRoSchmi : IModbusRtuCallback
{
public:
	
private:
	
	ModbusRtuRoSchmi* _modbus;
	
	button* b; //(3)
	
    long _lastModbusMessage;
	char _masterMode;
	
    unsigned short _holdingRegisters[MAX_HOLDINGREG_COUNT];

	unsigned char _coils;

private:
	
public:
	ModbusMgrRoSchmi();
	~ModbusMgrRoSchmi();

	void Init(unsigned char clientAddress, int baudRate);
	void Loop();

    bool WriteSingleCoil(uint8_t address, bool on_off_state);
    void SetAllCoils(bool on_off_state);
	bool ReadSingleCoil(uint8_t address);
    unsigned char ReadAllCoils();

    void SetHoldingRegisters(unsigned short HoldingRegArray[], int RegCount);
	unsigned short ReadHoldingRegister(unsigned short offset);
	unsigned short ReadMaxHoldingRegisterCount();

public:
	//class IModbusRtuCallback
	void OnMessageReceived(unsigned char address, unsigned char functionCode);
	void HoldingRegistersChanged(unsigned short address, unsigned short count);
	void CoilChanged(unsigned short address, unsigned short count);
};

