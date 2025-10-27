#include <Wire.h>
#include <SPI.h>
#include "ModbusRtuRoSchmi.h"

// Modbus master/slave implementation of the modbus RTU protocol
// this implementation does not implement all function codes
// Author: Reinhard Ostermeier, 2014
// For more information about the modbus protocol see: http://www.modbus.org/specs.php


ModbusRtuRoSchmi::ModbusRtuRoSchmi(unsigned char socket_number, unsigned char deviceId, int baudRate)
{
	_masterMode = false;

	_deviceId = deviceId;
	
	_lastReceive = 0;
	_bufferPos = 0;
	_expectedLength = MODBUS_BUFFER_LENGTH;

	_expectedResponseCode = 0x00;
	_responseTimeout = 0;
	_responseLength = 0;

	Callback = 0;

	// coils
	Coils = 0;
	CoilCount = 0;

	// holding registers
	HoldingRegisters = 0;
	HoldingRegisterCount = 0;

	Serial.end();
	//Serial.begin(9600, SERIAL_8E1);
    Serial.begin(baudRate, SERIAL_8E1);
}


ModbusRtuRoSchmi::~ModbusRtuRoSchmi()
{
}

void ModbusRtuRoSchmi::Poll()
{
	if (Serial.available() > 0)
	{
		// read new data
		_bufferPos += Serial.readBytes(reinterpret_cast<char*>(_buffer + _bufferPos), min(_expectedLength - _bufferPos, Serial.available()));
		_lastReceive = millis();

		if (_bufferPos >= 2 && _expectedLength == MODBUS_BUFFER_LENGTH && _expectedResponseCode == 0)
		{
			// check expected length
			switch (_buffer[1])
			{
			case 0x01:
			case 0x03:
			case 0x04:
			case 0x05:
			case 0x06:
				_expectedLength = 8;
				break;

			case 0x10:
				// write multiple registers has the data length defined in byte 6
				if (_bufferPos >= 7)
				{
					_expectedLength = 7 + _buffer[6] + 2;
				}
			}
		}
		else if (_expectedResponseCode > 0 && _bufferPos >= 2)
		{
			// if error flag is set adjust expected length
			if ((_buffer[1] & 0x80) != 0)
			{
				_expectedLength = 5;
			}
		}

		if (_bufferPos >= _expectedLength)
		{
			// message received
			if (_expectedResponseCode > 0)
			{
				if (_expectedResponseCode == _buffer[1])
				{
					_responseLength = _bufferPos;
				}
			}
			else
			{
				OnMessageReceived(_bufferPos);
			}
			_lastReceive = 0;
			_bufferPos = 0;
			_expectedLength = MODBUS_BUFFER_LENGTH;
		}
		else if (_expectedLength == MODBUS_BUFFER_LENGTH && _bufferPos >= 4)
		{
			// message with undefined length -> check CRC
			if (CheckCrc(_bufferPos))
			{
				// message received
				if (_expectedResponseCode > 0)
				{
					if (_expectedResponseCode == _buffer[1])
					{
						_responseLength = _bufferPos;
					}
				}
				else
				{
					OnMessageReceived(_bufferPos);
				}
				_lastReceive = 0;
				_bufferPos = 0;
				_expectedLength = MODBUS_BUFFER_LENGTH;
			}
		}
	}
	else if (_bufferPos > 0 && millis() > _lastReceive + 1000)
	{
		// reset
		_bufferPos = 0;
		_expectedLength = MODBUS_BUFFER_LENGTH;
	}
}

void ModbusRtuRoSchmi::OnMessageReceived(unsigned int length)
{
	if (Callback != 0)
	{
		Callback->OnMessageReceived(_buffer[0], _buffer[1]);
	}

	bool broadcast = _buffer[0] == 0;
	if (broadcast || _buffer[0] == _deviceId)
	{
		switch (_buffer[1])
		{
		case 0x01:
			if (broadcast)
			{
				return;
			}
			OnReadCoils(length);
			break;

		case 0x03:
			if (broadcast)
			{
				return;
			}
			OnReadHoldingRegisters(length);
			break;

		/*case 0x04:
			if (broadcast)
			{
				return;
			}
			OnReadInputRegisters(length);
			break;*/

		case 0x05:
			OnWriteSingleCoil(length, broadcast);
			break;

		case 0x06:
			OnWriteSingleRegister(length, broadcast);
			break;

		case 0x10:
			OnWriteMultipleRegisters(length, broadcast);
			break;

		default:
			if (!broadcast)
			{
				SendErrorResponse(0x01);
			}
		}
	}
}

unsigned short ModbusRtuRoSchmi::ExtractUshort(int pos)
{
	return ((unsigned short)(_buffer[pos]) << 8) + _buffer[pos + 1];
}

void ModbusRtuRoSchmi::InsertUshort(int pos, unsigned short value)
{
	_buffer[pos] = (unsigned char)((value & 0xff00) >> 8);
	_buffer[pos + 1] = (unsigned char)(value & 0x00ff);
}

void ModbusRtuRoSchmi::OnReadCoils(unsigned int length)
{
	if (length < 8 || Coils == 0)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	unsigned short count = ExtractUshort(4);
	if (count < 1 || count > CoilCount)
	{
		SendErrorResponse(0x03);
		return;
	}
	if (address + count > CoilCount)
	{
		SendErrorResponse(0x02);
		return;
	}
	// calc byte count
	_buffer[2] = count / 8;
	if (count % 8 > 0)
	{
		_buffer[2] += 1;
	}
	// fill with zero
	for (int n = 0; n < _buffer[2]; ++n)
	{
		_buffer[3 + n] = 0;
	}
	// set bits
	for (int n = 0; n < count; ++n)
	{
		if ((Coils[(address + n) / 8] & (1 << ((address + n) % 8))) != 0)
		{
			_buffer[3 + n / 8] |= (1 << (n % 8));
		}
	}
	Send(3 + _buffer[2]);
}

void ModbusRtuRoSchmi::OnReadHoldingRegisters(unsigned int length)
{
	if (length < 8 || HoldingRegisters == 0)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	unsigned short count = ExtractUshort(4);

	if (count < 1 || count > HoldingRegisterCount)
	{
		SendErrorResponse(0x03);
		return;
	}
	if (address + count > HoldingRegisterCount)
	{
		SendErrorResponse(0x02);
		return;
	}

	_buffer[2] = (unsigned char)(count * 2);
	for (int n = 0; n < count; ++n)
	{
		InsertUshort(3 + n * 2, HoldingRegisters[address + n]);
	}
	Send(3 + 2 * count);
}

/*void ModbusRtuRoSchmi::OnReadInputRegisters(unsigned int length)
{
	if (length < 8)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	unsigned short count = ExtractUshort(4);
	if (count < 1 || count > MEAS_VALUE_COUNT * 2)
	{
		SendErrorResponse(0x03);
		return;
	}
	if (address + count > MEAS_VALUE_COUNT * 2)
	{
		SendErrorResponse(0x02);
		return;
	}

	_buffer[2] = (unsigned char)(count * 2);
	int pos = _currentMeasValue * 2 + address;
	for (int n = 0; n < count; ++n)
	{
		if (pos >= MEAS_VALUE_COUNT * 2)
		{
			pos = 0;
		}
		InsertUshort(3 + n * 2, (unsigned short)_measValues[pos++]);
	}
	Send(3 + 2 * count);
}*/

void ModbusRtuRoSchmi::OnWriteSingleCoil(unsigned int length, bool broadcast)
{
	if (length < 8 || Coils == 0)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	bool value = ExtractUshort(4) != 0;
	if (address >= CoilCount)
	{
		if (!broadcast) SendErrorResponse(0x02);
		return;
	}
	if (value)
	{
		Coils[address / 8] |= (1 << (address % 8));
	}
	else
	{
		Coils[address / 8] &= ~(1 << (address % 8));
	}
	// response is echo
	if (!broadcast) Send(6);

	if (Callback != 0)
	{
		Callback->CoilChanged(address, 1);
	}
}

void ModbusRtuRoSchmi::OnWriteSingleRegister(unsigned int length, bool broadcast)
{
	if (length < 8 || HoldingRegisters == 0)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	unsigned short value = ExtractUshort(4);
	if (address + 1 > HoldingRegisterCount)
	{
		if (!broadcast) SendErrorResponse(0x02);
		return;
	}
	HoldingRegisters[address] = value;
	// response is echo
	if (!broadcast) Send(6);

	if (Callback != 0)
	{
		Callback->HoldingRegistersChanged(address, 1);
	}
}

void ModbusRtuRoSchmi::OnWriteMultipleRegisters(unsigned int length, bool broadcast)
{
	if (length < 8 || HoldingRegisters == 0)
	{
		SendErrorResponse(0x01);
		return;
	}
	unsigned short address = ExtractUshort(2);
	unsigned short count = ExtractUshort(4);
	if (count < 1 || count > HoldingRegisterCount)
	{
		if (!broadcast) SendErrorResponse(0x03);
		return;
	}
	if (address + count > HoldingRegisterCount)
	{
		if (!broadcast) SendErrorResponse(0x02);
		return;
	}
	for (int n = 0; n < count; ++n)
	{
		HoldingRegisters[address + n] = ExtractUshort(7 + 2 * n);
	}
	// response is echo of address and count
	if (!broadcast) Send(6);

	if (Callback != 0)
	{
		Callback->HoldingRegistersChanged(address, count);
	}
}

void ModbusRtuRoSchmi::SendErrorResponse(unsigned char ec)
{
	_buffer[1] |= 0x80;
	_buffer[2] = ec;
	Send(3);
}

bool ModbusRtuRoSchmi::CheckCrc(int length)
{
	unsigned short crc = CalcCrc(length - 2);
	return (_buffer[length - 2] == (unsigned char)(crc & 0x00ff) &&
		_buffer[length - 1] == (unsigned char)((crc & 0xff00) >> 8));
}

void ModbusRtuRoSchmi::Send(unsigned int length)
{
	unsigned short crc = CalcCrc(length);
	_buffer[length++] = (unsigned char)(crc & 0x00ff);
	_buffer[length++] = (unsigned char)((crc & 0xff00) >> 8);
	Serial.write(_buffer, length);
}

unsigned short ModbusRtuRoSchmi::CalcCrc(unsigned int length)
{
	unsigned short crc = 0xffff;
	bool lsbHigh;
	for (int i = 0; i < length; i++)
	{
		crc = (unsigned short)(crc ^ _buffer[i]);
		for (int j = 0; j < 8; j++)
		{
			lsbHigh = (crc & 0x0001) != 0;
			crc = (unsigned short)((crc >> 1) & 0x7FFF);

			if (lsbHigh)
			{
				crc = (unsigned short)(crc ^ 0xa001);
			}
		}
	}
	return crc;
}


// master mode methods

void ModbusRtuRoSchmi::PrepareSend(unsigned char deviceAddress, unsigned short functionCode)
{
	_bufferPos = 0;
	_expectedLength = MODBUS_BUFFER_LENGTH;
	_responseLength = 0;

	_buffer[0] = deviceAddress;
	_buffer[1] = functionCode;
}

bool ModbusRtuRoSchmi::CheckResponse(char& error)
{
	if (_expectedResponseCode == 0x00 || millis() > _responseTimeout)
	{
		error = -1;
		return true;
	}
	if (_responseLength > 0)
	{
		if ((_buffer[1] & 0x80) != 0)
		{
			error = _buffer[2];
		}
		else
		{
			error = 0;
		}
		_expectedResponseCode = 0;
		_responseTimeout = 0;
		return true;
	}
	return false;
}

void ModbusRtuRoSchmi::ReadCoils(unsigned char deviceAddress, unsigned short startAddress, unsigned short coilCount, int timeout)
{
	PrepareSend(deviceAddress, 0x01);

	InsertUshort(2, startAddress);
	InsertUshort(4, coilCount);

	_expectedLength = 5 + coilCount / 8;
	_expectedResponseCode = 0x01;
	_responseTimeout = millis() + timeout;

	Send(6);
}

bool ModbusRtuRoSchmi::PoolReadCoils(char& error, unsigned char* coils)
{
	Poll();

	if (CheckResponse(error))
	{
		if (error == 0)
		{
			for (int n = 0; n < _buffer[2]; ++n)
			{
				coils[n] = _buffer[+n];
			}

			_responseLength = 0;
		}
		return true;
	}
	return false;
}


void ModbusRtuRoSchmi::WriteSingleCoil(unsigned char deviceAddress, unsigned short address, unsigned short value, int timeout)
{
	PrepareSend(deviceAddress, 0x05);

	InsertUshort(2, address);
	InsertUshort(4, value);

	_expectedLength = 8;
	_expectedResponseCode = 0x05;
	_responseTimeout = millis() + timeout;

	Send(6);
}

bool ModbusRtuRoSchmi::PollWriteSingleCoil(char& error)
{
	Poll();

	if (CheckResponse(error))
	{
		// nothing to parse
		return true;
	}
	return false;
}


