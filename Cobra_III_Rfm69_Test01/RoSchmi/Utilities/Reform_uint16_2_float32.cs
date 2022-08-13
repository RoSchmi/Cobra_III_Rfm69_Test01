using System;
using Microsoft.SPOT;

namespace RoSchmi.Utilities
{
    
    public static class Reform_uint16_2_float32
    {

        public static float Convert(UInt16 pU1, UInt16 pU2)
        {
            float result = 0;
            UInt16 u1 = pU1;
            UInt16 u2 = pU2;

            // This is an example for value: 240.50
            //u1 = 0x4370;
            //u2 = 0x8000;



            // sign 0 is positive, 1 is negativ        
            bool sign = ((uint)u1 & 0x8000) == 0;

            UInt16 expon = 0;
            Int32 finalExponent = 0;


            UInt16 maskedExponent = (UInt16)(u1 & 0x7F80);
            expon = (UInt16)(maskedExponent >> 7);
            finalExponent = (Int32)expon - 127;

            if (finalExponent > 19)
            {
                throw new ArgumentOutOfRangeException("Exception: Value was to large");
            }

            UInt32 u_32 = (UInt32)(u1 * 65536) + u2;
            result = BitConverter.ToSingle(BitConverter.GetBytes((int)u_32), 0);
            return result;
        }

    }
}


        /*
        public struct int16_2_float_function_result
        {
            public bool validity;         
            public float value;           
        }


        int16_2_float_function_result reform_uint16_2_float32(UInt16 pU1, UInt16 pU2)
        {
            float result = 0; 
            UInt16 u1 = pU1;
            UInt16 u2 = pU2;

            int16_2_float_function_result resultStruct;
                      
            // This is an example for value: 240.50
            //u1 = 0x4370;
            //u2 = 0x8000;
     
            // sign 0 is positive, 1 is negativ        
            bool sign = ((UInt32)u1 & 0x8000) == 0;
            
            UInt16 expon = 0;
            Int32 finalExponent = 0;
            UInt16 maskedExponent = (UInt16)(u1 & 0x7F80);           
            expon = (UInt16)(maskedExponent >> 7);
            finalExponent = (Int32)expon - 127;

            if (finalExponent > 19)
            {         
                resultStruct.validity = false;
                resultStruct.value = 0;
                return resultStruct;            
            }

            // This is the ugly way in C
            
            if (finalExponent >= -8)     // function returns not valid results for finalExponent < -8
            {
                uint32_t mantissaLow = u2;
                uint32_t mantissaHigh = (((uint32_t)u1 & 0x007F) & 0xFF7F);  // set higher 8 bits to zero, set the following bit to one
                uint32_t mantissaHighShifted = mantissaHigh << 16;       // shift 16 bits left so that the lower part can be added
                uint32_t mantissa = (mantissaHighShifted + mantissaLow) | 0x800000;   // add higher 16 bits and lower 16 bits, set highest bit to one

                uint32_t preDecPoint = (uint32_t)mantissa >> (23 - finalExponent);
                int32_t indexFirstPastPointDigit = 23 - finalExponent - 1;
                uint32_t maskBit = (uint32_t)pow(2, indexFirstPastPointDigit);
                double pastDecPoint = 0;
                for (int32_t i = 0; i <= (int32_t)indexFirstPastPointDigit; i++)
                {
                    if ((mantissa & maskBit) != 0)
                    {
                        pastDecPoint += 1.0 / (pow(2, i + 1));
                    }
                    maskBit = maskBit >> 1;
                }
                result = (float)(preDecPoint + pastDecPoint);
                if (sign != true)
                {
                    result = result * -1;
                }
                resultStruct.value = result;    
                resultStruct.validity = true;
                return resultStruct;
            }
            else
            {               
                resultStruct.value = 0;    
                resultStruct.validity = true;
                return resultStruct;               
            }

    }
}
*/