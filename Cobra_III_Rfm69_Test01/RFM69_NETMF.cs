/* RFM69 Driver for NETMF Mainboards
// NETMF 4.3, GHI SDK 2016 R1
// **********************************************************************************
// Copyright RoSchmi January 2017, Version 1.1
// 
// **********************************************************************************
// This is an adaption of Felix Rusu - felix@lowpowerlab.com RFM69 library for arduino 
// like mainboards to NETMF / Gadgeteer
// RFM69 library and code by Felix Rusu - felix@lowpowerlab.com
// Arduino libraries are at: https://github.com/LowPowerLab/
// Make sure you adjust the settings in the configuration section below !!!
// **********************************************************************************
// Copyright Felix Rusu, LowPowerLab.com
// Library and code by Felix Rusu - felix@lowpowerlab.com
// **********************************************************************************
// Parts of the code concerning "Automatic transmission control" (ATC)
// are Copyright Thomas Studwell (2014,2015) https://github.com/TomWS1/RFM69_ATC
// Adjustments are by Felix Rusu, LowPowerLab.com 
//
// License
// **********************************************************************************
// This program is free software; you can redistribute it 
// and/or modify it under the terms of the GNU General    
// Public License as published by the Free Software       
// Foundation; either version 3 of the License, or        
// (at your option) any later version.                    
//                                                        
// This program is distributed in the hope that it will   
// be useful, but WITHOUT ANY WARRANTY; without even the  
// implied warranty of MERCHANTABILITY or FITNESS FOR A   
// PARTICULAR PURPOSE. See the GNU General Public        
// License for more details.                              
//                                                        
// You should have received a copy of the GNU General    
// Public License along with this program.
// If not, see <http://www.gnu.org/licenses></http:>.
//                                                        
// Licence can be viewed at                               
// http://www.gnu.org/licenses/gpl-3.0.txt
//
// Please maintain this license information along with authorship
// and copyright notices in any redistribution of this code
// **********************************************************************************/


// Comment inactive "#define DebugPrint" when the debugger is not connected!!!, otherwise the program will eventually not work properly!
//#define DebugPrint

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using System.Text;

namespace RoSchmi.RFM69_NETMF
{
    public class RFM69_NETMF
    {
        #region RFM69 Registers and Definitions
        // Not all of this stuff is needed and can be commented out to save memory space

        #region RFM69 Registers
        const byte REG_FIFO = 0x00;
        const byte REG_OPMODE = 0x01;
        const byte REG_DATAMODUL = 0x02;
        const byte REG_BITRATEMSB = 0x03;
        const byte REG_BITRATELSB = 0x04;
        const byte REG_FDEVMSB = 0x05;
        const byte REG_FDEVLSB = 0x06;
        const byte REG_FRFMSB = 0x07;
        const byte REG_FRFMID = 0x08;
        const byte REG_FRFLSB = 0x09;
        const byte REG_OSC1 = 0x0A;
        const byte REG_AFCCTRL = 0x0B;
        const byte REG_LOWBAT = 0x0C;
        const byte REG_LISTEN1 = 0x0D;
        const byte REG_LISTEN2 = 0x0E;
        const byte REG_LISTEN3 = 0x0F;
        const byte REG_VERSION = 0x10;
        const byte REG_PALEVEL = 0x11;
        const byte REG_PARAMP = 0x12;
        const byte REG_OCP = 0x13;
        const byte REG_AGCREF = 0x14;  // not present on RFM69/SX1231
        const byte REG_AGCTHRESH1 = 0x15;  // not present on RFM69/SX1231
        const byte REG_AGCTHRESH2 = 0x16;  // not present on RFM69/SX1231
        const byte REG_AGCTHRESH3 = 0x17;  // not present on RFM69/SX1231
        const byte REG_LNA = 0x18;
        const byte REG_RXBW = 0x19;
        const byte REG_AFCBW = 0x1A;
        const byte REG_OOKPEAK = 0x1B;
        const byte REG_OOKAVG = 0x1C;
        const byte REG_OOKFIX = 0x1D;
        const byte REG_AFCFEI = 0x1E;
        const byte REG_AFCMSB = 0x1F;
        const byte REG_AFCLSB = 0x20;
        const byte REG_FEIMSB = 0x21;
        const byte REG_FEILSB = 0x22;
        const byte REG_RSSICONFIG = 0x23;
        const byte REG_RSSIVALUE = 0x24;
        const byte REG_DIOMAPPING1 = 0x25;
        
        const byte REG_DIOMAPPING2 = 0x26;
        const byte REG_IRQFLAGS1 = 0x27;
        const byte REG_IRQFLAGS2 = 0x28;
        const byte REG_RSSITHRESH = 0x29;
        const byte REG_RXTIMEOUT1 = 0x2A;
        const byte REG_RXTIMEOUT2 = 0x2B;
        const byte REG_PREAMBLEMSB = 0x2C;
        const byte REG_PREAMBLELSB = 0x2D;
        const byte REG_SYNCCONFIG = 0x2E;
        const byte REG_SYNCVALUE1 = 0x2F;
        const byte REG_SYNCVALUE2 = 0x30;
        const byte REG_SYNCVALUE3 = 0x31;
        const byte REG_SYNCVALUE4 = 0x32;
        const byte REG_SYNCVALUE5 = 0x33;
        const byte REG_SYNCVALUE6 = 0x34;
        const byte REG_SYNCVALUE7 = 0x35;
        const byte REG_SYNCVALUE8 = 0x36;
        const byte REG_PACKETCONFIG1 = 0x37;
        const byte REG_PAYLOADLENGTH = 0x38;
        const byte REG_NODEADRS = 0x39;
        const byte REG_BROADCASTADRS = 0x3A;
        const byte REG_AUTOMODES = 0x3B;
        const byte REG_FIFOTHRESH = 0x3C;
        const byte REG_PACKETCONFIG2 = 0x3D;
        const byte REG_AESKEY1 = 0x3E;
        const byte REG_AESKEY2 = 0x3F;
        const byte REG_AESKEY3 = 0x40;
        const byte REG_AESKEY4 = 0x41;
        const byte REG_AESKEY5 = 0x42;
        const byte REG_AESKEY6 = 0x43;
        const byte REG_AESKEY7 = 0x44;
        const byte REG_AESKEY8 = 0x45;
        const byte REG_AESKEY9 = 0x46;
        const byte REG_AESKEY10 = 0x47;
        const byte REG_AESKEY11 = 0x48;
        const byte REG_AESKEY12 = 0x49;
        const byte REG_AESKEY13 = 0x4A;
        const byte REG_AESKEY14 = 0x4B;
        const byte REG_AESKEY15 = 0x4C;
        const byte REG_AESKEY16 = 0x4D;
        const byte REG_TEMP1 = 0x4E;
        const byte REG_TEMP2 = 0x4F;
        const byte REG_TESTLNA = 0x58;
        const byte REG_TESTPA1 = 0x5A;  // only present on RFM69HW/SX1231H
        const byte REG_TESTPA2 = 0x5C;  // only present on RFM69HW/SX1231H
        const byte REG_TESTDAGC = 0x6F;
        #endregion
        //******************************************************;
        // RF69/SX1231 bit control definition
        //******************************************************

        // RegOpMode
        const byte RF_OPMODE_SEQUENCER_OFF = 0x80;
        const byte RF_OPMODE_SEQUENCER_ON = 0x00;  // Default

        const byte RF_OPMODE_LISTEN_ON = 0x40;
        const byte RF_OPMODE_LISTEN_OFF = 0x00;  // Default

        const byte RF_OPMODE_LISTENABORT = 0x20;

        const byte RF_OPMODE_SLEEP = 0x00;
        const byte RF_OPMODE_STANDBY = 0x04;  // Default
        const byte RF_OPMODE_SYNTHESIZER = 0x08;
        const byte RF_OPMODE_TRANSMITTER = 0x0C;
        const byte RF_OPMODE_RECEIVER = 0x10;


        // RegDataModul
        const byte RF_DATAMODUL_DATAMODE_PACKET = 0x00;  // Default
        const byte RF_DATAMODUL_DATAMODE_CONTINUOUS = 0x40;
        const byte RF_DATAMODUL_DATAMODE_CONTINUOUSNOBSYNC = 0x60;

        const byte RF_DATAMODUL_MODULATIONTYPE_FSK = 0x00;  // Default
        const byte RF_DATAMODUL_MODULATIONTYPE_OOK = 0x08;

        const byte RF_DATAMODUL_MODULATIONSHAPING_00 = 0x00;  // Default
        const byte RF_DATAMODUL_MODULATIONSHAPING_01 = 0x01;
        const byte RF_DATAMODUL_MODULATIONSHAPING_10 = 0x02;
        const byte RF_DATAMODUL_MODULATIONSHAPING_11 = 0x03;


        // RegBitRate (bits/sec) example bit rates
        const byte RF_BITRATEMSB_1200 = 0x68;
        const byte RF_BITRATELSB_1200 = 0x2B;
        const byte RF_BITRATEMSB_2400 = 0x34;
        const byte RF_BITRATELSB_2400 = 0x15;
        const byte RF_BITRATEMSB_4800 = 0x1A;  // Default
        const byte RF_BITRATELSB_4800 = 0x0B;  // Default
        const byte RF_BITRATEMSB_9600 = 0x0D;
        const byte RF_BITRATELSB_9600 = 0x05;
        const byte RF_BITRATEMSB_19200 = 0x06;
        const byte RF_BITRATELSB_19200 = 0x83;
        const byte RF_BITRATEMSB_38400 = 0x03;
        const byte RF_BITRATELSB_38400 = 0x41;

        const byte RF_BITRATEMSB_38323 = 0x03;
        const byte RF_BITRATELSB_38323 = 0x43;

        const byte RF_BITRATEMSB_34482 = 0x03;
        const byte RF_BITRATELSB_34482 = 0xA0;

        const byte RF_BITRATEMSB_76800 = 0x01;
        const byte RF_BITRATELSB_76800 = 0xA1;
        const byte RF_BITRATEMSB_153600 = 0x00;
        const byte RF_BITRATELSB_153600 = 0xD0;
        const byte RF_BITRATEMSB_57600 = 0x02;
        const byte RF_BITRATELSB_57600 = 0x2C;
        const byte RF_BITRATEMSB_115200 = 0x01;
        const byte RF_BITRATELSB_115200 = 0x16;
        const byte RF_BITRATEMSB_12500 = 0x0A;
        const byte RF_BITRATELSB_12500 = 0x00;
        const byte RF_BITRATEMSB_25000 = 0x05;
        const byte RF_BITRATELSB_25000 = 0x00;
        const byte RF_BITRATEMSB_50000 = 0x02;
        const byte RF_BITRATELSB_50000 = 0x80;
        const byte RF_BITRATEMSB_100000 = 0x01;
        const byte RF_BITRATELSB_100000 = 0x40;
        const byte RF_BITRATEMSB_150000 = 0x00;
        const byte RF_BITRATELSB_150000 = 0xD5;
        const byte RF_BITRATEMSB_200000 = 0x00;
        const byte RF_BITRATELSB_200000 = 0xA0;
        const byte RF_BITRATEMSB_250000 = 0x00;
        const byte RF_BITRATELSB_250000 = 0x80;
        const byte RF_BITRATEMSB_300000 = 0x00;
        const byte RF_BITRATELSB_300000 = 0x6B;
        const byte RF_BITRATEMSB_32768 = 0x03;
        const byte RF_BITRATELSB_32768 = 0xD1;
        // custom bit rates
        const byte RF_BITRATEMSB_55555 = 0x02;
        const byte RF_BITRATELSB_55555 = 0x40;
        const byte RF_BITRATEMSB_200KBPS = 0x00;
        const byte RF_BITRATELSB_200KBPS = 0xa0;


        // RegFdev - frequency deviation (Hz)
        const byte RF_FDEVMSB_2000 = 0x00;
        const byte RF_FDEVLSB_2000 = 0x21;
        const byte RF_FDEVMSB_5000 = 0x00;  // Default;
        const byte RF_FDEVLSB_5000 = 0x52;  // Default
        const byte RF_FDEVMSB_7500 = 0x00;
        const byte RF_FDEVLSB_7500 = 0x7B;
        const byte RF_FDEVMSB_10000 = 0x00;
        const byte RF_FDEVLSB_10000 = 0xA4;
        const byte RF_FDEVMSB_15000 = 0x00;
        const byte RF_FDEVLSB_15000 = 0xF6;
        const byte RF_FDEVMSB_20000 = 0x01;
        const byte RF_FDEVLSB_20000 = 0x48;
        const byte RF_FDEVMSB_25000 = 0x01;
        const byte RF_FDEVLSB_25000 = 0x9A;
        const byte RF_FDEVMSB_30000 = 0x01;
        const byte RF_FDEVLSB_30000 = 0xEC;
        const byte RF_FDEVMSB_35000 = 0x02;
        const byte RF_FDEVLSB_35000 = 0x3D;
        const byte RF_FDEVMSB_40000 = 0x02;
        const byte RF_FDEVLSB_40000 = 0x8F;
        const byte RF_FDEVMSB_45000 = 0x02;
        const byte RF_FDEVLSB_45000 = 0xE1;
        const byte RF_FDEVMSB_50000 = 0x03;
        const byte RF_FDEVLSB_50000 = 0x33;
        const byte RF_FDEVMSB_55000 = 0x03;
        const byte RF_FDEVLSB_55000 = 0x85;
        const byte RF_FDEVMSB_60000 = 0x03;
        const byte RF_FDEVLSB_60000 = 0xD7;
        const byte RF_FDEVMSB_65000 = 0x04;
        const byte RF_FDEVLSB_65000 = 0x29;
        const byte RF_FDEVMSB_70000 = 0x04;
        const byte RF_FDEVLSB_70000 = 0x7B;
        const byte RF_FDEVMSB_75000 = 0x04;
        const byte RF_FDEVLSB_75000 = 0xCD;
        const byte RF_FDEVMSB_80000 = 0x05;
        const byte RF_FDEVLSB_80000 = 0x1F;
        const byte RF_FDEVMSB_85000 = 0x05;
        const byte RF_FDEVLSB_85000 = 0x71;
        const byte RF_FDEVMSB_90000 = 0x05;
        const byte RF_FDEVLSB_90000 = 0xC3;
        const byte RF_FDEVMSB_95000 = 0x06;
        const byte RF_FDEVLSB_95000 = 0x14;
        const byte RF_FDEVMSB_100000 = 0x06;
        const byte RF_FDEVLSB_100000 = 0x66;
        const byte RF_FDEVMSB_110000 = 0x07;
        const byte RF_FDEVLSB_110000 = 0x0A;
        const byte RF_FDEVMSB_120000 = 0x07;
        const byte RF_FDEVLSB_120000 = 0xAE;
        const byte RF_FDEVMSB_130000 = 0x08;
        const byte RF_FDEVLSB_130000 = 0x52;
        const byte RF_FDEVMSB_140000 = 0x08;
        const byte RF_FDEVLSB_140000 = 0xF6;
        const byte RF_FDEVMSB_150000 = 0x09;
        const byte RF_FDEVLSB_150000 = 0x9A;
        const byte RF_FDEVMSB_160000 = 0x0A;
        const byte RF_FDEVLSB_160000 = 0x3D;
        const byte RF_FDEVMSB_170000 = 0x0A;
        const byte RF_FDEVLSB_170000 = 0xE1;
        const byte RF_FDEVMSB_180000 = 0x0B;
        const byte RF_FDEVLSB_180000 = 0x85;
        const byte RF_FDEVMSB_190000 = 0x0C;
        const byte RF_FDEVLSB_190000 = 0x29;
        const byte RF_FDEVMSB_200000 = 0x0C;
        const byte RF_FDEVLSB_200000 = 0xCD;
        const byte RF_FDEVMSB_210000 = 0x0D;
        const byte RF_FDEVLSB_210000 = 0x71;
        const byte RF_FDEVMSB_220000 = 0x0E;
        const byte RF_FDEVLSB_220000 = 0x14;
        const byte RF_FDEVMSB_230000 = 0x0E;
        const byte RF_FDEVLSB_230000 = 0xB8;
        const byte RF_FDEVMSB_240000 = 0x0F;
        const byte RF_FDEVLSB_240000 = 0x5C;
        const byte RF_FDEVMSB_250000 = 0x10;
        const byte RF_FDEVLSB_250000 = 0x00;
        const byte RF_FDEVMSB_260000 = 0x10;
        const byte RF_FDEVLSB_260000 = 0xA4;
        const byte RF_FDEVMSB_270000 = 0x11;
        const byte RF_FDEVLSB_270000 = 0x48;
        const byte RF_FDEVMSB_280000 = 0x11;
        const byte RF_FDEVLSB_280000 = 0xEC;
        const byte RF_FDEVMSB_290000 = 0x12;
        const byte RF_FDEVLSB_290000 = 0x8F;
        const byte RF_FDEVMSB_300000 = 0x13;
        const byte RF_FDEVLSB_300000 = 0x33;


        // RegFrf (MHz) - carrier frequency
        // 315Mhz band
        const byte RF_FRFMSB_314 = 0x4E;
        const byte RF_FRFMID_314 = 0x80;
        const byte RF_FRFLSB_314 = 0x00;
        const byte RF_FRFMSB_315 = 0x4E;
        const byte RF_FRFMID_315 = 0xC0;
        const byte RF_FRFLSB_315 = 0x00;
        const byte RF_FRFMSB_316 = 0x4F;
        const byte RF_FRFMID_316 = 0x00;
        const byte RF_FRFLSB_316 = 0x00;
        // 433mhz band;
        const byte RF_FRFMSB_433 = 0x6C;
        const byte RF_FRFMID_433 = 0x40;
        const byte RF_FRFLSB_433 = 0x00;
        const byte RF_FRFMSB_434 = 0x6C;
        const byte RF_FRFMID_434 = 0x80;
        const byte RF_FRFLSB_434 = 0x00;
        const byte RF_FRFMSB_435 = 0x6C;
        const byte RF_FRFMID_435 = 0xC0;
        const byte RF_FRFLSB_435 = 0x00;
        // 868Mhz band;
        const byte RF_FRFMSB_863 = 0xD7;
        const byte RF_FRFMID_863 = 0xC0;
        const byte RF_FRFLSB_863 = 0x00;
        const byte RF_FRFMSB_864 = 0xD8;
        const byte RF_FRFMID_864 = 0x00;
        const byte RF_FRFLSB_864 = 0x00;
        const byte RF_FRFMSB_865 = 0xD8;
        const byte RF_FRFMID_865 = 0x40;
        const byte RF_FRFLSB_865 = 0x00;
        const byte RF_FRFMSB_866 = 0xD8;
        const byte RF_FRFMID_866 = 0x80;
        const byte RF_FRFLSB_866 = 0x00;
        const byte RF_FRFMSB_867 = 0xD8;
        const byte RF_FRFMID_867 = 0xC0;
        const byte RF_FRFLSB_867 = 0x00;
        const byte RF_FRFMSB_868 = 0xD9;
        const byte RF_FRFMID_868 = 0x00;
        const byte RF_FRFLSB_868 = 0x00;
        const byte RF_FRFMSB_869 = 0xD9;
        const byte RF_FRFMID_869 = 0x40;
        const byte RF_FRFLSB_869 = 0x00;
        const byte RF_FRFMSB_870 = 0xD9;
        const byte RF_FRFMID_870 = 0x80;
        const byte RF_FRFLSB_870 = 0x00;
        // 915Mhz band;
        const byte RF_FRFMSB_902 = 0xE1;
        const byte RF_FRFMID_902 = 0x80;
        const byte RF_FRFLSB_902 = 0x00;
        const byte RF_FRFMSB_903 = 0xE1;
        const byte RF_FRFMID_903 = 0xC0;
        const byte RF_FRFLSB_903 = 0x00;
        const byte RF_FRFMSB_904 = 0xE2;
        const byte RF_FRFMID_904 = 0x00;
        const byte RF_FRFLSB_904 = 0x00;
        const byte RF_FRFMSB_905 = 0xE2;
        const byte RF_FRFMID_905 = 0x40;
        const byte RF_FRFLSB_905 = 0x00;
        const byte RF_FRFMSB_906 = 0xE2;
        const byte RF_FRFMID_906 = 0x80;
        const byte RF_FRFLSB_906 = 0x00;
        const byte RF_FRFMSB_907 = 0xE2;
        const byte RF_FRFMID_907 = 0xC0;
        const byte RF_FRFLSB_907 = 0x00;
        const byte RF_FRFMSB_908 = 0xE3;
        const byte RF_FRFMID_908 = 0x00;
        const byte RF_FRFLSB_908 = 0x00;
        const byte RF_FRFMSB_909 = 0xE3;
        const byte RF_FRFMID_909 = 0x40;
        const byte RF_FRFLSB_909 = 0x00;
        const byte RF_FRFMSB_910 = 0xE3;
        const byte RF_FRFMID_910 = 0x80;
        const byte RF_FRFLSB_910 = 0x00;
        const byte RF_FRFMSB_911 = 0xE3;
        const byte RF_FRFMID_911 = 0xC0;
        const byte RF_FRFLSB_911 = 0x00;
        const byte RF_FRFMSB_912 = 0xE4;
        const byte RF_FRFMID_912 = 0x00;
        const byte RF_FRFLSB_912 = 0x00;
        const byte RF_FRFMSB_913 = 0xE4;
        const byte RF_FRFMID_913 = 0x40;
        const byte RF_FRFLSB_913 = 0x00;
        const byte RF_FRFMSB_914 = 0xE4;
        const byte RF_FRFMID_914 = 0x80;
        const byte RF_FRFLSB_914 = 0x00;
        const byte RF_FRFMSB_915 = 0xE4;  // Default
        const byte RF_FRFMID_915 = 0xC0;  // Default
        const byte RF_FRFLSB_915 = 0x00;  // Default
        const byte RF_FRFMSB_916 = 0xE5;
        const byte RF_FRFMID_916 = 0x00;
        const byte RF_FRFLSB_916 = 0x00;
        const byte RF_FRFMSB_917 = 0xE5;
        const byte RF_FRFMID_917 = 0x40;
        const byte RF_FRFLSB_917 = 0x00;
        const byte RF_FRFMSB_918 = 0xE5;
        const byte RF_FRFMID_918 = 0x80;
        const byte RF_FRFLSB_918 = 0x00;
        const byte RF_FRFMSB_919 = 0xE5;
        const byte RF_FRFMID_919 = 0xC0;
        const byte RF_FRFLSB_919 = 0x00;
        const byte RF_FRFMSB_920 = 0xE6;
        const byte RF_FRFMID_920 = 0x00;
        const byte RF_FRFLSB_920 = 0x00;
        const byte RF_FRFMSB_921 = 0xE6;
        const byte RF_FRFMID_921 = 0x40;
        const byte RF_FRFLSB_921 = 0x00;
        const byte RF_FRFMSB_922 = 0xE6;
        const byte RF_FRFMID_922 = 0x80;
        const byte RF_FRFLSB_922 = 0x00;
        const byte RF_FRFMSB_923 = 0xE6;
        const byte RF_FRFMID_923 = 0xC0;
        const byte RF_FRFLSB_923 = 0x00;
        const byte RF_FRFMSB_924 = 0xE7;
        const byte RF_FRFMID_924 = 0x00;
        const byte RF_FRFLSB_924 = 0x00;
        const byte RF_FRFMSB_925 = 0xE7;
        const byte RF_FRFMID_925 = 0x40;
        const byte RF_FRFLSB_925 = 0x00;
        const byte RF_FRFMSB_926 = 0xE7;
        const byte RF_FRFMID_926 = 0x80;
        const byte RF_FRFLSB_926 = 0x00;
        const byte RF_FRFMSB_927 = 0xE7;
        const byte RF_FRFMID_927 = 0xC0;
        const byte RF_FRFLSB_927 = 0x00;
        const byte RF_FRFMSB_928 = 0xE8;
        const byte RF_FRFMID_928 = 0x00;
        const byte RF_FRFLSB_928 = 0x00;


        // RegOsc1
        const byte RF_OSC1_RCCAL_START = 0x80;
        const byte RF_OSC1_RCCAL_DONE = 0x40;


        // RegAfcCtrl
        const byte RF_AFCCTRL_LOWBETA_OFF = 0x00;  // Default
        const byte RF_AFCCTRL_LOWBETA_ON = 0x20;


        // RegLowBat
        const byte RF_LOWBAT_MONITOR = 0x10;
        const byte RF_LOWBAT_ON = 0x08;
        const byte RF_LOWBAT_OFF = 0x00;  // Default

        const byte RF_LOWBAT_TRIM_1695 = 0x00;
        const byte RF_LOWBAT_TRIM_1764 = 0x01;
        const byte RF_LOWBAT_TRIM_1835 = 0x02;  // Default
        const byte RF_LOWBAT_TRIM_1905 = 0x03;
        const byte RF_LOWBAT_TRIM_1976 = 0x04;
        const byte RF_LOWBAT_TRIM_2045 = 0x05;
        const byte RF_LOWBAT_TRIM_2116 = 0x06;
        const byte RF_LOWBAT_TRIM_2185 = 0x07;


        // RegListen1
        const byte RF_LISTEN1_RESOL_64 = 0x50;
        const byte RF_LISTEN1_RESOL_4100 = 0xA0;  // Default
        const byte RF_LISTEN1_RESOL_262000 = 0xF0;

        const byte RF_LISTEN1_RESOL_IDLE_64 = 0x40;
        const byte RF_LISTEN1_RESOL_IDLE_4100 = 0x80;  // Default
        const byte RF_LISTEN1_RESOL_IDLE_262000 = 0xC0;

        const byte RF_LISTEN1_RESOL_RX_64 = 0x10;
        const byte RF_LISTEN1_RESOL_RX_4100 = 0x20;  // Default
        const byte RF_LISTEN1_RESOL_RX_262000 = 0x30;

        const byte RF_LISTEN1_CRITERIA_RSSI = 0x00;  // Default
        const byte RF_LISTEN1_CRITERIA_RSSIANDSYNC = 0x08;

        const byte RF_LISTEN1_END_00 = 0x00;
        const byte RF_LISTEN1_END_01 = 0x02;  // Default
        const byte RF_LISTEN1_END_10 = 0x04;


        // RegListen2
        const byte RF_LISTEN2_COEFIDLE_VALUE = 0xF5; // Default


        // RegListen3
        const byte RF_LISTEN3_COEFRX_VALUE = 0x20; // Default

        // RegVersion
        const byte RF_VERSION_VER = 0x24;  // Default


        // RegPaLevel
        const byte RF_PALEVEL_PA0_ON = 0x80;  // Default
        const byte RF_PALEVEL_PA0_OFF = 0x00;
        const byte RF_PALEVEL_PA1_ON = 0x40;
        const byte RF_PALEVEL_PA1_OFF = 0x00;  // Default
        const byte RF_PALEVEL_PA2_ON = 0x20;
        const byte RF_PALEVEL_PA2_OFF = 0x00;  // Default

        const byte RF_PALEVEL_OUTPUTPOWER_00000 = 0x00;
        const byte RF_PALEVEL_OUTPUTPOWER_00001 = 0x01;
        const byte RF_PALEVEL_OUTPUTPOWER_00010 = 0x02;
        const byte RF_PALEVEL_OUTPUTPOWER_00011 = 0x03;
        const byte RF_PALEVEL_OUTPUTPOWER_00100 = 0x04;
        const byte RF_PALEVEL_OUTPUTPOWER_00101 = 0x05;
        const byte RF_PALEVEL_OUTPUTPOWER_00110 = 0x06;
        const byte RF_PALEVEL_OUTPUTPOWER_00111 = 0x07;
        const byte RF_PALEVEL_OUTPUTPOWER_01000 = 0x08;
        const byte RF_PALEVEL_OUTPUTPOWER_01001 = 0x09;
        const byte RF_PALEVEL_OUTPUTPOWER_01010 = 0x0A;
        const byte RF_PALEVEL_OUTPUTPOWER_01011 = 0x0B;
        const byte RF_PALEVEL_OUTPUTPOWER_01100 = 0x0C;
        const byte RF_PALEVEL_OUTPUTPOWER_01101 = 0x0D;
        const byte RF_PALEVEL_OUTPUTPOWER_01110 = 0x0E;
        const byte RF_PALEVEL_OUTPUTPOWER_01111 = 0x0F;
        const byte RF_PALEVEL_OUTPUTPOWER_10000 = 0x10;
        const byte RF_PALEVEL_OUTPUTPOWER_10001 = 0x11;
        const byte RF_PALEVEL_OUTPUTPOWER_10010 = 0x12;
        const byte RF_PALEVEL_OUTPUTPOWER_10011 = 0x13;
        const byte RF_PALEVEL_OUTPUTPOWER_10100 = 0x14;
        const byte RF_PALEVEL_OUTPUTPOWER_10101 = 0x15;
        const byte RF_PALEVEL_OUTPUTPOWER_10110 = 0x16;
        const byte RF_PALEVEL_OUTPUTPOWER_10111 = 0x17;
        const byte RF_PALEVEL_OUTPUTPOWER_11000 = 0x18;
        const byte RF_PALEVEL_OUTPUTPOWER_11001 = 0x19;
        const byte RF_PALEVEL_OUTPUTPOWER_11010 = 0x1A;
        const byte RF_PALEVEL_OUTPUTPOWER_11011 = 0x1B;
        const byte RF_PALEVEL_OUTPUTPOWER_11100 = 0x1C;
        const byte RF_PALEVEL_OUTPUTPOWER_11101 = 0x1D;
        const byte RF_PALEVEL_OUTPUTPOWER_11110 = 0x1E;
        const byte RF_PALEVEL_OUTPUTPOWER_11111 = 0x1F;  // Default


        // RegPaRamp
        const byte RF_PARAMP_3400 = 0x00;
        const byte RF_PARAMP_2000 = 0x01;
        const byte RF_PARAMP_1000 = 0x02;
        const byte RF_PARAMP_500 = 0x03;
        const byte RF_PARAMP_250 = 0x04;
        const byte RF_PARAMP_125 = 0x05;
        const byte RF_PARAMP_100 = 0x06;
        const byte RF_PARAMP_62 = 0x07;
        const byte RF_PARAMP_50 = 0x08;
        const byte RF_PARAMP_40 = 0x09;  // Default
        const byte RF_PARAMP_31 = 0x0A;
        const byte RF_PARAMP_25 = 0x0B;
        const byte RF_PARAMP_20 = 0x0C;
        const byte RF_PARAMP_15 = 0x0D;
        const byte RF_PARAMP_12 = 0x0E;
        const byte RF_PARAMP_10 = 0x0F;


        // RegOcp
        const byte RF_OCP_OFF = 0x0F;
        const byte RF_OCP_ON = 0x1A;  // Default

        const byte RF_OCP_TRIM_45 = 0x00;
        const byte RF_OCP_TRIM_50 = 0x01;
        const byte RF_OCP_TRIM_55 = 0x02;
        const byte RF_OCP_TRIM_60 = 0x03;
        const byte RF_OCP_TRIM_65 = 0x04;
        const byte RF_OCP_TRIM_70 = 0x05;
        const byte RF_OCP_TRIM_75 = 0x06;
        const byte RF_OCP_TRIM_80 = 0x07;
        const byte RF_OCP_TRIM_85 = 0x08;
        const byte RF_OCP_TRIM_90 = 0x09;
        const byte RF_OCP_TRIM_95 = 0x0A;  // Default
        const byte RF_OCP_TRIM_100 = 0x0B;
        const byte RF_OCP_TRIM_105 = 0x0C;
        const byte RF_OCP_TRIM_110 = 0x0D;
        const byte RF_OCP_TRIM_115 = 0x0E;
        const byte RF_OCP_TRIM_120 = 0x0F;


        // RegAgcRef - not present on RFM69/SX1231
        const byte RF_AGCREF_AUTO_ON = 0x40;  // Default
        const byte RF_AGCREF_AUTO_OFF = 0x00;

        const byte RF_AGCREF_LEVEL_MINUS80 = 0x00;  // Default
        const byte RF_AGCREF_LEVEL_MINUS81 = 0x01;
        const byte RF_AGCREF_LEVEL_MINUS82 = 0x02;
        const byte RF_AGCREF_LEVEL_MINUS83 = 0x03;
        const byte RF_AGCREF_LEVEL_MINUS84 = 0x04;
        const byte RF_AGCREF_LEVEL_MINUS85 = 0x05;
        const byte RF_AGCREF_LEVEL_MINUS86 = 0x06;
        const byte RF_AGCREF_LEVEL_MINUS87 = 0x07;
        const byte RF_AGCREF_LEVEL_MINUS88 = 0x08;
        const byte RF_AGCREF_LEVEL_MINUS89 = 0x09;
        const byte RF_AGCREF_LEVEL_MINUS90 = 0x0A;
        const byte RF_AGCREF_LEVEL_MINUS91 = 0x0B;
        const byte RF_AGCREF_LEVEL_MINUS92 = 0x0C;
        const byte RF_AGCREF_LEVEL_MINUS93 = 0x0D;
        const byte RF_AGCREF_LEVEL_MINUS94 = 0x0E;
        const byte RF_AGCREF_LEVEL_MINUS95 = 0x0F;
        const byte RF_AGCREF_LEVEL_MINUS96 = 0x10;
        const byte RF_AGCREF_LEVEL_MINUS97 = 0x11;
        const byte RF_AGCREF_LEVEL_MINUS98 = 0x12;
        const byte RF_AGCREF_LEVEL_MINUS99 = 0x13;
        const byte RF_AGCREF_LEVEL_MINUS100 = 0x14;
        const byte RF_AGCREF_LEVEL_MINUS101 = 0x15;
        const byte RF_AGCREF_LEVEL_MINUS102 = 0x16;
        const byte RF_AGCREF_LEVEL_MINUS103 = 0x17;
        const byte RF_AGCREF_LEVEL_MINUS104 = 0x18;
        const byte RF_AGCREF_LEVEL_MINUS105 = 0x19;
        const byte RF_AGCREF_LEVEL_MINUS106 = 0x1A;
        const byte RF_AGCREF_LEVEL_MINUS107 = 0x1B;
        const byte RF_AGCREF_LEVEL_MINUS108 = 0x1C;
        const byte RF_AGCREF_LEVEL_MINUS109 = 0x1D;
        const byte RF_AGCREF_LEVEL_MINUS110 = 0x1E;
        const byte RF_AGCREF_LEVEL_MINUS111 = 0x1F;
        const byte RF_AGCREF_LEVEL_MINUS112 = 0x20;
        const byte RF_AGCREF_LEVEL_MINUS113 = 0x21;
        const byte RF_AGCREF_LEVEL_MINUS114 = 0x22;
        const byte RF_AGCREF_LEVEL_MINUS115 = 0x23;
        const byte RF_AGCREF_LEVEL_MINUS116 = 0x24;
        const byte RF_AGCREF_LEVEL_MINUS117 = 0x25;
        const byte RF_AGCREF_LEVEL_MINUS118 = 0x26;
        const byte RF_AGCREF_LEVEL_MINUS119 = 0x27;
        const byte RF_AGCREF_LEVEL_MINUS120 = 0x28;
        const byte RF_AGCREF_LEVEL_MINUS121 = 0x29;
        const byte RF_AGCREF_LEVEL_MINUS122 = 0x2A;
        const byte RF_AGCREF_LEVEL_MINUS123 = 0x2B;
        const byte RF_AGCREF_LEVEL_MINUS124 = 0x2C;
        const byte RF_AGCREF_LEVEL_MINUS125 = 0x2D;
        const byte RF_AGCREF_LEVEL_MINUS126 = 0x2E;
        const byte RF_AGCREF_LEVEL_MINUS127 = 0x2F;
        const byte RF_AGCREF_LEVEL_MINUS128 = 0x30;
        const byte RF_AGCREF_LEVEL_MINUS129 = 0x31;
        const byte RF_AGCREF_LEVEL_MINUS130 = 0x32;
        const byte RF_AGCREF_LEVEL_MINUS131 = 0x33;
        const byte RF_AGCREF_LEVEL_MINUS132 = 0x34;
        const byte RF_AGCREF_LEVEL_MINUS133 = 0x35;
        const byte RF_AGCREF_LEVEL_MINUS134 = 0x36;
        const byte RF_AGCREF_LEVEL_MINUS135 = 0x37;
        const byte RF_AGCREF_LEVEL_MINUS136 = 0x38;
        const byte RF_AGCREF_LEVEL_MINUS137 = 0x39;
        const byte RF_AGCREF_LEVEL_MINUS138 = 0x3A;
        const byte RF_AGCREF_LEVEL_MINUS139 = 0x3B;
        const byte RF_AGCREF_LEVEL_MINUS140 = 0x3C;
        const byte RF_AGCREF_LEVEL_MINUS141 = 0x3D;
        const byte RF_AGCREF_LEVEL_MINUS142 = 0x3E;
        const byte RF_AGCREF_LEVEL_MINUS143 = 0x3F;


        // RegAgcThresh1 - not present on RFM69/SX1231
        const byte RF_AGCTHRESH1_SNRMARGIN_000 = 0x00;
        const byte RF_AGCTHRESH1_SNRMARGIN_001 = 0x20;
        const byte RF_AGCTHRESH1_SNRMARGIN_010 = 0x40;
        const byte RF_AGCTHRESH1_SNRMARGIN_011 = 0x60;
        const byte RF_AGCTHRESH1_SNRMARGIN_100 = 0x80;
        const byte RF_AGCTHRESH1_SNRMARGIN_101 = 0xA0;  // Default
        const byte RF_AGCTHRESH1_SNRMARGIN_110 = 0xC0;
        const byte RF_AGCTHRESH1_SNRMARGIN_111 = 0xE0;

        const byte RF_AGCTHRESH1_STEP1_0 = 0x00;
        const byte RF_AGCTHRESH1_STEP1_1 = 0x01;
        const byte RF_AGCTHRESH1_STEP1_2 = 0x02;
        const byte RF_AGCTHRESH1_STEP1_3 = 0x03;
        const byte RF_AGCTHRESH1_STEP1_4 = 0x04;
        const byte RF_AGCTHRESH1_STEP1_5 = 0x05;
        const byte RF_AGCTHRESH1_STEP1_6 = 0x06;
        const byte RF_AGCTHRESH1_STEP1_7 = 0x07;
        const byte RF_AGCTHRESH1_STEP1_8 = 0x08;
        const byte RF_AGCTHRESH1_STEP1_9 = 0x09;
        const byte RF_AGCTHRESH1_STEP1_10 = 0x0A;
        const byte RF_AGCTHRESH1_STEP1_11 = 0x0B;
        const byte RF_AGCTHRESH1_STEP1_12 = 0x0C;
        const byte RF_AGCTHRESH1_STEP1_13 = 0x0D;
        const byte RF_AGCTHRESH1_STEP1_14 = 0x0E;
        const byte RF_AGCTHRESH1_STEP1_15 = 0x0F;
        const byte RF_AGCTHRESH1_STEP1_16 = 0x10;  // Default
        const byte RF_AGCTHRESH1_STEP1_17 = 0x11;
        const byte RF_AGCTHRESH1_STEP1_18 = 0x12;
        const byte RF_AGCTHRESH1_STEP1_19 = 0x13;
        const byte RF_AGCTHRESH1_STEP1_20 = 0x14;
        const byte RF_AGCTHRESH1_STEP1_21 = 0x15;
        const byte RF_AGCTHRESH1_STEP1_22 = 0x16;
        const byte RF_AGCTHRESH1_STEP1_23 = 0x17;
        const byte RF_AGCTHRESH1_STEP1_24 = 0x18;
        const byte RF_AGCTHRESH1_STEP1_25 = 0x19;
        const byte RF_AGCTHRESH1_STEP1_26 = 0x1A;
        const byte RF_AGCTHRESH1_STEP1_27 = 0x1B;
        const byte RF_AGCTHRESH1_STEP1_28 = 0x1C;
        const byte RF_AGCTHRESH1_STEP1_29 = 0x1D;
        const byte RF_AGCTHRESH1_STEP1_30 = 0x1E;
        const byte RF_AGCTHRESH1_STEP1_31 = 0x1F;


        // RegAgcThresh2 - not present on RFM69/SX1231
        const byte RF_AGCTHRESH2_STEP2_0 = 0x00;
        const byte RF_AGCTHRESH2_STEP2_1 = 0x10;
        const byte RF_AGCTHRESH2_STEP2_2 = 0x20;
        const byte RF_AGCTHRESH2_STEP2_3 = 0x30;  // XXX wrong -- Default
        const byte RF_AGCTHRESH2_STEP2_4 = 0x40;
        const byte RF_AGCTHRESH2_STEP2_5 = 0x50;
        const byte RF_AGCTHRESH2_STEP2_6 = 0x60;
        const byte RF_AGCTHRESH2_STEP2_7 = 0x70;  // default
        const byte RF_AGCTHRESH2_STEP2_8 = 0x80;
        const byte RF_AGCTHRESH2_STEP2_9 = 0x90;
        const byte RF_AGCTHRESH2_STEP2_10 = 0xA0;
        const byte RF_AGCTHRESH2_STEP2_11 = 0xB0;
        const byte RF_AGCTHRESH2_STEP2_12 = 0xC0;
        const byte RF_AGCTHRESH2_STEP2_13 = 0xD0;
        const byte RF_AGCTHRESH2_STEP2_14 = 0xE0;
        const byte RF_AGCTHRESH2_STEP2_15 = 0xF0;

        const byte RF_AGCTHRESH2_STEP3_0 = 0x00;
        const byte RF_AGCTHRESH2_STEP3_1 = 0x01;
        const byte RF_AGCTHRESH2_STEP3_2 = 0x02;
        const byte RF_AGCTHRESH2_STEP3_3 = 0x03;
        const byte RF_AGCTHRESH2_STEP3_4 = 0x04;
        const byte RF_AGCTHRESH2_STEP3_5 = 0x05;
        const byte RF_AGCTHRESH2_STEP3_6 = 0x06;
        const byte RF_AGCTHRESH2_STEP3_7 = 0x07;
        const byte RF_AGCTHRESH2_STEP3_8 = 0x08;
        const byte RF_AGCTHRESH2_STEP3_9 = 0x09;
        const byte RF_AGCTHRESH2_STEP3_10 = 0x0A;
        const byte RF_AGCTHRESH2_STEP3_11 = 0x0B;  // Default
        const byte RF_AGCTHRESH2_STEP3_12 = 0x0C;
        const byte RF_AGCTHRESH2_STEP3_13 = 0x0D;
        const byte RF_AGCTHRESH2_STEP3_14 = 0x0E;
        const byte RF_AGCTHRESH2_STEP3_15 = 0x0F;


        // RegAgcThresh3 - not present on RFM69/SX1231
        const byte RF_AGCTHRESH3_STEP4_0 = 0x00;
        const byte RF_AGCTHRESH3_STEP4_1 = 0x10;
        const byte RF_AGCTHRESH3_STEP4_2 = 0x20;
        const byte RF_AGCTHRESH3_STEP4_3 = 0x30;
        const byte RF_AGCTHRESH3_STEP4_4 = 0x40;
        const byte RF_AGCTHRESH3_STEP4_5 = 0x50;
        const byte RF_AGCTHRESH3_STEP4_6 = 0x60;
        const byte RF_AGCTHRESH3_STEP4_7 = 0x70;
        const byte RF_AGCTHRESH3_STEP4_8 = 0x80;
        const byte RF_AGCTHRESH3_STEP4_9 = 0x90;  // Default
        const byte RF_AGCTHRESH3_STEP4_10 = 0xA0;
        const byte RF_AGCTHRESH3_STEP4_11 = 0xB0;
        const byte RF_AGCTHRESH3_STEP4_12 = 0xC0;
        const byte RF_AGCTHRESH3_STEP4_13 = 0xD0;
        const byte RF_AGCTHRESH3_STEP4_14 = 0xE0;
        const byte RF_AGCTHRESH3_STEP4_15 = 0xF0;

        const byte RF_AGCTHRESH3_STEP5_0 = 0x00;
        const byte RF_AGCTHRESH3_STEP5_1 = 0x01;
        const byte RF_AGCTHRESH3_STEP5_2 = 0x02;
        const byte RF_AGCTHRESH3_STEP5_3 = 0x03;
        const byte RF_AGCTHRESH3_STEP5_4 = 0x04;
        const byte RF_AGCTHRESH3_STEP5_5 = 0x05;
        const byte RF_AGCTHRESH3_STEP5_6 = 0x06;
        const byte RF_AGCTHRESH3_STEP5_7 = 0x07;
        const byte RF_AGCTHRES33_STEP5_8 = 0x08;
        const byte RF_AGCTHRESH3_STEP5_9 = 0x09;
        const byte RF_AGCTHRESH3_STEP5_10 = 0x0A;
        const byte RF_AGCTHRESH3_STEP5_11 = 0x0B;  // Default
        const byte RF_AGCTHRESH3_STEP5_12 = 0x0C;
        const byte RF_AGCTHRESH3_STEP5_13 = 0x0D;
        const byte RF_AGCTHRESH3_STEP5_14 = 0x0E;
        const byte RF_AGCTHRESH3_STEP5_15 = 0x0F;


        // RegLna
        const byte RF_LNA_ZIN_50 = 0x00;  // Reset value
        const byte RF_LNA_ZIN_200 = 0x80;  // Recommended default

        const byte RF_LNA_LOWPOWER_OFF = 0x00;  // Default
        const byte RF_LNA_LOWPOWER_ON = 0x40;

        const byte RF_LNA_CURRENTGAIN = 0x08;

        const byte RF_LNA_GAINSELECT_AUTO = 0x00;  // Default
        const byte RF_LNA_GAINSELECT_MAX = 0x01;
        const byte RF_LNA_GAINSELECT_MAXMINUS6 = 0x02;
        const byte RF_LNA_GAINSELECT_MAXMINUS12 = 0x03;
        const byte RF_LNA_GAINSELECT_MAXMINUS24 = 0x04;
        const byte RF_LNA_GAINSELECT_MAXMINUS36 = 0x05;
        const byte RF_LNA_GAINSELECT_MAXMINUS48 = 0x06;


        // RegRxBw
        const byte RF_RXBW_DCCFREQ_000 = 0x00;
        const byte RF_RXBW_DCCFREQ_001 = 0x20;
        const byte RF_RXBW_DCCFREQ_010 = 0x40;  // Recommended default
        const byte RF_RXBW_DCCFREQ_011 = 0x60;
        const byte RF_RXBW_DCCFREQ_100 = 0x80; // Reset value
        const byte RF_RXBW_DCCFREQ_101 = 0xA0;
        const byte RF_RXBW_DCCFREQ_110 = 0xC0;
        const byte RF_RXBW_DCCFREQ_111 = 0xE0;

        const byte RF_RXBW_MANT_16 = 0x00;  // Reset value
        const byte RF_RXBW_MANT_20 = 0x08;
        const byte RF_RXBW_MANT_24 = 0x10;  // Recommended default

        const byte RF_RXBW_EXP_0 = 0x00;
        const byte RF_RXBW_EXP_1 = 0x01;
        const byte RF_RXBW_EXP_2 = 0x02;
        const byte RF_RXBW_EXP_3 = 0x03;
        const byte RF_RXBW_EXP_4 = 0x04;
        const byte RF_RXBW_EXP_5 = 0x05;  // Recommended default
        const byte RF_RXBW_EXP_6 = 0x06;  // Reset value
        const byte RF_RXBW_EXP_7 = 0x07;


        // RegAfcBw
        const byte RF_AFCBW_DCCFREQAFC_000 = 0x00;
        const byte RF_AFCBW_DCCFREQAFC_001 = 0x20;
        const byte RF_AFCBW_DCCFREQAFC_010 = 0x40;
        const byte RF_AFCBW_DCCFREQAFC_011 = 0x60;
        const byte RF_AFCBW_DCCFREQAFC_100 = 0x80;  // Default
        const byte RF_AFCBW_DCCFREQAFC_101 = 0xA0;
        const byte RF_AFCBW_DCCFREQAFC_110 = 0xC0;
        const byte RF_AFCBW_DCCFREQAFC_111 = 0xE0;

        const byte RF_AFCBW_MANTAFC_16 = 0x00;
        const byte RF_AFCBW_MANTAFC_20 = 0x08;  // Default
        const byte RF_AFCBW_MANTAFC_24 = 0x10;

        const byte RF_AFCBW_EXPAFC_0 = 0x00;
        const byte RF_AFCBW_EXPAFC_1 = 0x01;
        const byte RF_AFCBW_EXPAFC_2 = 0x02;  // Reset value
        const byte RF_AFCBW_EXPAFC_3 = 0x03;  // Recommended default
        const byte RF_AFCBW_EXPAFC_4 = 0x04;
        const byte RF_AFCBW_EXPAFC_5 = 0x05;
        const byte RF_AFCBW_EXPAFC_6 = 0x06;
        const byte RF_AFCBW_EXPAFC_7 = 0x07;


        // RegOokPeak
        const byte RF_OOKPEAK_THRESHTYPE_FIXED = 0x00;
        const byte RF_OOKPEAK_THRESHTYPE_PEAK = 0x40;  // Default
        const byte RF_OOKPEAK_THRESHTYPE_AVERAGE = 0x80;

        const byte RF_OOKPEAK_PEAKTHRESHSTEP_000 = 0x00;  // Default
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_001 = 0x08;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_010 = 0x10;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_011 = 0x18;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_100 = 0x20;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_101 = 0x28;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_110 = 0x30;
        const byte RF_OOKPEAK_PEAKTHRESHSTEP_111 = 0x38;

        const byte RF_OOKPEAK_PEAKTHRESHDEC_000 = 0x00;  // Default
        const byte RF_OOKPEAK_PEAKTHRESHDEC_001 = 0x01;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_010 = 0x02;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_011 = 0x03;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_100 = 0x04;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_101 = 0x05;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_110 = 0x06;
        const byte RF_OOKPEAK_PEAKTHRESHDEC_111 = 0x07;


        // RegOokAvg
        const byte RF_OOKAVG_AVERAGETHRESHFILT_00 = 0x00;
        const byte RF_OOKAVG_AVERAGETHRESHFILT_01 = 0x40;
        const byte RF_OOKAVG_AVERAGETHRESHFILT_10 = 0x80;  // Default
        const byte RF_OOKAVG_AVERAGETHRESHFILT_11 = 0xC0;


        // RegOokFix
        const byte RF_OOKFIX_FIXEDTHRESH_VALUE = 0x06;  // Default


        // RegAfcFei
        const byte RF_AFCFEI_FEI_DONE = 0x40;
        const byte RF_AFCFEI_FEI_START = 0x20;
        const byte RF_AFCFEI_AFC_DONE = 0x10;
        const byte RF_AFCFEI_AFCAUTOCLEAR_ON = 0x08;
        const byte RF_AFCFEI_AFCAUTOCLEAR_OFF = 0x00;  // Default

        const byte RF_AFCFEI_AFCAUTO_ON = 0x04;
        const byte RF_AFCFEI_AFCAUTO_OFF = 0x00;  // Default

        const byte RF_AFCFEI_AFC_CLEAR = 0x02;
        const byte RF_AFCFEI_AFC_START = 0x01;


        // RegRssiConfig
        const byte RF_RSSI_FASTRX_ON = 0x08;  // not present on RFM69/SX1231
        const byte RF_RSSI_FASTRX_OFF = 0x00;  // Default

        const byte RF_RSSI_DONE = 0x02;
        const byte RF_RSSI_START = 0x01;


        // RegDioMapping1
        const byte RF_DIOMAPPING1_DIO0_00 = 0x00;  // Default
        const byte RF_DIOMAPPING1_DIO0_01 = 0x40;
        const byte RF_DIOMAPPING1_DIO0_10 = 0x80;
        const byte RF_DIOMAPPING1_DIO0_11 = 0xC0;

        const byte RF_DIOMAPPING1_DIO1_00 = 0x00;  // Default
        const byte RF_DIOMAPPING1_DIO1_01 = 0x10;
        const byte RF_DIOMAPPING1_DIO1_10 = 0x20;
        const byte RF_DIOMAPPING1_DIO1_11 = 0x30;

        const byte RF_DIOMAPPING1_DIO2_00 = 0x00;  // Default
        const byte RF_DIOMAPPING1_DIO2_01 = 0x04;
        const byte RF_DIOMAPPING1_DIO2_10 = 0x08;
        const byte RF_DIOMAPPING1_DIO2_11 = 0x0C;

        const byte RF_DIOMAPPING1_DIO3_00 = 0x00;  // Default
        const byte RF_DIOMAPPING1_DIO3_01 = 0x01;
        const byte RF_DIOMAPPING1_DIO3_10 = 0x02;
        const byte RF_DIOMAPPING1_DIO3_11 = 0x03;


        // RegDioMapping2
        const byte RF_DIOMAPPING2_DIO4_00 = 0x00;  // Default
        const byte RF_DIOMAPPING2_DIO4_01 = 0x40;
        const byte RF_DIOMAPPING2_DIO4_10 = 0x80;
        const byte RF_DIOMAPPING2_DIO4_11 = 0xC0;

        const byte RF_DIOMAPPING2_DIO5_00 = 0x00;  // Default
        const byte RF_DIOMAPPING2_DIO5_01 = 0x10;
        const byte RF_DIOMAPPING2_DIO5_10 = 0x20;
        const byte RF_DIOMAPPING2_DIO5_11 = 0x30;

        const byte RF_DIOMAPPING2_CLKOUT_32 = 0x00;
        const byte RF_DIOMAPPING2_CLKOUT_16 = 0x01;
        const byte RF_DIOMAPPING2_CLKOUT_8 = 0x02;
        const byte RF_DIOMAPPING2_CLKOUT_4 = 0x03;
        const byte RF_DIOMAPPING2_CLKOUT_2 = 0x04;
        const byte RF_DIOMAPPING2_CLKOUT_1 = 0x05;  // Reset value
        const byte RF_DIOMAPPING2_CLKOUT_RC = 0x06;
        const byte RF_DIOMAPPING2_CLKOUT_OFF = 0x07;  // Recommended default


        // RegIrqFlags1
        const byte RF_IRQFLAGS1_MODEREADY = 0x80;
        const byte RF_IRQFLAGS1_RXREADY = 0x40;
        const byte RF_IRQFLAGS1_TXREADY = 0x20;
        const byte RF_IRQFLAGS1_PLLLOCK = 0x10;
        const byte RF_IRQFLAGS1_RSSI = 0x08;
        const byte RF_IRQFLAGS1_TIMEOUT = 0x04;
        const byte RF_IRQFLAGS1_AUTOMODE = 0x02;
        const byte RF_IRQFLAGS1_SYNCADDRESSMATCH = 0x01;


        // RegIrqFlags2
        const byte RF_IRQFLAGS2_FIFOFULL = 0x80;
        const byte RF_IRQFLAGS2_FIFONOTEMPTY = 0x40;
        const byte RF_IRQFLAGS2_FIFOLEVEL = 0x20;
        const byte RF_IRQFLAGS2_FIFOOVERRUN = 0x10;
        const byte RF_IRQFLAGS2_PACKETSENT = 0x08;
        const byte RF_IRQFLAGS2_PAYLOADREADY = 0x04;
        const byte RF_IRQFLAGS2_CRCOK = 0x02;
        const byte RF_IRQFLAGS2_LOWBAT = 0x01;  // not present on RFM69/SX1231


        // RegRssiThresh
        const byte RF_RSSITHRESH_VALUE = 0xE4;  // Default


        // RegRxTimeout1
        const byte RF_RXTIMEOUT1_RXSTART_VALUE = 0x00;  // Default


        // RegRxTimeout2
        const byte RF_RXTIMEOUT2_RSSITHRESH_VALUE = 0x00;  // Default


        // RegPreamble
        const byte RF_PREAMBLESIZE_MSB_VALUE = 0x00;  // Default
        const byte RF_PREAMBLESIZE_LSB_VALUE = 0x03;  // Default


        // RegSyncConfig
        const byte RF_SYNC_ON = 0x80;  // Default
        const byte RF_SYNC_OFF = 0x00;

        const byte RF_SYNC_FIFOFILL_AUTO = 0x00;  // Default -- when sync interrupt occurs
        const byte RF_SYNC_FIFOFILL_MANUAL = 0x40;

        const byte RF_SYNC_SIZE_1 = 0x00;
        const byte RF_SYNC_SIZE_2 = 0x08;
        const byte RF_SYNC_SIZE_3 = 0x10;
        const byte RF_SYNC_SIZE_4 = 0x18;  // Default
        const byte RF_SYNC_SIZE_5 = 0x20;
        const byte RF_SYNC_SIZE_6 = 0x28;
        const byte RF_SYNC_SIZE_7 = 0x30;
        const byte RF_SYNC_SIZE_8 = 0x38;

        const byte RF_SYNC_TOL_0 = 0x00;  // Default
        const byte RF_SYNC_TOL_1 = 0x01;
        const byte RF_SYNC_TOL_2 = 0x02;
        const byte RF_SYNC_TOL_3 = 0x03;
        const byte RF_SYNC_TOL_4 = 0x04;
        const byte RF_SYNC_TOL_5 = 0x05;
        const byte RF_SYNC_TOL_6 = 0x06;
        const byte RF_SYNC_TOL_7 = 0x07;


        // RegSyncValue1-8
        const byte RF_SYNC_BYTE1_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE2_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE3_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE4_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE5_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE6_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE7_VALUE = 0x00;  // Default
        const byte RF_SYNC_BYTE8_VALUE = 0x00;  // Default


        // RegPacketConfig1
        const byte RF_PACKET1_FORMAT_FIXED = 0x00;  // Default
        const byte RF_PACKET1_FORMAT_VARIABLE = 0x80;

        const byte RF_PACKET1_DCFREE_OFF = 0x00;  // Default
        const byte RF_PACKET1_DCFREE_MANCHESTER = 0x20;
        const byte RF_PACKET1_DCFREE_WHITENING = 0x40;

        const byte RF_PACKET1_CRC_ON = 0x10;  // Default
        const byte RF_PACKET1_CRC_OFF = 0x00;

        const byte RF_PACKET1_CRCAUTOCLEAR_ON = 0x00;  // Default
        const byte RF_PACKET1_CRCAUTOCLEAR_OFF = 0x08;

        const byte RF_PACKET1_ADRSFILTERING_OFF = 0x00;  // Default
        const byte RF_PACKET1_ADRSFILTERING_NODE = 0x02;
        const byte RF_PACKET1_ADRSFILTERING_NODEBROADCAST = 0x04;


        // RegPayloadLength
        const byte RF_PAYLOADLENGTH_VALUE = 0x40;  // Default


        // RegBroadcastAdrs
        const byte RF_BROADCASTADDRESS_VALUE = 0x00;


        // RegAutoModes
        const byte RF_AUTOMODES_ENTER_OFF = 0x00;  // Default
        const byte RF_AUTOMODES_ENTER_FIFONOTEMPTY = 0x20;
        const byte RF_AUTOMODES_ENTER_FIFOLEVEL = 0x40;
        const byte RF_AUTOMODES_ENTER_CRCOK = 0x60;
        const byte RF_AUTOMODES_ENTER_PAYLOADREADY = 0x80;
        const byte RF_AUTOMODES_ENTER_SYNCADRSMATCH = 0xA0;
        const byte RF_AUTOMODES_ENTER_PACKETSENT = 0xC0;
        const byte RF_AUTOMODES_ENTER_FIFOEMPTY = 0xE0;

        const byte RF_AUTOMODES_EXIT_OFF = 0x00;  // Default
        const byte RF_AUTOMODES_EXIT_FIFOEMPTY = 0x04;
        const byte RF_AUTOMODES_EXIT_FIFOLEVEL = 0x08;
        const byte RF_AUTOMODES_EXIT_CRCOK = 0x0C;
        const byte RF_AUTOMODES_EXIT_PAYLOADREADY = 0x10;
        const byte RF_AUTOMODES_EXIT_SYNCADRSMATCH = 0x14;
        const byte RF_AUTOMODES_EXIT_PACKETSENT = 0x18;
        const byte RF_AUTOMODES_EXIT_RXTIMEOUT = 0x1C;

        const byte RF_AUTOMODES_INTERMEDIATE_SLEEP = 0x00;  // Default
        const byte RF_AUTOMODES_INTERMEDIATE_STANDBY = 0x01;
        const byte RF_AUTOMODES_INTERMEDIATE_RECEIVER = 0x02;
        const byte RF_AUTOMODES_INTERMEDIATE_TRANSMITTER = 0x03;


        // RegFifoThresh
        const byte RF_FIFOTHRESH_TXSTART_FIFOTHRESH = 0x00;  // Reset value
        const byte RF_FIFOTHRESH_TXSTART_FIFONOTEMPTY = 0x80;  // Recommended default

        const byte RF_FIFOTHRESH_VALUE = 0x0F;  // Default


        // RegPacketConfig2
        const byte RF_PACKET2_RXRESTARTDELAY_1BIT = 0x00;  // Default
        const byte RF_PACKET2_RXRESTARTDELAY_2BITS = 0x10;
        const byte RF_PACKET2_RXRESTARTDELAY_4BITS = 0x20;
        const byte RF_PACKET2_RXRESTARTDELAY_8BITS = 0x30;
        const byte RF_PACKET2_RXRESTARTDELAY_16BITS = 0x40;
        const byte RF_PACKET2_RXRESTARTDELAY_32BITS = 0x50;
        const byte RF_PACKET2_RXRESTARTDELAY_64BITS = 0x60;
        const byte RF_PACKET2_RXRESTARTDELAY_128BITS = 0x70;
        const byte RF_PACKET2_RXRESTARTDELAY_256BITS = 0x80;
        const byte RF_PACKET2_RXRESTARTDELAY_512BITS = 0x90;
        const byte RF_PACKET2_RXRESTARTDELAY_1024BITS = 0xA0;
        const byte RF_PACKET2_RXRESTARTDELAY_2048BITS = 0xB0;
        const byte RF_PACKET2_RXRESTARTDELAY_NONE = 0xC0;
        const byte RF_PACKET2_RXRESTART = 0x04;

        const byte RF_PACKET2_AUTORXRESTART_ON = 0x02;  // Default
        const byte RF_PACKET2_AUTORXRESTART_OFF = 0x00;

        const byte RF_PACKET2_AES_ON = 0x01;
        const byte RF_PACKET2_AES_OFF = 0x00;  // Default

        // RegAesKey1-16
        const byte RF_AESKEY1_VALUE = 0x00;  // Default
        const byte RF_AESKEY2_VALUE = 0x00;  // Default
        const byte RF_AESKEY3_VALUE = 0x00;  // Default
        const byte RF_AESKEY4_VALUE = 0x00;  // Default
        const byte RF_AESKEY5_VALUE = 0x00;  // Default
        const byte RF_AESKEY6_VALUE = 0x00;  // Default
        const byte RF_AESKEY7_VALUE = 0x00;  // Default
        const byte RF_AESKEY8_VALUE = 0x00;  // Default
        const byte RF_AESKEY9_VALUE = 0x00;  // Default
        const byte RF_AESKEY10_VALUE = 0x00;  // Default
        const byte RF_AESKEY11_VALUE = 0x00;  // Default
        const byte RF_AESKEY12_VALUE = 0x00;  // Default
        const byte RF_AESKEY13_VALUE = 0x00;  // Default
        const byte RF_AESKEY14_VALUE = 0x00;  // Default
        const byte RF_AESKEY15_VALUE = 0x00;  // Default
        const byte RF_AESKEY16_VALUE = 0x00;  // Default


        // RegTemp1
        const byte RF_TEMP1_MEAS_START = 0x08;
        const byte RF_TEMP1_MEAS_RUNNING = 0x04;
        // not present on RFM69/SX1231
        const byte RF_TEMP1_ADCLOWPOWER_ON = 0x01;  // Default
        const byte RF_TEMP1_ADCLOWPOWER_OFF = 0x00;


        // RegTestLna
        const byte RF_TESTLNA_NORMAL = 0x1B;
        const byte RF_TESTLNA_HIGH_SENSITIVITY = 0x2D;


        // RegTestDagc
        const byte RF_DAGC_NORMAL = 0x00;  // Reset value
        const byte RF_DAGC_IMPROVED_LOWBETA1 = 0x20;
        const byte RF_DAGC_IMPROVED_LOWBETA0 = 0x30;  // Recommended default

        #endregion

        //***************** End of RFM69 Registers and Definitions ***************

        // Stuff for message Buffer
        private const int _defaultCapacity = 16;
        private static MessageEntity[] _buffer = new MessageEntity[_defaultCapacity];
        private static int _head;
        private static int _tail;
        private static int _count;
        private static int _capacity;


        // Changed by RoSchmi from -90 to -82
        const short CSMA_LIMIT = -82;
        //const short  CSMA_LIMIT        =   -90;   // upper RX signal sensitivity threshold in dBm for carrier sense access
        const byte RF69_MODE_SLEEP = 0;              // XTAL OFF
        const byte RF69_MODE_STANDBY = 1;              // XTAL ON
        const byte RF69_MODE_SYNTH = 2;              // PLL ON
        const byte RF69_MODE_RX = 3;              // RX MODE
        const byte RF69_MODE_TX = 4;              // TX MODE

        const byte RF69_BROADCAST_ADDR = 0xFF;
        // Changed by RoSchmi
        const byte RF69_MAX_DATA_LEN = 60;
        //const byte RF69_MAX_DATA_LEN = 61;

        // By RoSchmi, used in the canSend method
        int _adjusted_CSMA_LIMIT = CSMA_LIMIT;

        const int COURSE_TEMP_COEF = -90; // puts the temperature reading in the ballpark, user can fine tune the returned value
        // Changed by RoSchmi
        //const int RF69_CSMA_LIMIT_MS = 1000;
        const int RF69_CSMA_LIMIT_MS = 200;
        const int RF69_TX_LIMIT_MS = 1000;
        
        SPI spi;
        SPI.Configuration MySpiConfig;

        private byte[] writeByteArray;
        private byte[] readByteArray;
        
        private OutputPort reset;

        //private OutputPort trigger_P5; // Only needed for impulses for logic analyzer

        private Microsoft.SPOT.Hardware.InterruptPort input;

        private static Object LockQueueAccess = new Object();
        private static Object LockReceiveDone = new Object();

        // TWS: define CTLbyte bits
        const byte RFM69_CTL_SENDACK = 0x80;
        const byte RFM69_CTL_REQACK = 0x40;

        // variables from RFM69_ATC
        private byte ACK_RSSI_REQUESTED = 0;  // new type of flag on ACK_REQUEST
        const byte RFM69_CTL_RESERVE1 = 0x20;
        private short _ackRSSI;             // saved powerLevel in case we do auto power adjustment, this value gets dithered
        private short _targetRSSI = 0;      // if non-zero then this is the desired end point RSSI for our transmission, zero is default to disabled
        private byte _transmitLevel = 31;

        public byte[] DATA = new byte[RF69_MAX_DATA_LEN]; // recv/xmit buf, including header & crc bytes
        public byte DATALEN;
        public byte SENDERID;
        private byte TARGETID;     // should match _address
        private byte PAYLOADLEN;
        private byte ACK_REQUESTED;
        private byte ACK_RECEIVED; // should be polled immediately after sending a packet with ACK request
        public Int16 RSSI;

        private bool _promiscuousMode = false;
        private int _powerLevel = 31;
        private int _maxPowerLevel = 31;
        private bool _isRFM69HW = false;
        private byte _address;
        private byte _networkID = 100;
        private byte _mode = 1;

        public enum Frequency : byte { RF69_315MHZ = 31, RF69_433MHZ = 43, RF69_868MHZ = 86, RF69_915MHZ = 91 };

        private string label = string.Empty;
        private string location = string.Empty;
        private string measuredQuantity = string.Empty;
        private string destinationTable = string.Empty;

        private int setModeLoopCtr = 0;
        private int rescueCounter = 0;

        #region Constructor
        /// <summary>Constructs a new instance.</summary>
        /// <param name="pSpiMod">The SPI-Module</param>
        /// <param name="pChipSelect">The SPI-Chip-Select Pin</param>
        /// <param name="pExtInterrupt">The Interrupt-Pin</param>
        /// <param name="isRFM69HW">select true if the module is a RFM69HW or RFM69HCW</param>
        /// <param name="pLabel">optional, a name (label) for this radio module (returned in event args)</param>
        /// <param name="pLocation">optional, the loacation from where messages are transmitted (returned in event args).</param>
        /// <param name="pMeasuredQuantity">optional, the measure quantity values from this source have (e.g. °C, Torr, %) (returned in event args)</param>
        /// <param name="pDestinationTable">optional, the target where messages which come with the event handler shall be stored (returned in event args)</param>
        /// 

        public RFM69_NETMF(SPI.SPI_module pSpiMod, Cpu.Pin pChipSelect, Cpu.Pin pExtInterrupt, Cpu.Pin pReset, bool isRFM69HW, string pLabel = "", string pLocation = "", string pMeasuredQuantity = "", string pDestinationTable = "")
        {
            this.label = pLabel;
            this.location = pLocation;
            this.measuredQuantity = pMeasuredQuantity;
            this.destinationTable = pDestinationTable;

            QueueInitialize();
           
            bool SPI_CS_ACTIVE_HIGH = false;
            uint SPI_CS_SETUP_TIME = 0;        
            uint SPI_CS_HOLD_TIME = 0;         
            bool SPI_CLOCK_IDLE_HIGH = false;
            bool SPI_SAMPLING_EDGE_RISING = true;
            uint SPI_CLOCK_RATE_KHZ = 1000;

            this.MySpiConfig = new SPI.Configuration(pChipSelect, SPI_CS_ACTIVE_HIGH,SPI_CS_SETUP_TIME,SPI_CS_HOLD_TIME,SPI_CLOCK_IDLE_HIGH,SPI_SAMPLING_EDGE_RISING,SPI_CLOCK_RATE_KHZ,pSpiMod);
            this.spi = new SPI(MySpiConfig);

            this.input = new InterruptPort(pExtInterrupt, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            this.input.DisableInterrupt();
            this.input.OnInterrupt +=input_OnInterrupt;

            this._mode = RF69_MODE_STANDBY;        
            this._promiscuousMode = false;         
            this._powerLevel = 31;
            this._isRFM69HW = isRFM69HW;

            this.reset = new OutputPort(pReset, false);
            //this.trigger_P5 =  new OutputPort(xxx, false); 
        }
        #endregion

        #region Stuff concerning Send Queue
        public void QueueInitialize()
        {
            _capacity = _defaultCapacity;
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public int QueueCount
        {
            get { return _count; }
        }

        public int Queuecapacity
        {
            get { return _capacity; }
        }

        public void QueueClear()
        {
            _count = 0;
            _tail = _head;
        }

        public bool QueueHasFreePlaces(int value = 1)
        {
            if (_count + value <= _capacity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void QueueEnqueueMessageEntity(MessageEntity pMessageEntity)
        {
            if (_count == _capacity)
            {

                //Grow();
                //Debug.Print("New Capacity: " + _buffer.Length);
            }
            _buffer[_head] = pMessageEntity;
            _head = (_head + 1) % _capacity;
            _count++;
        }

        public static MessageEntity QueuePreViewNextMessageEntity()
        {
            MessageEntity Value = QueueDequeueMessageEntity(true);
            return Value;
        }

        public static MessageEntity QueueDequeueNextMessageEntity()
        {
            MessageEntity Value = QueueDequeueMessageEntity(false);
            return Value;
        }

        private static MessageEntity QueueDequeueMessageEntity(bool PreView)
        {
            if (_count > 0)
            {
                MessageEntity value = _buffer[_tail];

                if (!PreView)
                {
                    _tail = (_tail + 1) % _capacity;
                    _count--;
                }
                return value;
            }
            else
            {
                return null;
                //SampleValue InvalidSampleValue = new SampleValue(DateTime.Now, 0);
                //return InvalidSampleValue;
            }
        }
        #endregion

        #region Interrupt handler
        void input_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            if ((_mode == RF69_MODE_RX) && ((byte)(readReg(REG_IRQFLAGS2) & RF_IRQFLAGS2_PAYLOADREADY) != 0))
            {
#if DebugPrint
                    Debug.Print("In Interrupt, in received loop");
#endif
                RSSI = readRSSI(false);
                setMode(RF69_MODE_STANDBY);

                this.input.DisableInterrupt();

                //trigger_P5.Write(false);     //Used to trigger Logic Analyzer
                //trigger_P5.Write(true);

                PAYLOADLEN = readReg(REG_FIFO);
                PAYLOADLEN = PAYLOADLEN > 66 ? (byte)66 : PAYLOADLEN; // precaution
                TARGETID = readReg(REG_FIFO);

                // match this node's address, or broadcast address or anything in promiscuous mode
                // address situation could receive packets that are malformed and don't fit this libraries extra fields
                bool theResult = !(_promiscuousMode || TARGETID == _address || TARGETID == RF69_BROADCAST_ADDR) || PAYLOADLEN < 3;

                if (!(_promiscuousMode || TARGETID == _address || TARGETID == RF69_BROADCAST_ADDR) || PAYLOADLEN < 3)
                {
                    PAYLOADLEN = 0;
                    receiveBegin();
                    this.input.EnableInterrupt();
                    return;
                }

                DATALEN = (byte)((int)PAYLOADLEN - 3);
                SENDERID = readReg(REG_FIFO);
                byte CTLbyte = readReg(REG_FIFO);
                ACK_RECEIVED = (byte)(CTLbyte & RFM69_CTL_SENDACK); // extract ACK-received flag   0x80
                ACK_REQUESTED = (byte)(CTLbyte & RFM69_CTL_REQACK); // extract ACK-requested flag  0x04

                interruptHook(CTLbyte);     // TWS: hook to derived class interrupt function

                byte[] spiCMD = new byte[DATALEN + 1];
                spiCMD[0] = REG_FIFO & 0x7F;
                byte[] spiResult = new byte[DATALEN + 1];
                spiResult = SPIWriteReadData(spiCMD);

                Array.Copy(spiResult, 1, DATA, 0, DATALEN);

                if (DATALEN < RF69_MAX_DATA_LEN)
                {
                    DATA[DATALEN] = 0; // add null at end of string
                }
                setMode(RF69_MODE_RX);
            }
            //RSSI = readRSSI();
        }
        #endregion

        #region interruptHook for ATC (Automatic transmission control)
        //=============================================================================
        // interruptHook() - gets called by the base class interrupt handler right after the header is fetched.
        //=============================================================================
        void interruptHook(byte CTLbyte)
        {
            ACK_RSSI_REQUESTED = (byte)(CTLbyte & RFM69_CTL_RESERVE1); // TomWS1: extract the ACK RSSI request bit (could potentially merge with ACK_REQUESTED)
            // TomWS1: now see if this was an ACK with an ACK_RSSI response
            if ((ACK_RECEIVED != 0x00) && (ACK_RSSI_REQUESTED != 0x00))
            {
                // the next byte contains the ACK_RSSI (assuming the datalength is valid)
                if (DATALEN >= 1)
                {
                    _ackRSSI = (short)((short)readReg(REG_FIFO) * -1);
                    DATALEN -= 1;   // and compensate data length accordingly
                    // TomWS1: Now dither transmitLevel value (register update occurs later when transmitting);
                    if (_targetRSSI != 0)
                    {
                        if (_ackRSSI < _targetRSSI && _transmitLevel < _maxPowerLevel)
                        {
                            _transmitLevel++; /*Debug.Print("\n ==_transmitLevel ++   == to " + _transmitLevel.ToString());*/
                        }
                        else if (_ackRSSI > _targetRSSI && _transmitLevel > 1)
                        {
                            _transmitLevel -= 2; /*Debug.Print("\n == _transmitLevel --   == to " + _transmitLevel.ToString());*/
                        }
                    }
                }
            }
        }
        #endregion

        #region internal Method readRSSI(..)
        // get the received signal strength indicator (RSSI)
        private short readRSSI(bool forceTrigger = false)
        {
            short rssi = 0;
            if (forceTrigger)
            {
                // RSSI trigger not needed if DAGC is in continuous mode
                if ((byte)(readReg(REG_TESTDAGC) & RF_DAGC_IMPROVED_LOWBETA0) == 0x00)  //RssiStart command and RssiDone flags are not usable when DAGC is turned on
                {
                    writeReg(REG_RSSICONFIG, RF_RSSI_START);
                    while ((readReg(REG_RSSICONFIG) & RF_RSSI_DONE) == 0x00) // wait for RSSI_Ready
                    {
                        //Debug.Print(readReg(REG_RSSICONFIG).ToString("X2"));
                        Thread.Sleep(50);
                    }
                }
            }
            rssi = (short)-readReg(REG_RSSIVALUE);
            rssi >>= 1;
            return rssi;
        }
        #endregion

        #region public Method hardReset()
        public void hardReset()
        {
            this.reset.Write(false);
            Thread.Sleep(100);
            this.reset.Write(true);
            Thread.Sleep(100);
            this.reset.Write(false);
            Thread.Sleep(20);
        }
        #endregion

        #region public Method Initialize(..)
        /// <summary>Initializes the RFM69 with frequency, nodeID and networkID </summary>
        /// <param name="freqBand">The proper frequency for your RFM69 module. Enumeration RF69_315MHZ, RF69_433MHZ, RF69_868MHZ or RF69_915MHZ</param>
        /// <param name="nodeID">The nodeID of this module. All modules in a network must have a different nodeID</param>
        /// <param name="networkID">The networkID. All modules you want to talk with must have the same networkID</param>
        /// 
        public bool initialize(Frequency freqBand, byte nodeID, byte networkID)  //e.g: NetworkID = 100, NodeID = 1
        {
            // Set parameter for ATC mode (automatic transmission control)
            this._targetRSSI = 0;        // TomWS1: default to disabled
            this._ackRSSI = 0;           // TomWS1: no existing response at init time
            this.ACK_RSSI_REQUESTED = 0; // TomWS1: init to none
            this._transmitLevel = 31;    // TomWS1: match default value in PA Level register

            // Set NodeID
            _address = nodeID;          // e.g. NodeID = 1
            _networkID = networkID;     // e.g networkID = 100

            byte[][] CONFIG = new byte[][] {
                new byte[2] {REG_OPMODE, RF_OPMODE_SEQUENCER_ON | RF_OPMODE_LISTEN_OFF | RF_OPMODE_STANDBY},
                new byte[2] {REG_DATAMODUL, RF_DATAMODUL_DATAMODE_PACKET | RF_DATAMODUL_MODULATIONTYPE_FSK | RF_DATAMODUL_MODULATIONSHAPING_00},  // no shaping
                new byte[2] {REG_BITRATEMSB, RF_BITRATEMSB_55555},      // default: 4.8 KBPS
                new byte[2] {REG_BITRATELSB, RF_BITRATELSB_55555},
                new byte[2] {REG_FDEVMSB, RF_FDEVMSB_50000},            // default: 5KHz, (FDEV + BitRate / 2 <= 500KHz)
                new byte[2] {REG_FDEVLSB, RF_FDEVLSB_50000},
                new byte[2] {REG_FRFMSB, (byte) (freqBand==Frequency.RF69_315MHZ ? RF_FRFMSB_315 : (freqBand==Frequency.RF69_433MHZ ? RF_FRFMSB_433 : (freqBand==Frequency.RF69_868MHZ ? RF_FRFMSB_868 : RF_FRFMSB_915))) },
                new byte[2] {REG_FRFMID, (byte) (freqBand==Frequency.RF69_315MHZ ? RF_FRFMID_315 : (freqBand==Frequency.RF69_433MHZ ? RF_FRFMID_433 : (freqBand==Frequency.RF69_868MHZ ? RF_FRFMID_868 : RF_FRFMID_915))) },
                new byte[2] {REG_FRFLSB, (byte) (freqBand==Frequency.RF69_315MHZ ? RF_FRFLSB_315 : (freqBand==Frequency.RF69_433MHZ ? RF_FRFLSB_433 : (freqBand==Frequency.RF69_868MHZ ? RF_FRFLSB_868 : RF_FRFLSB_915))) },

                 // looks like PA1 and PA2 are not implemented on RFM69W, hence the max output power is 13dBm
                // +17dBm and +20dBm are possible on RFM69HW
                // +13dBm formula: Pout = -18 + OutputPower (with PA0 or PA1**)
                // +17dBm formula: Pout = -14 + OutputPower (with PA1 and PA2)**
                // +20dBm formula: Pout = -11 + OutputPower (with PA1 and PA2)** and high power PA settings (section 3.3.7 in datasheet)
                ///* 0x11 */ { REG_PALEVEL, RF_PALEVEL_PA0_ON | RF_PALEVEL_PA1_OFF | RF_PALEVEL_PA2_OFF | RF_PALEVEL_OUTPUTPOWER_11111},
                ///* 0x13 */ { REG_OCP, RF_OCP_ON | RF_OCP_TRIM_95 }, // over current protection (default is 95mA)

                // RXBW defaults are { REG_RXBW, RF_RXBW_DCCFREQ_010 | RF_RXBW_MANT_24 | RF_RXBW_EXP_5} (RxBw: 10.4KHz)
                new byte[2] {REG_RXBW, RF_RXBW_DCCFREQ_010 | RF_RXBW_MANT_16 | RF_RXBW_EXP_2 },         // (BitRate < 2 * RxBw)
                 //for BR-19200: /* 0x19 */ { REG_RXBW, RF_RXBW_DCCFREQ_010 | RF_RXBW_MANT_24 | RF_RXBW_EXP_3 },
                new byte[2] {REG_DIOMAPPING1, RF_DIOMAPPING1_DIO0_01},          // DIO0 is the only IRQ we're using
                new byte[2] {REG_DIOMAPPING2, RF_DIOMAPPING2_CLKOUT_OFF},       // DIO5 ClkOut disable for power saving
                new byte[2] {REG_IRQFLAGS2, RF_IRQFLAGS2_FIFOOVERRUN},          // writing to this bit ensures that the FIFO & status flags are reset
                new byte[2] {REG_RSSITHRESH, 220},                              // must be set to dBm = (-Sensitivity / 2), default is 0xE4 = 228 so -114dBm
                ///* 0x2D */ { REG_PREAMBLELSB, RF_PREAMBLESIZE_LSB_VALUE } // default 3 preamble bytes 0xAAAAAA
                new byte[2] {REG_SYNCCONFIG, RF_SYNC_ON | RF_SYNC_FIFOFILL_AUTO | RF_SYNC_SIZE_2 | RF_SYNC_TOL_0},
                new byte[2] {REG_SYNCVALUE1, 0x2D},                             // attempt to make this compatible with sync1 byte of RFM12B lib
                new byte[2] {REG_SYNCVALUE2, networkID},                        // NETWORK ID
                new byte[2] {REG_PACKETCONFIG1, RF_PACKET1_FORMAT_VARIABLE | RF_PACKET1_DCFREE_OFF | RF_PACKET1_CRC_ON | RF_PACKET1_CRCAUTOCLEAR_ON | RF_PACKET1_ADRSFILTERING_OFF},
                new byte[2] {REG_PAYLOADLENGTH, 66},                            // in variable length mode: the max frame size, not used in TX
                ///* 0x39 */ { REG_NODEADRS, nodeID }, // turned off because we're not using address filtering
                new byte[2] {REG_FIFOTHRESH, RF_FIFOTHRESH_TXSTART_FIFONOTEMPTY | RF_FIFOTHRESH_VALUE},     // TX on FIFO not empty
                // In Arduino application RF_PACKET2_AES_ON is used
              //  new byte[2] {REG_PACKETCONFIG2, RF_PACKET2_RXRESTARTDELAY_2BITS | RF_PACKET2_AUTORXRESTART_ON | RF_PACKET2_AES_OFF },       // RXRESTARTDELAY must match transmitter PA ramp-down time (bitrate dependent)
                new byte[2] {REG_PACKETCONFIG2, RF_PACKET2_RXRESTARTDELAY_2BITS | RF_PACKET2_AUTORXRESTART_ON | RF_PACKET2_AES_ON },       // RXRESTARTDELAY must match transmitter PA ramp-down time (bitrate dependent)
                //for BR-19200: /* 0x3D */ { REG_PACKETCONFIG2, RF_PACKET2_RXRESTARTDELAY_NONE | RF_PACKET2_AUTORXRESTART_ON | RF_PACKET2_AES_OFF }, // RXRESTARTDELAY must match transmitter PA ramp-down time (bitrate dependent)
                new byte[2] {REG_TESTDAGC, RF_DAGC_IMPROVED_LOWBETA0},          // run DAGC continuously in RX mode for Fading Margin Improvement, recommended default for AfcLowBetaOn=0
                new byte[2] {255, 0},
            };
            #region asure that register write an read works
            int timeOutMs = 50;
            long timeOutTicks = timeOutMs * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            byte result = 0x11;
            do
            {
                writeReg(REG_SYNCVALUE1, 0xAA);
                result = readReg(REG_SYNCVALUE1);
            } while (result != 0xaa && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks < timeOutTicks));

            if (result == 0xaa)
            {
            #if DebugPrint
                Debug.Print("Hex 0xAA could be retrieved");
            #endif
            }
            else
            {
            #if DebugPrint
                Debug.Print("Hex 0xAA could not be retrieved, the value read back was " + result.ToString());
            #endif
                return false;
            }

            timeOutTicks = timeOutMs * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            do
            {
                writeReg(REG_SYNCVALUE1, 0x55);
                result = readReg(REG_SYNCVALUE1);
            } while (result != 0x55 && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks < timeOutTicks));
            if (result == 0x55)
            {
            #if DebugPrint
                Debug.Print("Hex 0x55 could be retrieved");
            #endif
            }
            else
            {
            #if DebugPrint
                Debug.Print("Hex 0x55 could not be retrieved, the value read back was " + result.ToString());
                throw new Exception("Error: Could not sync with RFM69HCW module!");
            #endif
                return false;
            }
            #endregion

            for (int i = 0; CONFIG[i][0] != 255; i++)
            {
                writeReg(CONFIG[i][0], CONFIG[i][1]);
                //Debug.Print(CONFIG[i][0].ToString() + "  " + CONFIG[i][1].ToString());
            }
            #if DebugPrint
                Debug.Print("Network-ID = " + readReg(REG_SYNCVALUE2).ToString());
            #endif

            // Encryption is persistent between resets and can trip you up during debugging.
            // Disable it during initialization so we always start from a known state.
            encrypt(null);

            setHighPower(_isRFM69HW); // called regardless if it's a RFM69W or RFM69HW
            setMode(RF69_MODE_STANDBY);
            timeOutTicks = timeOutMs * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            while (((byte)(readReg(REG_IRQFLAGS1) & RF_IRQFLAGS1_MODEREADY) == 0x00) && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks < timeOutTicks))
            {
                //Debug.Print("Not yet ready");
            }; // wait for ModeReady

            if ((Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks > timeOutTicks))
            {
                return false;
            }
            else
            {

            #if DebugPrint
                Debug.Print("\r\nPrinting REG-initialization:");
                Debug.Print("REG_OPMODE (0x" + REG_OPMODE.ToString("X2") + ") = 0x" + readReg(REG_OPMODE).ToString("X2") + " (" + readReg(REG_OPMODE) + ")");
                Debug.Print("REG_DATAMODUL (0x" + REG_DATAMODUL.ToString("X2") + ") = 0x" + readReg(REG_DATAMODUL).ToString("X2") + " (" + readReg(REG_DATAMODUL) + ")");
                Debug.Print("REG_BITRATEMSB (0x" + REG_BITRATEMSB.ToString("X2") + ") = 0x" + readReg(REG_BITRATEMSB).ToString("X2") + " (" + readReg(REG_BITRATEMSB) + ")");
                Debug.Print("REG_BITRATELSB (0x" + REG_BITRATELSB.ToString("X2") + ") = 0x" + readReg(REG_BITRATELSB).ToString("X2") + " (" + readReg(REG_BITRATELSB) + ")");
                Debug.Print("REG_FDEVMSB (0x" + REG_FDEVMSB.ToString("X2") + ") = 0x" + readReg(REG_FDEVMSB).ToString("X2") + " (" + readReg(REG_FDEVMSB) + ")");
                Debug.Print("REG_FDEVLSB (0x" + REG_FDEVLSB.ToString("X2") + ") = 0x" + readReg(REG_FDEVLSB).ToString("X2") + " (" + readReg(REG_FDEVLSB) + ")");
                Debug.Print("REG_FRFMSB (0x" + REG_FRFMSB.ToString("X2") + ") = 0x" + readReg(REG_FRFMSB).ToString("X2") + " (" + readReg(REG_FRFMSB) + ")");
                Debug.Print("REG_FRFMID (0x" + REG_FRFMID.ToString("X2") + ") = 0x" + readReg(REG_FRFMID).ToString("X2") + " (" + readReg(REG_FRFMID) + ")");
                Debug.Print("REG_FRFLSB (0x" + REG_FRFLSB.ToString("X2") + ") = 0x" + readReg(REG_FRFLSB).ToString("X2") + " (" + readReg(REG_FRFLSB) + ")");
                Debug.Print("");
                Debug.Print("REG_PALEVEL (0x" + REG_PALEVEL.ToString("X2") + ") = 0x" + readReg(REG_PALEVEL).ToString("X2") + " (" + readReg(REG_PALEVEL) + ")");
                Debug.Print("REG_OCP (0x" + REG_OCP.ToString("X2") + ") = 0x" + readReg(REG_OCP).ToString("X2") + " (" + readReg(REG_OCP) + ")");
                Debug.Print("");
                Debug.Print("REG_RXBW (0x" + REG_RXBW.ToString("X2") + ") = 0x" + readReg(REG_RXBW).ToString("X2") + " (" + readReg(REG_RXBW) + ")");
                Debug.Print("");
                Debug.Print("REG_DIOMAPPING1 (0x" + REG_DIOMAPPING1.ToString("X2") + ") = 0x" + readReg(REG_DIOMAPPING1).ToString("X2") + " (" + readReg(REG_DIOMAPPING1) + ")");
                Debug.Print("REG_DIOMAPPING2 (0x" + REG_DIOMAPPING2.ToString("X2") + ") = 0x" + readReg(REG_DIOMAPPING2).ToString("X2") + " (" + readReg(REG_DIOMAPPING2) + ")");
                Debug.Print("");
                Debug.Print("REG_IRQFLAGS2 (0x" + REG_IRQFLAGS2.ToString("X2") + ") = 0x" + readReg(REG_IRQFLAGS2).ToString("X2") + " (" + readReg(REG_IRQFLAGS2) + ")");
                Debug.Print("");
                Debug.Print("REG_RSSITHRESH (0x" + REG_RSSITHRESH.ToString("X2") + ") = 0x" + readReg(REG_RSSITHRESH).ToString("X2") + " (" + readReg(REG_RSSITHRESH) + ")");
                Debug.Print("REG_SYNCCONFIG (0x" + REG_SYNCCONFIG.ToString("X2") + ") = 0x" + readReg(REG_SYNCCONFIG).ToString("X2") + " (" + readReg(REG_SYNCCONFIG) + ")");
                Debug.Print("REG_SYNCVALUE1 (0x" + REG_SYNCVALUE1.ToString("X2") + ") = 0x" + readReg(REG_SYNCVALUE1).ToString("X2") + " (" + readReg(REG_SYNCVALUE1) + ")");
                Debug.Print("REG_SYNCVALUE2 (0x" + REG_SYNCVALUE2.ToString("X2") + ") = 0x" + readReg(REG_SYNCVALUE2).ToString("X2") + " (" + readReg(REG_SYNCVALUE2) + ")");
                Debug.Print("REG_PACKETCONFIG1 (0x" + REG_PACKETCONFIG1.ToString("X2") + ") = 0x" + readReg(REG_PACKETCONFIG1).ToString("X2") + " (" + readReg(REG_PACKETCONFIG1) + ")");
                Debug.Print("REG_PAYLOADLENGTH (0x" + REG_PAYLOADLENGTH.ToString("X2") + ") = 0x" + readReg(REG_PAYLOADLENGTH).ToString("X2") + " (" + readReg(REG_PAYLOADLENGTH) + ")");
                Debug.Print("REG_PACKETCONFIG2 (0x" + REG_PACKETCONFIG2.ToString("X2") + ") = 0x" + readReg(REG_PACKETCONFIG2).ToString("X2") + " (" + readReg(REG_PACKETCONFIG2) + ")");
                Debug.Print("REG_TESTDAGC (0x" + REG_TESTDAGC.ToString("X2") + ") = 0x" + readReg(REG_TESTDAGC).ToString("X2") + " (" + readReg(REG_TESTDAGC) + ")");
                #endif
                Thread LoopThread = new Thread(runReceiveLoop);
                LoopThread.Start();                                 // Start Reveive/Send-Loop in a separate thread
                this.input.EnableInterrupt();
                return true;
            }
        }

        #endregion

        #region public Method sleep()
        /// <summary>Put transceiver in sleep mode to save battery - to wake or resume receiving just call receiveDone() </summary>
        ///
        public void sleep()
        {
            setMode(RF69_MODE_SLEEP);
        }
        #endregion

        #region public Method setAddress(..)
        /// <summary>Set this node's address</summary>
        ///
        public void setAddress(byte addr)
        {
            _address = addr;
            writeReg(REG_NODEADRS, _address);
        }
        #endregion

        #region public Method setNetwork(..)
        /// <summary>Set this node's network id</summary>
        ///
        public void setNetwork(byte networkID)
        {
            _networkID = networkID;
            writeReg(REG_SYNCVALUE2, networkID);
        }
        #endregion

        #region public Method enableAutoPower
        /// <summary>Sets the targetRSSI: 0 means ACT is deactivated, e.g. - 30 is a rather high signal strength, - 90 is rather low </summary>
        /// <param name="pTargetRSSI">The RSSI target to which we want to step down to avoid a too high signal strength</param>
        ///
        public void enableAutoPower(short pTargetRSSI = -60)  // default - 60 as a still rather high signal strength
        {
            if (pTargetRSSI != 0 & ((pTargetRSSI > -20) | (pTargetRSSI < -80)))
            { throw new InvalidOperationException("targetRSSI out of allowed range (0 for deactivated or -20 to -80)"); }
            this._targetRSSI = pTargetRSSI;         // just set the value (if non-zero, then enabled), caller's responsibility to use a reasonable value
        }

        #endregion

        #region public Method getAckRSSI
        /// <summary>Gets the targetRSSI: 0 means ACT is deactivated. Otherwise power is incrementally lowered to reach this targetRSSI</summary>
        ///
        public int getAckRSSI()
        {
            return ((_targetRSSI == 0) ? 0 : _ackRSSI); // TWS: New method to retrieve the ack'd RSSI (if any) 
        }
        #endregion

        #region public Method promiscuous(..)
        /// <summary>Enables or disables filtering to capture only frames sent to this/broadcast address</summary>
        /// <param name="onOff">true = disable filtering to capture all frames on network. false = enable node/broadcast filtering to capture only frames sent to this/broadcast address</param>
        ///

        public void promiscuous(bool onOff)
        {
            _promiscuousMode = onOff;
            //writeReg(REG_PACKETCONFIG1, (byte)((byte)(readReg(REG_PACKETCONFIG1) & 0xF9) | (onOff ? RF_PACKET1_ADRSFILTERING_OFF : RF_PACKET1_ADRSFILTERING_NODEBROADCAST)));
        }
        #endregion

        #region public Method setLNA(..)
        public byte setLNA(byte newReg) // TWS: New method used to disable LNA AGC for testing purposes 
        {
            byte oldReg;
            oldReg = readReg(REG_LNA);
            //writeReg(REG_LNA, ((newReg & 7) | (oldReg & ~7)));   // just control the LNA Gain bits for now 
            writeReg(REG_LNA, (byte)((newReg & 0x07) | (oldReg & ~0x07)));   // just control the LNA Gain bits for now 
            return oldReg;  // return the original value in case we need to restore it 
        }
        #endregion

        #region public Method asyncSendWithRetry(..)
        // to increase the chance of getting a packet across, call this function instead of send
        // and it handles all the ACK requesting/retrying for you :)
        // The only twist is that you have to manually listen to ACK requests on the other side and send back the ACKs
        // The reason for the semi-automaton is that the lib is interrupt driven and
        // requires user action to read the received data and decide what to do with it

        // NETMF is not fast enough to catch an early ACK, so on the other side the sending of the ACK must be done
        // after a delay of around 50 ms, that the ACK is not missed by the NETMF device because it comes to early

        /// <summary>Performs an asynchronous transmission. The status of the ACK is returned in the 'ACKReturned' event</summary>
        /// <param name="pPayLoad">The payload to be sent to the recipient, max. 66 bytes</param>
        /// <param name="pRetries">The number of retries of transmitting the payload, no retries = 0</param>
        /// <param name="pWaitForACK_Timeout_Ms">The timeout for waiting for an ACK from the recipient (allowed: 150 - 1000 ms)</param>
        /// <param name="pTimeStamp">A TimeStamp to identifiy the ACK in the 'ACKReturned' event</param>
        ///

        public void asyncSendWithRetry(byte pToAddress, string pPayLoad, byte pRetries, short pRetryWaitTime_Ms, DateTime pTimeStamp)
        {
            asyncSendWithRetry(pToAddress, Encoding.UTF8.GetBytes(pPayLoad), pRetries, pRetryWaitTime_Ms, pTimeStamp);
        }

        /// <summary>Performs an asynchronous transmission. The status of the ACK is returned in the 'ACKReturned' event</summary>
        /// <param name="pPayLoad">The payload to be sent to the recipient, max. 66 bytes</param>
        /// <param name="pRetries">The number of retries of transmitting the payload, no retries = 0</param>
        /// <param name="pWaitForACK_Timeout_Ms">The timeout for waiting for an ACK from the recipient (allowed: 150 - 1000 ms)</param>
        /// <param name="pTimeStamp">A TimeStamp to identifiy the ACK in the 'ACKReturned' event</param>
        ///
        public void asyncSendWithRetry(byte pToAddress, byte[] pPayLoad, byte pRetries, short pWaitForACK_Timeout_Ms, DateTime pTimeStamp)
        {
            lock (LockQueueAccess)
            {
                if (QueueHasFreePlaces())
                {
                    QueueEnqueueMessageEntity(new MessageEntity(pPayLoad, pTimeStamp, _address, pToAddress, pRetries, pWaitForACK_Timeout_Ms));
                }
            }
        }
        #endregion

        #region public Method send(..)
        public void send(byte toAddress, byte[] buffer, byte bufferSize, bool requestACK)
        {
            byte sender = SENDERID;
            writeReg(REG_PACKETCONFIG2, (byte)((byte)(readReg(REG_PACKETCONFIG2) & 0xFB) | RF_PACKET2_RXRESTART)); // avoid RX deadlocks

            long timeOutTicks = RF69_CSMA_LIMIT_MS * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            while (true)
            {
                if (canSend())
                {
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT - 1;
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT < -90 ? -90 : _adjusted_CSMA_LIMIT;
            #if DebugPrint
                        Debug.Print("In send: Exit with can send");
            #endif
                    break;
                }
                if (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks > timeOutTicks)
                {
                    // Level is stepped up from -90 in steps of 2, but not higher than -70
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT + 2;
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT > -60 ? -60 : _adjusted_CSMA_LIMIT;
            #if DebugPrint
                        Debug.Print("In Send: TimeOut reached");
            #endif
                    break;
                }
                lock (LockReceiveDone)
                {
                    receiveDone();
                }
            }
            SENDERID = sender;          // TWS: Restore SenderID after it gets wiped out by receiveDone()
            sendFrame(toAddress, buffer, bufferSize, requestACK, false);

            #if DebugPrint
                Debug.Print("Return from send");
            #endif
        }
        #endregion

        #region internal Method sendFrame(..)
        void sendFrame(byte toAddress, byte[] buffer, byte bufferSize, bool requestACK, bool sendACK, bool sendRSSI = false, short lastRSSI = 0)
        {
            Thread.Sleep(0);
            long StartTime = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            setMode(RF69_MODE_STANDBY); // turn off receiver to prevent reception while filling fifo
            
            while (true)                // wait for ModeReady with timeout
            {
                if ((readReg(REG_IRQFLAGS1) & RF_IRQFLAGS1_MODEREADY) != 0x00)
                {
                    break;
                }
                if (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks - StartTime > 100 * TimeSpan.TicksPerMillisecond)
                {
                    break;
                }
            }
            //Debug.Print("Set DIO0 to Packet Sent");
            writeReg(REG_DIOMAPPING1, RF_DIOMAPPING1_DIO0_00); // DIO0 is "Packet Sent"
            if (bufferSize > RF69_MAX_DATA_LEN)
            {
                bufferSize = RF69_MAX_DATA_LEN;
            }

            // control byte
            byte CTLbyte = 0x00;
            if (sendACK)
            {
                if (sendRSSI)
                { CTLbyte = RFM69_CTL_SENDACK | RFM69_CTL_RESERVE1; }
                else
                { CTLbyte = RFM69_CTL_SENDACK; }
            }
            else if (requestACK)
            {
                if (_targetRSSI != 0)
                {
                    CTLbyte = RFM69_CTL_REQACK | RFM69_CTL_RESERVE1;
                }
                else
                {
                    CTLbyte = RFM69_CTL_REQACK;
                }
            }

            // write to FIFO
            this.input.DisableInterrupt();

            StartTime = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            byte[] spiCMD;
            if (sendRSSI)
            {
                spiCMD = new byte[bufferSize + 6];
            }
            else
            {
                spiCMD = new byte[bufferSize + 5];
            }
            spiCMD[0] = 0x80;
            if (sendRSSI)
            {
                spiCMD[1] = (byte)(bufferSize + 4);
            }
            else
            {
                spiCMD[1] = (byte)(bufferSize + 3);
            }

            spiCMD[2] = toAddress;
            spiCMD[3] = _address;
            spiCMD[4] = CTLbyte;

            byte[] spiResult;
            if (sendRSSI)
            {
                spiCMD[5] = (byte)System.Math.Abs((int)lastRSSI);
                spiResult = new byte[bufferSize + 6];
                if (bufferSize > 0)
                {
                    Array.Copy(buffer, 0, spiCMD, 6, buffer.Length);
                }
            }
            else
            {
                spiResult = new byte[bufferSize + 5];
                if (bufferSize > 0)
                {
                    Array.Copy(buffer, 0, spiCMD, 5, buffer.Length);
                }
            }
            spiResult = SPIWriteReadData(spiCMD);

            this.input.EnableInterrupt();

            // no need to wait for transmit mode to be ready since its handled by the radio
            setMode(RF69_MODE_TX);
            long timeOutTicks = RF69_CSMA_LIMIT_MS * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            StartTime = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            while (true)
            {
                Thread.Sleep(0);
                if (input.Read() == true)
                {
                    break;
                }
                if (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks > StartTime + RF69_TX_LIMIT_MS * TimeSpan.TicksPerMillisecond)
                {
                    break;
                }
            }
            setMode(RF69_MODE_STANDBY);
        }
        #endregion

        #region public Method receiveDone()
        // checks if a packet was received and/or puts transceiver in receive (ie RX or listen) mode

        public bool receiveDone()
        {
            this.input.DisableInterrupt();  // re-enabled in unselect() via setMode() or via receiveBegin()
            rescueCounter++;
            if (rescueCounter > 500)        // Since RFM69 seems to forget to make an interrupt sometimes this rescues from a hang
            {                               // which would occur otherwise by calling receiveBegin every 500th iteration
                rescueCounter = 0;
            #if DebugPrint
                    Debug.Print("RescueCounter action occured");
            #endif
                receiveBegin();
                return false;
            }

            if ((_mode == RF69_MODE_RX) && (PAYLOADLEN > 0))
            {
            #if DebugPrint
                    Debug.Print("Received Payload. Mode: " + _mode + " PAYLOADLEN: " + PAYLOADLEN.ToString() + " RescueCounter: " + rescueCounter);
            #endif
                rescueCounter = 0;
                setMode(RF69_MODE_STANDBY); // enables interrupts
                return true;
            }
            else
            {
                if (_mode == RF69_MODE_RX) // already in RX no payload yet
                {
                    this.input.EnableInterrupt();   // explicitly re-enable interrupts
                    return false;
                }
            }
            #if DebugPrint
                Debug.Print("Starting ReceiveBegin. Mode: " + _mode + " PAYLOADLEN: " + PAYLOADLEN.ToString());
            #endif
            receiveBegin();
            return false;
        }
        #endregion

        #region ReceiveLoop (in own thread)
        void runReceiveLoop()
        {
            Thread.Sleep(1000);
            long lastSendTime = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            long timeOutTicks;
            long sentTime;
            bool _ackReceived = false;

            bool receiveDoneRetTrue = false;
            MessageEntity actEntity;

            //TimeSpan StartTicks;
            //long Duration;
            UInt32 loopCount = 0;
            while (true)
            {
                loopCount++;
                // if there is something in the buffer >> send it out
                if ((QueueCount > 0) && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks - lastSendTime > 1000 * TimeSpan.TicksPerMillisecond)) // force a gap of 1000 ms between consecutive sends
                {
                    lock (LockQueueAccess)
                    {
                        actEntity = QueueDequeueNextMessageEntity();
                    }
                    for (int i = 0; i <= actEntity.Retries; i++)
                    {
                        Debug.Print(" TRY#" + (i + 1));
                    #if DebugPrint
                            Debug.Print(" TRY#" + (i + 1));
                    #endif

                        this.send(actEntity.Recipient, actEntity.PayLoad, (byte)actEntity.PayLoad.Length, requestACK: true);
                        lastSendTime = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

                        timeOutTicks = actEntity.WaitForACK_Time_MS * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

                        while (true)  // run the loop until ACK was received or timeOut (pRetryWaitTime_Ms) ended
                        {
                            if (ACKReceived(actEntity.Recipient))
                            {
                            #if DebugPrint
                                    Debug.Print("ACK received  ms: " + (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks - lastSendTime) / TimeSpan.TicksPerMillisecond);
                            #endif
                                _ackReceived = true;
                                OnACKReturned(this, new ACK_EventArgs(true, actEntity.PayLoad, SENDERID, actEntity.TimeOfMessage));
                                break;
                            }
                            if (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks > timeOutTicks)
                            {
                                _ackReceived = false;
                                if (_targetRSSI != 0)
                                {
                                    if (_transmitLevel < _maxPowerLevel) _transmitLevel++;
                                }
                                break;
                            }
                            Thread.Sleep(20);    // Seems to be important ( 10 or 20 ms), if it is to short the interrupt can come in a moment where interrupts are disabled
                        }
                        if (_ackReceived)
                        {
                            break;
                        }
                    }        // end of for (int i = 0; ...... loop
                    if (!_ackReceived)
                    {
                        OnACKReturned(this, new ACK_EventArgs(false, actEntity.PayLoad, 0xFF, actEntity.TimeOfMessage));
                    }
                    //Debug.Print("Retries finished");
                }   // end of: if Queue contains stuff to send  >> send

                // For "Debugging": Throws an event indicating that the Send/Receive Loop is running
                // can hold the loopCount (iteration) and another value for additional information
                // can be commented out if the information is not required in the main thread
                OnReceiveLoopIteration(this, new ReceiveLoopEventArgs(loopCount, 0));

                lock (LockReceiveDone)
                {
                    receiveDoneRetTrue = receiveDone();
                }
                if (receiveDoneRetTrue)
                {
                    byte[] dataBytes = new byte[DATALEN];
                    Array.Copy(DATA, dataBytes, DATALEN);
                    byte senderID = SENDERID;
                    //Debug.Print("[" + SENDERID.ToString() + "] " + new string(Encoding.UTF8.GetChars(dataBytes)) + "   [RX_RSSI: " + RSSI + "]");
                    //check if sender wanted an ACK 
                    if (ACKRequested())
                    {
                        sendACK();
                        //Debug.Print(" - ACK sent\r\n");
                        #if DebugPrint
                                Debug.Print(" - ACK sent");
                        #endif
                    }
                    this.OnMessageReceived(this, new MessageReceivedEventArgs(dataBytes, true, 0, label, location, measuredQuantity, destinationTable, senderID, _networkID, RSSI));
                    lock (LockReceiveDone)
                    {
                        receiveDone(); //put radio in RX mode
                    }

                }
                Thread.Sleep(10);
            }   // end of while(true) loop
        }
        #endregion

        #region public Class MessageEntity
        public class MessageEntity
        {
            byte[] _payLoad;
            DateTime _timeOfMessage;
            byte _senderID;
            byte _recipientID;
            byte _retries;
            short _waitForACK_Time_MS;

            public MessageEntity(byte[] pPayLoad, DateTime pTimeOfMessage, byte pSenderID, byte pRecipientID, byte pRetries, short pWaitForACK_Time_MS)
            {
                this._payLoad = pPayLoad;
                this._timeOfMessage = pTimeOfMessage;
                this._senderID = pSenderID;
                this._recipientID = pRecipientID;
                this._retries = pRetries;
                this._waitForACK_Time_MS = pWaitForACK_Time_MS;
            }

            public byte[] PayLoad
            {
                get { return this._payLoad; }
                set { this._payLoad = value; }
            }

            public DateTime TimeOfMessage
            {
                get { return this._timeOfMessage; }
                set { this._timeOfMessage = value; }
            }
            public byte SenderID
            {
                get { return this._senderID; }
                set { this._senderID = value; }
            }
            public byte Recipient
            {
                get { return this._recipientID; }
                set { this._recipientID = value; }
            }
            public byte Retries
            {
                get { return this._retries; }
                set { this._retries = value; }
            }
            public short WaitForACK_Time_MS
            {
                get { return this._waitForACK_Time_MS; }
                set { this._waitForACK_Time_MS = value; }
            }
        }
        #endregion

        #region public Class MessageReceivedEventArgs
        /// <summary>
        /// Event arguments for the signal received event.
        /// </summary>
        public class MessageReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// The Character Array with received data
            /// </summary>
            public byte[] receivedData { get; private set; }

            /// <summary>
            /// Signals that the received data are valid
            /// </summary>
            public bool messageIsValid { get; private set; }

            /// <summary>
            /// Contains the measured value
            /// </summary>
            /// 
            public int measuredValue { get; private set; }

            /// <summary>
            /// The time that the signal was received.
            /// </summary>
            public DateTime ReadTime { get; private set; }

            /// <summary>
            /// The Label or name of the sensor
            /// </summary>
            public string SensorLabel { get; private set; }

            /// <summary>
            /// The Location, where the sensor is located
            /// </summary>
            public string SensorLocation { get; private set; }

            /// <summary>
            /// The Physical Quantits of the measure value, e.g. Temperatur, humidity or pressure
            /// </summary>
            public string MeasuredQuantity { get; private set; }

            /// <summary>
            /// For optionally use: The name of e.g. a table where the values can be stored
            /// </summary>
            public string DestinationTable { get; private set; }

            /// <summary>
            /// The SenderID
            /// </summary>
            public byte senderID { get; private set; }

            /// <summary>
            /// The networkID
            /// </summary>
            public byte networkID { get; private set; }

            /// <summary>
            /// The sinal strength
            /// </summary>
            public short RSSI { get; private set; }


            internal MessageReceivedEventArgs(byte[] pReceivedData, bool pMessageIsValid, int pMeasuredValue,
                                     string pSensorLabel, string pSensorLocation, string pMeasuredQuantitiy,
                                     string pDestinationTable, byte pSenderID, byte pNetworkID, short pRSSI)
            {
                this.measuredValue = pMeasuredValue;
                this.receivedData = pReceivedData;
                this.messageIsValid = pMessageIsValid;
                this.ReadTime = DateTime.Now;
                this.SensorLabel = pSensorLabel;
                this.SensorLocation = pSensorLocation;
                this.MeasuredQuantity = pMeasuredQuantitiy;
                this.DestinationTable = pDestinationTable;
                this.senderID = pSenderID;
                this.networkID = pNetworkID;
                this.RSSI = pRSSI;
            }
        }
        #endregion

        #region public Class ACK_EventArgs
        /// <summary>
        /// Event arguments for the ACK received event.
        /// </summary>
        public class ACK_EventArgs : EventArgs
        {
            /// <summary>
            /// Signals that an ACK was received
            /// </summary>
            public bool ACK_Received { get; private set; }

            /// <summary>
            /// A Copy of the sent data
            /// </summary>
            public byte[] sentData { get; private set; }

            /// <summary>
            /// The address of the node which sent the ACK
            /// </summary>
            public byte senderOfTheACK { get; private set; }

            /// <summary>
            /// The time on which the message was sent
            /// </summary>
            public DateTime sendTime { get; private set; }

            internal ACK_EventArgs(bool pACK_Received, byte[] pSentData, byte pSenderOfTheACK, DateTime pSendTime)
            {
                this.ACK_Received = pACK_Received;
                this.sentData = pSentData;
                this.senderOfTheACK = pSenderOfTheACK;
                this.sendTime = pSendTime;
            }
        }
        #endregion

        #region public Class ReceiveLoopEventArgs
        /// <summary>
        /// Event arguments for the ReceiveLoopEvent event.
        /// </summary>
        public class ReceiveLoopEventArgs : EventArgs
        {
            public UInt32 Iteration { get; private set; }

            public UInt32 EventID { get; private set; }


            internal ReceiveLoopEventArgs(UInt32 pIteration, UInt32 pEventID)
            {
                this.Iteration = pIteration;
                this.EventID = pEventID;
            }
        }
        #endregion

        #region Delegate and Eventhandler ReceiveLoop
        /// <summary>
        /// The delegate that is used to show that the ReceiveLoop is running.
        /// </summary>
        /// <param name="sender">The <see cref="RFM69_Device"/> object that raised the event.</param>
        public delegate void ReceiveLoopEventHandler(RFM69_NETMF sender, ReceiveLoopEventArgs e);
        /// <summary>
        /// Raised for each iteration of the Receive Loop
        /// </summary>
        public event ReceiveLoopEventHandler ReceiveLoopIteration;

        private ReceiveLoopEventHandler onReceiveLoopIteration;

        private void OnReceiveLoopIteration(RFM69_NETMF sender, ReceiveLoopEventArgs e)
        {
            if (this.onReceiveLoopIteration == null)
            {
                this.onReceiveLoopIteration = this.OnReceiveLoopIteration;
            }
            this.ReceiveLoopIteration(sender, e);
        }
        #endregion

        #region Delegate and Eventhandler ACKReturned
        /// <summary>
        /// The delegate that is used to handle the ACK received event.
        /// </summary>
        /// <param name="sender">The <see cref="RFM69_Device"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        public delegate void ACKReturnedEventHandler(RFM69_NETMF sender, ACK_EventArgs e);

        /// <summary>
        /// Raised when the module detects an 433 Mhz signal.
        /// </summary>
        public event ACKReturnedEventHandler ACKReturned;

        private ACKReturnedEventHandler onACKReturned;

        private void OnACKReturned(RFM69_NETMF sender, ACK_EventArgs e)
        {
            if (this.onACKReturned == null)
            {
                this.onACKReturned = this.OnACKReturned;
            }
            this.ACKReturned(sender, e);
        }
        #endregion

        #region Delegate and Eventhandler MessageReceived
        /// <summary>
        /// The delegate that is used to handle the event.
        /// </summary>
        /// <param name="sender">The <see cref="RFM69_Device"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        public delegate void MessageReceivedEventHandler(RFM69_NETMF sender, MessageReceivedEventArgs e);

        /// <summary>
        /// Raised when the module detects an 433 Mhz signal.
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        private MessageReceivedEventHandler onMessageReceived;

        private void OnMessageReceived(RFM69_NETMF sender, MessageReceivedEventArgs e)
        {
            if (this.onMessageReceived == null)
            {
                this.onMessageReceived = this.OnMessageReceived;
            }
            //if (GT.Program.CheckAndInvoke(this.MessageReceived, this.onMessageReceived, sender, e))
            //    this.MessageReceived(sender, e);
            this.MessageReceived(sender, e);
        }
        #endregion

        #region public Method setPowerLevel
        // set *transmit/TX* output power: 0=min, 31=max
        // this results in a "weaker" transmitted signal, and directly results in a lower RSSI at the receiver
        // the power configurations are explained in the SX1231H datasheet (Table 10 on p21; RegPaLevel p66): http://www.semtech.com/images/datasheet/sx1231h.pdf
        // valid powerLevel parameter values are 0-31 and result in a directly proportional effect on the output/transmission power
        // this function implements 2 modes as follows:
        //       - for RFM69W the range is from 0-31 [-18dBm to 13dBm] (PA0 only on RFIO pin)
        //       - for RFM69HW the range is from 0-31 [5dBm to 20dBm]  (PA1 & PA2 on PA_BOOST pin & high Power PA settings - see section 3.3.7 in datasheet, p22)
        public void setPowerLevel(byte powerLevel, byte maxPowerLevel = 31)
        {
            _maxPowerLevel = (maxPowerLevel > 31 ? 31 : maxPowerLevel);
            _powerLevel = (powerLevel > 31 ? 31 : powerLevel);

            _transmitLevel = (byte)_powerLevel;
            if (_isRFM69HW)
            {
                _powerLevel /= 2;
            }
            writeReg(REG_PALEVEL, (byte)((byte)(readReg(REG_PALEVEL) & 0xE0) | _powerLevel));
        }
        #endregion

        #region public Method setHighPower
        public void setHighPower(bool onOff)
        {
            _isRFM69HW = onOff;
            writeReg(REG_OCP, _isRFM69HW ? RF_OCP_OFF : RF_OCP_ON);   //RF_OCP_OFF = 0x0F; RF_OCP_ON = 0x1A; (Default)

            if (_isRFM69HW) // turning ON
            {
                writeReg(REG_PALEVEL, (byte)((byte)(readReg(REG_PALEVEL) & 0x1F) | RF_PALEVEL_PA1_ON | RF_PALEVEL_PA2_ON)); // enable P1 & P2 amplifier stages
            }
            else
            {
                writeReg(REG_PALEVEL, (byte)(RF_PALEVEL_PA0_ON | RF_PALEVEL_PA1_OFF | RF_PALEVEL_PA2_OFF | _powerLevel)); // enable P0 only
            }
        }
        #endregion

        #region public Method encrypt

        /// <summary>encrypt</summary>
        /// <param name="key">Defines the encryption key. null means: No encryption. Otherweise: KEY HAS TO BE 16 bytes !!! </param>
        ///
        public void encrypt(string key)
        {
            setMode(RF69_MODE_STANDBY);
            if ((key != null) && (key.Length != 0))
            {
                if (key.Length != 16)
                {
                    throw new Exception("Encryption key must have a length of 16 characters");
                }
                this.input.DisableInterrupt();

                for (int i = 0; i < key.Length; i++)
                {
                    writeReg((byte)(REG_AESKEY1 + i), (byte)key[i]);
                }
                this.input.EnableInterrupt();
                writeReg(REG_PACKETCONFIG2, (byte)((byte)((byte)readReg(REG_PACKETCONFIG2) & 0xFE) | 0x01));
            }
            else
            {
                writeReg(REG_PACKETCONFIG2, (byte)((byte)((byte)readReg(REG_PACKETCONFIG2) & 0xFE) | 0x00));
            }
            //Debug.Print("The Content of REG_PACKETCONFIG2 is : " + readReg(REG_PACKETCONFIG2).ToString("X2"));
        }
        #endregion

        #region public Method setMode
        public void setMode(byte newMode)
        {
            // Changed by RoSchmi to avoid possible hangs
            if (newMode == _mode)
            {
                setModeLoopCtr++;
                if (setModeLoopCtr < 10)    // every 10th iteration we do not return if the mode did not change but force setMode
                {                            // this is to prevent hangs if the variable _mode is already falsely set to RX mode, but the RFM69 is not
                    return;
                }
                else
                {
                    setModeLoopCtr = 0;
                }
            }

            switch (newMode)
            {
                case RF69_MODE_TX:
                    writeReg(REG_OPMODE, (byte)((byte)(readReg(REG_OPMODE) & 0xE3) | RF_OPMODE_TRANSMITTER));
                    if (_isRFM69HW) setHighPowerRegs(true);
                    break;
                case RF69_MODE_RX:
                    writeReg(REG_OPMODE, (byte)((byte)(readReg(REG_OPMODE) & 0xE3) | RF_OPMODE_RECEIVER));
                    if (_isRFM69HW) setHighPowerRegs(false);
                    break;
                case RF69_MODE_SYNTH:
                    writeReg(REG_OPMODE, (byte)((byte)(readReg(REG_OPMODE) & 0xE3) | RF_OPMODE_SYNTHESIZER));
                    break;
                case RF69_MODE_STANDBY:
                    writeReg(REG_OPMODE, (byte)((byte)(readReg(REG_OPMODE) & 0xE3) | RF_OPMODE_STANDBY));
                    break;
                case RF69_MODE_SLEEP:
                    writeReg(REG_OPMODE, (byte)((byte)(readReg(REG_OPMODE) & 0xE3) | RF_OPMODE_SLEEP));
                    break;
                default:
                    return;
            }
            // we are using packet mode, so this check is not really needed
            // but waiting for mode ready is necessary when going from sleep because the FIFO may not be immediately available from previous mode

            while ((readReg(REG_IRQFLAGS1) & RF_IRQFLAGS1_MODEREADY) == 0x00) ; // wait for ModeReady

            //while (_mode == RF69_MODE_SLEEP && (readReg(REG_IRQFLAGS1) & RF_IRQFLAGS1_MODEREADY) == 0x00) ; // wait for ModeReady

            _mode = newMode;

            if (newMode == RF69_MODE_TX)
            {
                if (_targetRSSI != 0)   // Needed only for ATC
                {
                    setPowerLevel(_transmitLevel);   // TomWS1: apply most recent transmit level if auto power
                }
                //if (_isRFM69HW) { setHighPowerRegs(true); }
            }


        }
        #endregion

        #region public Method readTemperature(..)
        public int readTemperature(byte calFactor) // returns centigrade 
        {
            setMode(RF69_MODE_STANDBY);
            writeReg(REG_TEMP1, RF_TEMP1_MEAS_START);
            while ((readReg(REG_TEMP1) & RF_TEMP1_MEAS_RUNNING) == 0) ;
            return ~readReg(REG_TEMP2) + COURSE_TEMP_COEF + calFactor; // 'complement' corrects the slope, rising temp = rising val 
        } // COURSE_TEMP_COEF puts reading in the ballpark, user can add additional correction 
        #endregion

        #region public Method rcCalibration()
        public void rcCalibration()
        {
            writeReg(REG_OSC1, RF_OSC1_RCCAL_START);
            while ((readReg(REG_OSC1) & RF_OSC1_RCCAL_DONE) != 0x00) ;
        }
        #endregion

        #region internal Method setHighPowerRegs (TWS)
        // internal method
        void setHighPowerRegs(bool onOff)
        {
            if (onOff == true)
            {
                writeReg(REG_TESTPA1, 0x5D);
                writeReg(REG_TESTPA2, 0x7C);
            }
            else
            {
                writeReg(REG_TESTPA1, 0x55);
                writeReg(REG_TESTPA2, 0x70);
            }
            //writeReg(REG_TESTPA1, onOff ? 0x5D : 0x55);
            //writeReg(REG_TESTPA2, onOff ? 0x7C : 0x70);
        }
        #endregion

        #region internal Method SPI: writeReg(..)
        /// <summary>SPI: Writes one byte to a register of the RFM69</summary>
        /// <param name="addr">Address of the RFM69 register</param>
        /// <param name="value">Byte that is to be written to the register</param>
        ///
        private void writeReg(byte addr, byte value)
        {
            byte[] command = new byte[2] { (byte)(addr | 0x80), value };
            spi.Write(command);
        }
        #endregion

        #region internal Method SPI: readReg(..)
        /// <summary>SPI: Reads one byte from a register of the RFM69</summary>
        /// <param name="addr">Address of the RFM69 register</param>
        ///
        private byte readReg(byte addr)
        {
            byte command = (byte)(addr & 0x7F);
            byte[] writeBuffer = new byte[2];
            writeBuffer[0] = command;
            byte[] readBuffer = new byte[2] { 0x00, 0x00 };
            spi.WriteRead(writeBuffer, readBuffer);
            return readBuffer[1];
        }
        #endregion

        #region internal Method SPI: SPIWriteData
        private void SPIWriteData(byte data)
        {
            this.writeByteArray = new byte[] { data };
            this.SPIWriteData(this.writeByteArray);
        }

        private void SPIWriteData(byte[] data)
        {
            this.spi.Write(data);
        }

        private void SPIWriteData(ushort[] data)
        {
            this.spi.Write(data);
        }
        #endregion

        #region internal Method SPI: SPIWriteReadData(..) byte or byte Array
        private byte SPIWriteReadData(byte data)
        {
            this.writeByteArray = new byte[] { data };
            return SPIWriteReadData(this.writeByteArray)[0];
        }
        private byte[] SPIWriteReadData(byte[] data)
        {
            this.readByteArray = new byte[data.Length];
            this.spi.WriteRead(data, this.readByteArray);
            return this.readByteArray;
        }
        #endregion

        #region internal Method receiveBegin()
        // internal function
        void receiveBegin()
        {
            if (_targetRSSI != 0)     // for ATC
            {
                ACK_RSSI_REQUESTED = 0;
            }

            //REG_IRQFLAGS2:
            DATALEN = 0;                            // 7 FifoFull
            SENDERID = 0;                           // 6 Fifo not empty
            TARGETID = 0;                           // 5 FifoLevel
            PAYLOADLEN = 0;                         // 4 Fifo Overrun
            ACK_REQUESTED = 0;                      // 3 PacketSent
            ACK_RECEIVED = 0;                       // 2 PayloadReady
            RSSI = 0;                               // 1 CrcOk
            // 0 unused
            if ((byte)(readReg(REG_IRQFLAGS2) & RF_IRQFLAGS2_PAYLOADREADY) != 0)          // if PayLoadReady
            {
                writeReg(REG_PACKETCONFIG2, (byte)((byte)((byte)readReg(REG_PACKETCONFIG2) & 0xFB) | RF_PACKET2_RXRESTART)); // avoid RX deadlocks  //forces the receiver in WAIT mode, in Continuous Rx mode
            }
            // REG_DIOMAPPING1 = Reg 0x25  (Mapping of pins DIO0 - DIO3
            // RF_DIOMAPPING1_DIO0_01 = 0x40  = b 0100|0000
            // Bit 7-6 of REG_DIOMAPPING1 make the assignment of Pin DIO0
            // in RX-Mode 01 in bits 7-6 make that Pin DI0 represents PayloadReady
            byte Diomapping1Readback = 0;
            int loopCtr = 0;
            while ((Diomapping1Readback != RF_DIOMAPPING1_DIO0_01) && loopCtr < 3)    // Write to Register and read back to be sure that it arrived (3x)
            {
                loopCtr++;
                writeReg(REG_DIOMAPPING1, RF_DIOMAPPING1_DIO0_01);   // set Pin DIO0 to "PAYLOADREADY" in receive mode
                Diomapping1Readback = readReg(REG_DIOMAPPING1);
                if (Diomapping1Readback != RF_DIOMAPPING1_DIO0_01)
                {
                    //Debug.Print("RF_DIOMAPPING1_DIO0_01 was written falsely");
                }
            }

            byte RF69_MODE_RX_Readback = 0;
            loopCtr = 0;
            while ((RF69_MODE_RX_Readback != 16) && loopCtr < 3)                      // Write to Register and read back to be sure that it arrived (3x)
            {
                loopCtr++;
                setMode(RF69_MODE_RX);
                RF69_MODE_RX_Readback = readReg(REG_OPMODE);
            }

            // input.EnableInterrupt added by RoSchmi
            this.input.EnableInterrupt();
        }
        #endregion

        #region public Method ACKRequested()
        // check whether an ACK was requested in the last received packet (non-broadcasted packet)
        public bool ACKRequested()
        {
            return ((ACK_REQUESTED != 0) && (TARGETID != RF69_BROADCAST_ADDR));
        }
        #endregion

        #region internal Method ACKReceived(..)
        // should be polled immediately after sending a packet with ACK request
        bool ACKReceived(byte fromNodeID)
        {
            //Debug.Print("ACKReceived was called");
            bool receiveDoneTrue;
            lock (LockReceiveDone)
            {
                receiveDoneTrue = receiveDone();
            }
            if (receiveDoneTrue)
            {
                if (ACK_RECEIVED != 0)
                {
                    if ((SENDERID == fromNodeID) || (fromNodeID == RF69_BROADCAST_ADDR))
                    {
                        //Debug.Print("      ACKReceived returned true");
                        return true;
                    }
                }
            }
            //Debug.Print("      ACKReceived returned false");
            return false;
        }
        #endregion

        #region public Method sendACK(..)
        public void sendACK(byte[] buffer = null, byte bufferSize = 0)
        {
            ACK_REQUESTED = 0;   // TWS added to make sure we don't end up in a timing race and infinite loop sending Acks
            byte sender = SENDERID;
            short _RSSI = RSSI; //save payload received RSSI value
            bool sendRSSI = false;
            if (_targetRSSI != 0)
            {
                sendRSSI = (ACK_RSSI_REQUESTED != 0);  // for ATC
            }
            writeReg(REG_PACKETCONFIG2, (byte)((byte)(readReg(REG_PACKETCONFIG2) & 0xFB) | RF_PACKET2_RXRESTART)); // avoid RX deadlocks
            setMode(RF69_MODE_RX); //Switching from STANDBY to RX before TX

            long timeOutTicks = RF69_CSMA_LIMIT_MS * TimeSpan.TicksPerMillisecond + Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            while (true)
            {
                if (canSendACK())
                {
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT - 1;
                    _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT < -90 ? -90 : _adjusted_CSMA_LIMIT;
                #if DebugPrint
                        Debug.Print("In sendACK: Exit with can send");
                #endif
                    break;
                }
                _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT + 2;
                _adjusted_CSMA_LIMIT = _adjusted_CSMA_LIMIT > -60 ? -60 : _adjusted_CSMA_LIMIT;
                if (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks > timeOutTicks)
                {
                #if DebugPrint
                        Debug.Print("TimeOut reached");
                #endif
                    break;
                }
                // Changed by RoSchmi
                setMode(RF69_MODE_STANDBY);
                lock (LockReceiveDone)
                {
                    receiveDone();
                }
            }
            sendFrame(sender, buffer, bufferSize, false, true, sendRSSI, _RSSI);
            Thread.Sleep(0);
            SENDERID = sender;
            RSSI = _RSSI; //restore payload RSSI
        }
        #endregion

        #region internal Method canSend()
        private bool canSend()
        {
            Int16 theRSSI = readRSSI(false);
            // for debugging
            #if DebugPrint
                bool RssiLevelLowEnough = theRSSI < _adjusted_CSMA_LIMIT;
                Debug.Print("RssiLevelLowEnough: " + RssiLevelLowEnough);
                Debug.Print("PayLoadLength:  " + PAYLOADLEN);
                Debug.Print("_mode:  " + _mode); 
            #endif

            if (((_mode == RF69_MODE_RX) && (PAYLOADLEN == 0)) && (theRSSI < _adjusted_CSMA_LIMIT)) // if signal stronger than -90dBm is detected assume channel activity
            {
                setMode(RF69_MODE_STANDBY);
                #if DebugPrint
                    Debug.Print("Can send, RSSI = " + theRSSI + " Limit is: " + _adjusted_CSMA_LIMIT);
                #endif
                return true;
            }
            #if DebugPrint
                Debug.Print("Can not send, RSSI = " + theRSSI + " Limit is: " + _adjusted_CSMA_LIMIT);
            #endif
            return false;
        }
        #endregion

        #region internal Method canSendACK
        private bool canSendACK()
        {
            Int16 theRSSI = readRSSI(false);
            // for debugging
            #if DebugPrint
                //bool RssiLevelLowEnough = theRSSI < _adjusted_CSMA_LIMIT;
                //Debug.Print("RssiLevelLowEnough: " + RssiLevelLowEnough);
                //Debug.Print("PayLoadLength:  " + PAYLOADLEN);
                //Debug.Print("_mode:  " + _mode);
            #endif

            if ((_mode == RF69_MODE_RX) && (theRSSI < _adjusted_CSMA_LIMIT)) // if signal stronger than e.g. -90dBm is detected assume channel activity
            {
                setMode(RF69_MODE_STANDBY);
                #if DebugPrint
                    Debug.Print("Can send ACK, RSSI = " + theRSSI + " Limit is: " + _adjusted_CSMA_LIMIT);
                #endif
                return true;
            }
            #if DebugPrint
                Debug.Print("Can not send ACC, RSSI = " + theRSSI + " Limit is: " + _adjusted_CSMA_LIMIT);
            #endif
            return false;
        }
        #endregion
    }
}