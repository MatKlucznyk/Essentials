﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Endpoints;
using Crestron.SimplSharpPro.DM.Endpoints.Transmitters;

using PepperDash.Core;


namespace PepperDash.Essentials.Core
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class DisplayBase : Device, IHasFeedback, IRoutingSinkWithSwitching, IPower, IWarmingCooling, IUsageTracking
	{
        public event SourceInfoChangeHandler CurrentSourceChange;

        public string CurrentSourceInfoKey { get; set; }
        public SourceListItem CurrentSourceInfo
        {
            get
            {
                return _CurrentSourceInfo;
            }
            set
            {
                if (value == _CurrentSourceInfo) return;

                var handler = CurrentSourceChange;

                if (handler != null)
                    handler(_CurrentSourceInfo, ChangeType.WillChange);

                _CurrentSourceInfo = value;

                if (handler != null)
                    handler(_CurrentSourceInfo, ChangeType.DidChange);
            }
        }
        SourceListItem _CurrentSourceInfo;

		public BoolFeedback PowerIsOnFeedback { get; protected set; }
		public BoolFeedback IsCoolingDownFeedback { get; protected set; }
		public BoolFeedback IsWarmingUpFeedback { get; private set; }

        public UsageTracking UsageTracker { get; set; }

		public uint WarmupTime { get; set; }
		public uint CooldownTime { get; set; }

		/// <summary>
		/// Bool Func that will provide a value for the PowerIsOn Output. Must be implemented
		/// by concrete sub-classes
		/// </summary>
		abstract protected Func<bool> PowerIsOnFeedbackFunc { get; }
		abstract protected Func<bool> IsCoolingDownFeedbackFunc { get; }
		abstract protected Func<bool> IsWarmingUpFeedbackFunc { get; }
        

		protected CTimer WarmupTimer;
		protected CTimer CooldownTimer;

		#region IRoutingInputs Members

		public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

		#endregion

		public DisplayBase(string key, string name)
			: base(key, name)
		{
			PowerIsOnFeedback = new BoolFeedback("PowerOnFeedback", PowerIsOnFeedbackFunc);
			IsCoolingDownFeedback = new BoolFeedback("IsCoolingDown", IsCoolingDownFeedbackFunc);
			IsWarmingUpFeedback = new BoolFeedback("IsWarmingUp", IsWarmingUpFeedbackFunc);

			InputPorts = new RoutingPortCollection<RoutingInputPort>();

            PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;
		}

        void PowerIsOnFeedback_OutputChange(object sender, EventArgs e)
        {
            if (UsageTracker != null)
            {
                if (PowerIsOnFeedback.BoolValue)
                    UsageTracker.StartDeviceUsage();
                else
                    UsageTracker.EndDeviceUsage();
            }
        }

		public abstract void PowerOn();
		public abstract void PowerOff();
		public abstract void PowerToggle();

        public virtual FeedbackCollection<Feedback> Feedbacks
		{
			get
			{
                return new FeedbackCollection<Feedback>
				{
					PowerIsOnFeedback,
					IsCoolingDownFeedback,
					IsWarmingUpFeedback
				};
			}
		}

		public abstract void ExecuteSwitch(object selector);

	}

	/// <summary>
	/// 
	/// </summary>
    public abstract class TwoWayDisplayBase : DisplayBase
	{
        public StringFeedback CurrentInputFeedback { get; private set; }

        abstract protected Func<string> CurrentInputFeedbackFunc { get; }


        public static MockDisplay DefaultDisplay
        { 
			get 
			{
				if (_DefaultDisplay == null)
					_DefaultDisplay = new MockDisplay("default", "Default Display");
				return _DefaultDisplay;
			} 
		}
		static MockDisplay _DefaultDisplay;

		public TwoWayDisplayBase(string key, string name)
			: base(key, name)
		{
            CurrentInputFeedback = new StringFeedback(CurrentInputFeedbackFunc);

			WarmupTime = 7000;
			CooldownTime = 15000;

            Feedbacks.Add(CurrentInputFeedback);

            
		}

	}
}