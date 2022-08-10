// Copyright RoSchmi 07. Sept. 2017
// Lisense Apache 2

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.Processor;
using GHI.Pins;

using System.Threading;

namespace RoSchmi.ButtonNETMF
{
    public class ButtonNETMF
    {
        InterruptPort input;
        OutputPort led;
        private LedMode currentMode;

        private ButtonEventHandler onButtonEvent;

        /// <summary>Represents the delegate that is used to handle the <see cref="ButtonReleased" /> and <see cref="ButtonPressed" /> events.</summary>
        /// <param name="sender">The <see cref="Button" /> object that raised the event.</param>
        /// <param name="state">The state of the Button</param>
        public delegate void ButtonEventHandler(ButtonNETMF sender, ButtonState state);

        /// <summary>Raised when the button is released.</summary>
        public event ButtonEventHandler ButtonReleased;

        /// <summary>Raised when the button is pressed.</summary>
        public event ButtonEventHandler ButtonPressed;

        /// <summary>Whether or not the button is pressed.</summary>
        public bool Pressed
        {
            get
            {
                return !this.input.Read();
            }
        }

        /// <summary>Whether or not the LED is currently on or off.</summary>
        public bool IsLedOn
        {
            get
            {
                return this.led.Read();
            }
        }

        /// <summary>Gets or sets the LED's current mode of operation.</summary>
        public LedMode Mode
        {
            get
            {
                return this.currentMode;
            }

            set
            {
                this.currentMode = value;

                if (this.currentMode == LedMode.On || (this.currentMode == LedMode.OnWhilePressed && this.Pressed) || (this.currentMode == LedMode.OnWhileReleased && !this.Pressed))
                    this.TurnLedOn();
                else if (this.currentMode == LedMode.Off || (this.currentMode == LedMode.OnWhileReleased && this.Pressed) || (this.currentMode == LedMode.OnWhilePressed && !this.Pressed))
                    this.TurnLedOff();
            }
        }

        /// <summary>The state of the button.</summary>
        public enum ButtonState
        {

            /// <summary>The button is pressed.</summary>
            Pressed = 0,

            /// <summary>The button is released.</summary>
            Released = 1
        }

        /// <summary>The various modes a LED can be set to.</summary>
        public enum LedMode
        {

            /// <summary>The LED is on regardless of the button state.</summary>
            On,

            /// <summary>The LED is off regardless of the button state.</summary>
            Off,

            /// <summary>The LED changes state whenever the button is pressed.</summary>
            ToggleWhenPressed,

            /// <summary>The LED changes state whenever the button is released.</summary>
            ToggleWhenReleased,

            /// <summary>The LED is on while the button is pressed.</summary>
            OnWhilePressed,

            /// <summary>The LED is on except when the button is pressed.</summary>
            OnWhileReleased
        }

        /// <summary>Constructs a new instance.</summary>
		
        //public ButtonNETMF(Cpu.Pin intPort, Cpu.Pin buttonLED)

        /*
        public ButtonNETMF(GHI.Processor.DeviceType deviceType, int socketNumber)
        {
            this.currentMode = LedMode.Off;

            Cpu.Pin _inputPort = Cpu.Pin.GPIO_Pin13;
            Cpu.Pin _outputPort = Cpu.Pin.GPIO_Pin14;

            this.input = new InterruptPort(_inputPort, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            this.led = new OutputPort(_outputPort, false);

            switch (deviceType)
            {
                case GHI.Processor.DeviceType.G120E:
                    {
                        switch (socketNumber)
                        {
                            case 12:
                                {
                                    _inputPort = FEZSpiderII.Socket12.Pin3;
                                    _outputPort = FEZSpiderII.Socket12.Pin4;
                                }
                                break;
                            
                            default:
                                {
                                    throw new NotSupportedException("Socket not supported");
                                    break;
                                }
                                
                        }
                    }
                    break;
                default:
                    {
                        throw new NotSupportedException("Mainboard not supported");
                        break;
                    }
            }

            
                    
            /*
            if (deviceType == DeviceType.G120E)
            {
                _inputPort = FEZSpiderII.Socket12.Pin3;
                _outputPort = FEZSpiderII.Socket12.Pin4;
            }
            */

            //this.input = new InterruptPort(FEZSpiderII.Socket12.Pin3, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            //this.input = new InterruptPort(_inputPort, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            //this.led = new OutputPort(_outputPort, false);
            

    

            //this.input = new InterruptPort(FEZSpiderII.Socket12.Pin3, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            //this.led = new OutputPort(FEZSpiderII.Socket12.Pin4, false);


           

            /*
            switch (deviceType)
            {
                case GHI.Processor.DeviceType.G120E:
                {
                    switch (socketNumber)
                    {
                        case 12:
                            {
                                this.input = new InterruptPort(_inputPort, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
                                this.led = new OutputPort(_outputPort, false);
                            }
                            break;
                            default:
                            {
                                throw new NotSupportedException("Socket not supported");
                            }
                    }
                }
                break;
                case GHI.Processor.DeviceType.EMX:
                {
                    switch (socketNumber)
                    {
                        case 12:
                            {
                                this.input = new InterruptPort(FEZSpider.Socket12.Pin3, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
                                this.led = new OutputPort(GHI.Pins.FEZSpider.Socket12.Pin4, false);
                            }
                            break;
                        default:
                            {
                                throw new NotSupportedException("Socket not supported");
                            }
                    }
                }
                break;


                default:
                {
                    throw new NotSupportedException("Mainboard not supported");
                }
            }
            */


           // this.input.OnInterrupt += input_OnInterrupt;
       // }
        


        
        public ButtonNETMF(Cpu.Pin intPort, Cpu.Pin buttonLED)
        {
            this.currentMode = LedMode.Off;
            this.input = new InterruptPort(intPort, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            this.led = new OutputPort(buttonLED, false);
            this.input.OnInterrupt += input_OnInterrupt;
        }
        
        void input_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            bool value = data2 != 0;
            ButtonState state = value ? ButtonState.Pressed: ButtonState.Released;

            switch (state)
            {
                case ButtonState.Released:
                    if (this.Mode == LedMode.OnWhilePressed)
                        this.TurnLedOff();
                    else if (this.Mode == LedMode.OnWhileReleased)
                        this.TurnLedOn();
                    else if (this.Mode == LedMode.ToggleWhenReleased)
                        this.ToggleLED();

                    break;

                case ButtonState.Pressed:
                    if (this.Mode == LedMode.OnWhilePressed)
                        this.TurnLedOn();
                    else if (this.Mode == LedMode.OnWhileReleased)
                        this.TurnLedOff();
                    else if (this.Mode == LedMode.ToggleWhenPressed)
                        this.ToggleLED();

                    break;
            }

            this.OnButtonEvent(this, state);
        }

        /// <summary>Turns on the LED.</summary>
        public void TurnLedOn()
        {
            this.led.Write(true);
        }

        /// <summary>Turns off the LED.</summary>
        public void TurnLedOff()
        {
            this.led.Write(false);
        }

        /// <summary>Turns the LED off if it is on and on if it is off.</summary>
        public void ToggleLED()
        {
            if (this.IsLedOn)
                this.TurnLedOff();
            else
                this.TurnLedOn();
        }

        private void OnButtonEvent(ButtonNETMF sender, ButtonState state)
        {
            if (this.onButtonEvent == null)
                this.onButtonEvent = this.OnButtonEvent;

            if (state == ButtonState.Pressed)
            {
                if (this.ButtonPressed != null)
                {
                    this.ButtonPressed(sender, state);
                }
            }
            else
            {
                if (this.ButtonReleased != null)
                {
                    this.ButtonReleased(sender, state);
                }
            }
        }
    }
}

