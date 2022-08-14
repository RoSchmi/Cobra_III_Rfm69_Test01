using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    /// <summary>
    /// Event arguments for the signal received event.
    /// </summary>
    /// 
    public class SignalReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// The Character Array with received data
            /// </summary>
            public Char[] receivedData { get; private set; }

            /// <summary>
            /// Signals that the received data are valid
            /// </summary>
            public bool signalIsValid { get; private set; }

            /// <summary>
            /// Contains the measured value
            /// </summary>
            /// 
            public int measuredValue { get; private set; }


            /// <summary>
            /// Gives the number of the repetition with the first valid result
            /// </summary>
            /// 
            public int repCount { get; private set; }
            
            public int failedBitsCount { get; private set; }

            /// <summary>
            /// Gives the number of noise spikes which were eliminated before a valid result was received
            /// </summary>
            public int eliminatedSpikesCount { get; private set; }

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
            /// The Channel on which the sensor is sending (not yet used)
            /// </summary>
            public string Channel { get; private set; }

            /// <summary>
            /// The RSSI (Signal Strength)
            /// </summary>
            public int RSSI { get; private set; }


            
            internal SignalReceivedEventArgs(Char[] pReceivedData, bool pSignalIsValid, int pMeasuredValue, DateTime pReadTime,
                                             int pRepetitionCount, int pFailedBitsCount, int pEliminatedSpikesCount,
                                             string pSensorLabel, string pSensorLocation, string pMeasuredQuantitiy,
                                             string pDestinationTable, string pChannel, int pRSSI)
            {
                this.measuredValue = pMeasuredValue;
                this.repCount = pRepetitionCount;
                this.receivedData = pReceivedData;
                this.signalIsValid = pSignalIsValid;
                this.failedBitsCount = pFailedBitsCount;
                this.eliminatedSpikesCount = pEliminatedSpikesCount;
                this.ReadTime = pReadTime;
                this.SensorLabel = pSensorLabel;
                this.SensorLocation = pSensorLocation;
                this.MeasuredQuantity = pMeasuredQuantitiy;
                this.DestinationTable = pDestinationTable;
                this.Channel = pChannel;
                this.RSSI = pRSSI;
            }   
    }
}

