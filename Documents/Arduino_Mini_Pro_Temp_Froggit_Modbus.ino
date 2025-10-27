

// RoSchmi 04.08.2017
// Captures temperature and humidity data from Froggit (Ambient) Weather Station Transmitters F007TH and FT007TP
// This variant is for transfering the data via Modbus to another Mainboard
// There is another variant of this Sketch: Medusa_Temp_Froggit_Modbus_SerialMonitor
// which writes the captured data to the serial monitor 

// Adaption for Arduino Pro Mini 3,3 V

// Little modification (identifies the Random-Id and the battery state) with additionl comments of the Sketch
// Temperature
// von BaronVonSchnowzer
// http://forum.arduino.cc/index.php?topic=214436.30
// https://eclecticmusingsofachaoticmind.wordpress.com/2015/01/21/home-automation-temperature-sensors/

// RF Input connected to Gadgeteer Socket 1, Pin 3

/* 
 Based on Andrew's sketch for capturing data from Ambient F007th Thermo-Hygrometer less the uploading it to Xively via Wifi and with the addition of a checksum check.
 
 Inspired by the many weather station hackers who have gone before,
 Only possible thanks to the invaluable help and support on the Arduino forums.
 
 With particular thanks to
 
 Rob Ward (whose Manchester Encoding reading by delay rather than interrupt
 is the basis of this code)
 https://github.com/robwlakes/ArduinoWeatherOS
 
 The work of 3zero8 capturing and analysing the F007th data
 http://forum.arduino.cc/index.php?topic=214436.0
 
 The work of Volgy capturing and analysing the F007th data
 https://github.com/volgy/gr-ambient
 
 Marco Schwartz for showing how to send sensor data to websites
 http://www.openhomeautomation.net/
 
 The forum contributions of;
 dc42: showing how to create 5 minute samples.
 jremington: suggesting how to construct error checking using averages (although not used this is where the idea of using newtemp and newhum for error checking originated)
 Krodal: for his 6 lines of code in the forum thread "Compare two sensor values"
 
 This example code is in the public domain.
 
 Additional work by Ron Lewis on reverse engineering the checksum
 
 What this code does:
 Captures Ambient F007th Thermo-Hygrometer data packets by;
 Identifying a header of at least 10 rising edges (manchester encoding binary 1s)
 Synchs the data correctly within byte boundaries
 Distinguishes between F007th data packets and other 434Mhz signals with equivalent header by checking value of sensor ID byte 
 Correctly identifies positive and negative temperature values to 1 decimal place for up to 8 channels
 Correctly identifies humidity values for up to 8 channels
 Error checks data by rejecting;
 humidity value outside the range 1 to 100%
 bad checksums
 
 Hardware to use with this code
 6 F007th Thermo-Hygrometer set to different channels (can be adapted for between 1 and 8)
 A 434Mhz receiver
 17cm strand of CAT-5 cable as an antenna.
 CC3000 (I use the Adafruit breakout board with ceramic antenna)
 
 Code optimisation
 In order to improve reliability this code has been optimised to remove float values except when sending to Xively.
 This code does not provide any output to the serial monitor on what is happening,  see earlier versions for printouts.
 
 F007th Ambient Thermo-Hygrometer
 Sample Data:
 0        1        2        3        4        5        6        7
 FD       45       4F       04       4B       0B       52       0
 0   1    2   3    4   5    6   7    8   9    A   B    C   D    E
 11111101 01000101 01001111 00000100 01001011 00001011 01010010 0000
 hhhhhhhh SSSSSSSS NRRRRRRR bCCCTTTT TTTTTTTT HHHHHHHH CCCCCCCC ????
 
 Channel 1 F007th sensor displaying 21.1 Centigrade and 11% RH
 
 hhhhhhhh = header with final 01 before data packet starts (note using this sketch the header 01 is omitted when the binary is displayed)
 SSSSSSSS = sensor ID, F007th = Ox45
 NRRRRRRR = Rolling Code Byte? Resets each time the battery is changed
 b = battery indicator?
 CCC = Channel identifier, channels 1 to 8 can be selected on the F007th unit using dipswitches. Channel 1 => 000, Channel 2 => 001, Channel 3 => 010 etc.
 TTTT TTTTTTTT = 12 bit temperature data.
 To obtain F: convert binary to decimal, take away 400 and divide by 10 e.g. (using example above) 010001001011 => 1099
 (1099-400)/10= 69.9F
 To obtain C: convert binary to decimal, take away 720 and multiply by 0.0556 e.g.
 0.0556*(1099-720)= 21.1C
 HHHHHHHH = 8 bit humidity in binary. e.g. (using example above) 00001011 => 11
 CCCCCCCC = checksum? Note that this sketch only looks at the first 6 bytes and ignores the checksum
 
 */

// Libraries
#include <SPI.h>
#include <Wire.h>

  
#include <ModbusRtuArduinoRoSchmi.h>
#include <ModbusMgrArduinoRoSchmi.h>

//#include <ModbusRtuRoSchmi.h>
//#include <ModbusMgrRoSchmi.h>
//#include <Modules/Button.h>

// for an error message concerning FLASH.cpp
// https://www.ghielectronics.com/community/forum/topic?id=22268
// mcalsyn: I was able to repro this in 1.6.3. 
// It's a weird error, especially given that the previous 
// line does a similar computation, but there is a 
// brute-force fix. If you change 64 * 1024 in flash.h to 65536, 
// then the base project compiles correctly (under 1.6.3, my current version). 


//using namespace gadgeteering;
//using namespace gadgeteering::mainboards;
//using namespace gadgeteering::modules;

//fez_medusa_mini board;

//ModbusRtuRoSchmi*  _modbus;
ModbusRtuArduinoRoSchmi*  _modbus;
ModbusMgrArduinoRoSchmi _ModbusManager = ModbusMgrArduinoRoSchmi();
//ModbusMgr_02 _ModbusManager = ModbusMgr_02();
//ModbusMgrRoSchmi _ModbusManager = ModbusMgrRoSchmi();

int ledPin = 13;
unsigned short holdingRegContent[40];
unsigned long lastSampleTime;
unsigned long preLastSampleTime;
int loopCtr = 0;
bool newSampleReceived = false;
typedef struct
{
  byte channel;
  byte sensorType;
  unsigned long sampleTime;
  unsigned short temp;
  unsigned short hum;
} sensorDataSet;

sensorDataSet DataSets[9];

#define MAX_BYTES 7
#define countof(x) (sizeof(x)/sizeof(x[0]))
// Interface Definitions
int RxPin           = 7;   //The number of signal from the Rx

// Variables for Manchester Receiver Logic:
word    sDelay     = 242;  //Small Delay about 1/4 of bit duration
word    lDelay     = 484;  //Long Delay about 1/2 of bit duration, 1/4 + 1/2 = 3/4
byte    polarity   = 1;    //0 for lo->hi==1 or 1 for hi->lo==1 for Polarity, sets tempBit at start
byte    tempBit    = 1;    //Reflects the required transition polarity
boolean firstZero  = false;//flags when the first '0' is found.
boolean noErrors   = true; //flags if signal does not follow Manchester conventions
//variables for Header detection
byte    headerBits = 10;   //The number of ones expected to make a valid header
byte    headerHits = 0;    //Counts the number of "1"s to determine a header
//Variables for Byte storage
boolean sync0In=true;      //Expecting sync0 to be inside byte boundaries, set to false for sync0 outside bytes
byte    dataByte   = 0xFF; //Accumulates the bit information
byte    nosBits    = 6;    //Counts to 8 bits within a dataByte
byte    maxBytes   = MAX_BYTES;    //Set the bytes collected after each header. NB if set too high, any end noise will cause an error
byte    nosBytes   = 0;    //Counter stays within 0 -> maxBytes
//Variables for multiple packets
byte    bank       = 0;    //Points to the array of 0 to 3 banks of results from up to 4 last data downloads 
byte    nosRepeats = 3;    //Number of times the header/data is fetched at least once or up to 4 times
//Banks for multiple packets if required (at least one will be needed)
byte  manchester[MAX_BYTES];   //Stores banks of manchester pattern decoded on the fly

// Variables to prepare recorded values for Ambient

byte stnId = 0;        //Identifies the channel number
int dataType = 0;      //Identifies the Ambient Thermo-Hygrometer code
byte randomId = 0;      //Identifies the Random-Id of the Sensor Device
byte batteryState = 0;  //Identifies the battery Low state
int Newtemp = 0;
int Newhum = 0;
int ChanTemp[9];    //make one extra so we can index 1 relative
int ChanHum[9];

void setup()
{
    Serial.begin(115200);
    //Serial.begin(9600, SERIAL_8E1);
    //Serial.begin(9600);
    Serial.println("Hello, Program started");
    pinMode(ledPin, OUTPUT);
    pinMode(RxPin, INPUT);
    _ModbusManager.Init(0x11, 9600);
    //_ModbusManager.Init(0x11, 19200);  // Baudrates higher than 19200 did not work
   
    eraseManchester();  //clear the array to different nos cause if all zeroes it might think that is a valid 3 packets ie all equal
}


// Main RF, to find header, then sync in with it and get a packet.

void loop()
{
    tempBit=polarity; //these begin the same for a packet
    noErrors=true;
    firstZero=false;
    headerHits=0;
    nosBits=6;
    nosBytes=0;
    
    while (noErrors && (nosBytes<maxBytes))
    {
        while(digitalRead(RxPin)!=tempBit)
        {
            //pause here until a transition is found
	      }//at Data transition, half way through bit pattern, this should be where RxPin==tempBit

	      delayMicroseconds(sDelay);//skip ahead to 3/4 of the bit pattern
	      // 3/4 the way through, if RxPin has changed it is definitely an error

	      if (digitalRead(RxPin)!=tempBit)
        {
          noErrors=false;//something has gone wrong, polarity has changed too early, ie always an error
	      }//exit and retry
	      else
        {         
	          delayMicroseconds(lDelay);
	          //now 1 quarter into the next bit pattern,
	          if(digitalRead(RxPin)==tempBit) //if RxPin has not swapped, then bitWaveform is swapping
        {
		    //If the header is done, then it means data change is occuring ie 1->0, or 0->1
		    //data transition detection must swap, so it loops for the opposite transitions
		    tempBit = tempBit^1;
	      }//end of detecting no transition at end of bit waveform, ie end of previous bit waveform same as start of next bitwaveform

	      //Now process the tempBit state and make data definite 0 or 1's, allow possibility of Pos or Neg Polarity
        //Serial.println("im in the fourth");
        //   delay(1000);
	      byte bitState = tempBit ^ polarity;//if polarity=1, invert the tempBit or if polarity=0, leave it alone.
	      if(bitState==1) //1 data could be header or packet
        {
		        if(!firstZero)
            {
		            headerHits++;
		            if (headerHits==headerBits)
                {
			             //Serial.print("H");
                }
            }
		        else
            {
		            add(bitState);//already seen first zero so add bit in
		        }
	      }//end of dealing with ones
	      else
        {   //bitState==0 could first error, first zero or packet
        	  // if it is header there must be no "zeroes" or errors
	          if(headerHits<headerBits)
            {
        	      //Still in header checking phase, more header hits required
	              noErrors=false;//landing here means header is corrupted, so it is probably an error
                //Serial.println("Header is corrupted");
        	  }//end of detecting a "zero" inside a header
            else
            {
                //Serial.println("Header is not corrupted");
	              //we have our header, chewed up any excess and here is a zero
        	      if (!firstZero) //if first zero, it has not been found previously
                {
                    firstZero=true;
                    add(bitState);//Add first zero to bytes 
                    //Serial.print("!");
                }//end of finding first zero
                else
                {
	                add(bitState);
        	      }//end of adding a zero bit
            }//end of dealing with a first zero
        }//end of dealing with zero's (in header, first or later zeroes)
        }//end of first error check
         //Serial.println("\r\nGot the bit pattern");
         //delay(1000);
    }//end of while noErrors=true and getting packet of bytes

    loopCtr++;
    if (loopCtr % 100 == 0)
    {
        _ModbusManager.Loop();       // Transfer to modbus every 100 cycles
        digitalWrite(ledPin, HIGH);
        delay(50);
        digitalWrite(ledPin, LOW);
        delay(50);
    }
} //end of mainloop

//Read the binary data from the bank and apply conversions where necessary to scale and format data

void add(byte bitData)
{
    dataByte=(dataByte<<1)|bitData;
    nosBits++;
    if (nosBits==8)
    {
	    nosBits=0;
	    manchester[nosBytes]=dataByte;
	    nosBytes++;
	    //Serial.print("B");
    }
    if(nosBytes==maxBytes)
    {
	    dataByte = 0xFF;
	    // Subroutines to extract data from Manchester encoding and error checking
   
      //Serial.println(manchester[1]); 
	    // Identify channels 1 to 8 by looking at 3 bits in byte 3
	    int stnId = ((manchester[3]&B01110000)/16)+1;
      //Serial.print("Channel: "); Serial.println(stnId);
      
	    // Identify sensor by looking for sensorID in byte 1 (F007th Ambient Thermo-Hygrometer = 0x45)
	     dataType = manchester[1];  
      //Serial.print("Sensor-ID: "); Serial.println(manchester[1]);
      
       // Identify sensor Random-Id (is randomly created at battery change)
      randomId = manchester[2];
      //Serial.print("Random-ID: "); Serial.println(manchester[2]);

      // Identify the low battery state
      batteryState = manchester[3]&B10000000;
      
	    // Gets raw temperature from bytes 3 and 4 (note this is neither C or F but a value from the sensor) 
	    Newtemp = (float((manchester[3]&B00000111)*256)+ manchester[4]);
      //Serial.print("Temperature: "); Serial.println(Newtemp); 
	    // Gets humidity data from byte 5 and uses the higher 8 bits for the device random-Id
     
      Newhum =(manchester [2]);
      Newhum = Newhum << 8;
      Newhum = Newhum + manchester[5];
     
	    //Serial.print("Humidity: "); Serial.println(manchester[5]);
        
      if ( Checksum (countof(manchester)-2, manchester+1) == manchester[MAX_BYTES-1])
      {
          //Serial.println("Checksum is correct");
            // Checks sensor is a F007th with a valid humidity reading equal or less than 100
            //if ((dataType == 0x45) && ((Newhum & B00001111) <= 100))  // for F007th     (Temp and Humidty)
            //if ((dataType == 0x46) && ((Newhum & B00001111) <= 100))    // for FT007TP  (Refrigerator)
            if (((dataType == 0x46)|| (dataType == 0x45)) && ((Newhum & B00001111) <= 100))    // for FT007TP and F007th
            {
              //Serial.println("dataType and humidity are correct");
		          ChanTemp[stnId] = Newtemp;     //Type of ChanTemp is int
	            ChanHum[stnId] = Newhum;       //Type of ChanHum is int

	          // print raw data
	          // char Buff[128];
	          // for (int i=0; i<maxBytes; i++)
	          // {
	          //  sprintf(&Buff[i*3], "%02X ",manchester[i]);
		        // }
 //To obtain F: convert binary to decimal, take away 400 and divide by 10 e.g. (using example above) 010001001011 => 1099
 //(1099-400)/10= 69.9F
 //To obtain C: convert binary to decimal, take away 720 and multiply by 0.0556 e.g.
 //0.0556*(1099-720)= 21.1C

           
		// 123456789 123456789 1234
                // Channel=1 F=123.1 H=12%<
	//sprintf(&Buff[maxBytes*3],"  Channel=%d F=%d.%d H=%d%%\n", 
  //                 stnId, 
  //         (Newtemp-400)/10, 
	//	    Newtemp-(Newtemp/10)*10, 
	//	    Newhum);
  //              Serial.println();
  //              Serial.print(Buff);
  //              Serial.println();
  //               
  //            Serial.println((Newtemp - 720)* 0.556);
              
    for (int i = 0; i < 6; i++)
    {
      digitalWrite(ledPin, HIGH);
      delay(50);
      digitalWrite(ledPin, LOW);
      delay(50);
    }
         
    preLastSampleTime = lastSampleTime & 0x7FFFFFFF;    // Mask highest bit, is used for battery state

    lastSampleTime = 0;
    if (batteryState != 0)
    {
      lastSampleTime = 0x80000000;
    }
    
    lastSampleTime = lastSampleTime + (millis() / 2);   // Type of lastSampleTime is unsigned long
    newSampleReceived = true;
    DataSets[stnId].channel = (byte)(stnId & 0x00FF);
    DataSets[stnId].sensorType = (byte)(dataType & 0x00FF);
    DataSets[stnId].sampleTime = lastSampleTime;
    DataSets[stnId].temp = Newtemp;
    DataSets[stnId].hum = Newhum;
    
    holdingRegContent[0 + (stnId -1) * 5] = (((unsigned short)DataSets[stnId].channel) << 8) + (unsigned short) DataSets[stnId].sensorType;
    holdingRegContent[1 + (stnId -1) * 5] = (unsigned short)(DataSets[stnId].sampleTime >> 16);
    holdingRegContent[2 + (stnId -1) * 5] = (unsigned short)(DataSets[stnId].sampleTime & 0x0000FFFF);
    holdingRegContent[3 + (stnId -1) * 5] = (unsigned short)DataSets[stnId].temp;
    holdingRegContent[4 + (stnId -1) * 5] = (unsigned short)DataSets[stnId].hum;

    // for tests: print the registers
    // char printBuffer[200];
    // sprintf(printBuffer, "%04X   %04X   %04X   %04X   %04X   %04X\n",
    //                      holdingRegContent[0 + (stnId -1) * 5],
    //                      holdingRegContent[1 + (stnId -1) * 5],
    //                      holdingRegContent[2 + (stnId -1) * 5],
    //                      holdingRegContent[3 + (stnId -1) * 5],
    //                      holdingRegContent[4 + (stnId -1) * 5]);        
    //  Serial.println(printBuffer);
    
     _ModbusManager.SetHoldingRegisters(holdingRegContent, 40);
   //_ModbusManager.Loop();                 
     }
	}
  else
  {
    //Serial.println("Checksum is not correct");
  }
  }
}

void eraseManchester()
{
    for( int j=0; j < 4; j++)
    { 
        manchester[j]=j;
    }
}

uint8_t Checksum(int length, uint8_t *buff)
{
    uint8_t mask = 0x7C;
    uint8_t checksum = 0x64;
    uint8_t data;
    int byteCnt;	

    for ( byteCnt=0; byteCnt < length; byteCnt++)
    {
    	int bitCnt;
	data = buff[byteCnt];
	
	for ( bitCnt= 7; bitCnt >= 0 ; bitCnt-- )
	{
            uint8_t bit;
			
            // Rotate mask right
	    bit = mask & 1;
	    mask =  (mask >> 1 ) | (mask << 7);
	    if ( bit )
	    {
		mask ^= 0x18;
	    }
			
	    // XOR mask into checksum if data bit is 1			
	    if ( data & 0x80 )
	    {
	    	checksum ^= mask;
            }
	    data <<= 1;	
	}
    }
    return checksum;
}


