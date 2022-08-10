using System;
using Microsoft.SPOT;

//namespace HeatingSurvey
namespace  Cobra_III_Rfm69_Test01
{
    public class OnOffBaseSensorMgr
    {
        /// <summary>
        /// Represents the state of the <see cref="InputSensor"/> object.
        /// </summary>
        public enum InputSensorState
        {
            /// <summary>
            /// The state of InputSensor is low.
            /// </summary>
            Low = 0,
            /// <summary>
            /// The state of InputSensor is high.
            /// </summary>
            High = 1
        }

        internal string SensorLabel { get; set; }                // Label or Name of the sensor, is transmitted in the eventargs
        internal string SensorLocation { get; set; }             // Location of the sensor, is transmitted in the eventargs
        internal string MeasuredQuantity { get; set; }           // Physical quantity of the measured value e.g. Temperature, Humidity, Pressure
        internal string DestinationTable { get; set; }           // Optionally defines the name of e.g. a table, where the readings can evtl. be stored
        internal string Channel { get; set; }                    // Optionally defines the channel on which the sensor is sending (not yet used)

        internal int dstOffset = 0;
        internal string dstStart = string.Empty;
        internal string dstEnd = string.Empty;

        internal bool _stopped = true;

        internal InputSensorState actState = InputSensorState.Low;
        internal InputSensorState oldState = InputSensorState.Low;



        #region OnOffSensorEventArgs
        /// <summary>
        /// Event arguments for the AzureSend event
        /// </summary>
        public class OnOffSensorEventArgs : EventArgs
        {
            /// <summary>
            /// State of the message
            /// </summary>
            /// 
            public bool ActState
            { get; private set; }

            /// <summary>
            /// Former State of the message
            /// </summary>
            /// 
            public bool OldState
            { get; private set; }

            /// <summary>
            /// RepaeatCount to succeed
            /// </summary>
            /// 
            public UInt16 RepeatSend
            { get; private set; }


            /// <summary>
            /// Timestamp
            /// </summary>
            /// 
            public DateTime Timestamp
            { get; private set; }

            /// <summary>
            /// SensorLabel
            /// </summary>
            /// 
            public string SensorLabel
            { get; private set; }

            /// <summary>
            /// SensorLocation
            /// </summary>
            /// 
            public string SensorLocation
            { get; private set; }

            /// <summary>
            /// MeasuredQuantity
            /// </summary>
            /// 
            public string MeasuredQuantity
            { get; private set; }

            /// <summary>
            /// DestinationTable
            /// </summary>
            /// 
            public string DestinationTable
            { get; private set; }

            /// <summary>
            /// Channel
            /// </summary>
            /// 
            public string Channel
            { get; private set; }

            /// <summary>
            /// LastOfDay
            /// </summary>
            /// 
            public bool LastOfDay
            { get; private set; }

             
            /// <summary>
            /// Val_1
            /// </summary>
            /// 
            public UInt32 Val_1
            { get; private set; }

            /// <summary>
            /// Val_2
            /// </summary>
            /// 
            public UInt32 Val_2
            { get; private set; }


            /// <summary>
            /// Val_3
            /// </summary>
            /// 
            public UInt32 Val_3
            { get; private set; }

            

            internal OnOffSensorEventArgs(InputSensorState pActState, InputSensorState pOldState, UInt16 pRepeatSend, DateTime pTimeStamp, string pSensorLabel, string pSensorLocation, string pMeasuredQuantity, string pDestinationTable, string pChannel, bool pLastOfDay, UInt32 pVal_1 = 0, UInt32 pVal_2 = 0, UInt32 pVal_3 = 0)
            {
                this.ActState = pActState == InputSensorState.High ? true : false;
                this.OldState = pOldState == InputSensorState.High ? true : false;
                this.RepeatSend = pRepeatSend;
                this.Timestamp = pTimeStamp;
                this.DestinationTable = pDestinationTable;
                this.MeasuredQuantity = pMeasuredQuantity;
                this.SensorLabel = pSensorLabel;
                this.SensorLocation = pSensorLocation;
                this.Channel = pChannel;
                this.LastOfDay = pLastOfDay;
                this.Val_1 = pVal_1;
                this.Val_2 = pVal_2;
                this.Val_3 = pVal_3;               
            }
        }
        #endregion
    }
}
